#include "mapper.hpp"
#include "pe.hpp"
#include "process.hpp"
#include "apc.hpp"
#include "nt_structs.hpp"
#include "klog.hpp"
#include "config.h"
#include <string.h>
// ---------------------------------------------------------------------------
// v4 — self-consistent image mapping.
//
// The payload DLL is mapped as a REAL image: ZwCreateSection(SEC_IMAGE) on the
// deployed DLL file, then ZwMapViewOfSection into the target. The memory
// manager derives per-page protections from the PE section characteristics
// (.text RX, .data RW, .rdata RW — the payload build marks .rdata RW so the
// IAT is writable without any protection call) and applies relocations for
// the actual view base. VAD and PTEs agree by construction.
//
// Why the previous methods died:
//   v2/v2.5 — data-only section + hand-cleared NX PTEs: injects, but a
//             PAGE_READWRITE VAD whose PTEs are executable is a persistent
//             manual-map artifact; EAAC's periodic scan found it ~3 min in
//             and the machine reset (Event 41, BugcheckCode=0).
//   v3      — ZwProtectVirtualMemory to RWX on the view: kernel refuses
//             (STATUS_SECTION_PROTECTION 0xC000004E). Dead end.
//
// Execute-policy note: EAAC enables dynamic-code (ACG) mitigations on the
// game. ACG blocks executable PAGEFILE-backed sections and exec protection
// transitions — it does NOT block file-backed image views, because that is
// how ordinary DLL loading works under ACG. The v2-era STATUS_DYNAMIC_CODE_
// BLOCKED was on SEC_COMMIT + execute; SEC_IMAGE is the allowed path.
//
// Execution: thread hijack redirects RIP to the payload's own exported
// kmap_thunk (compiled into .text by kmap_thunk.asm). The thunk saves the
// full user context, calls the image entry (DllMainCRTStartup → DllMain)
// as (base, DLL_PROCESS_ATTACH, NULL), restores the context and jumps back
// to the thread's original RIP. All runtime values live in the payload's
// exported kmap_ctx (.data), written by this driver. No RWX scratch, no
// shellcode, no protection-API calls with execute bits.
//
// The section object is created from the driver's system thread BEFORE
// attaching to the target: image-notify callbacks (PsSetLoadImageNotify-
// Routine) fire at section creation and attribute the load to the creating
// process — System (PID 4) here, not the game.
//
// Note: a file-backed section pins the payload file until the game exits
// (the loader's post-success DeleteFileA marks it delete-pending; it
// physically disappears once the view is torn down at process exit).
// ---------------------------------------------------------------------------

typedef NTSTATUS (NTAPI *pfn_MmCopyVirtualMemory)(
    PEPROCESS FromProcess, PVOID FromAddress,
    PEPROCESS ToProcess, PVOID ToAddress,
    SIZE_T BufferSize, KPROCESSOR_MODE PreviousMode, PSIZE_T BytesCopied);

// RVA → buffer pointer for a RAW FILE image (on disk the sections are not
// laid out at their RVAs — translate through the section table).
static PVOID FileRva(PVOID fileBuf, ULONG rva)
{
    auto nt = GetNtHeaders(fileBuf);
    if (!nt) return nullptr;
    auto sec = IMAGE_FIRST_SECTION(nt);
    for (USHORT i = 0; i < nt->FileHeader.NumberOfSections; i++) {
        ULONG va = sec[i].VirtualAddress;
        ULONG vs = sec[i].Misc.VirtualSize ? sec[i].Misc.VirtualSize
                                           : sec[i].SizeOfRawData;
        if (rva >= va && rva < va + vs) {
            if (rva - va >= sec[i].SizeOfRawData) return nullptr; // beyond raw data
            return (PVOID)((ULONG_PTR)fileBuf + sec[i].PointerToRawData
                           + (rva - va));
        }
    }
    return nullptr;
}

// Walk the file's export directory, return the RVA of a named export
// (function or data — both are just RVAs in AddressOfFunctions).
static ULONG FindExportRva(PVOID fileBuf, const char* name)
{
    auto nt = GetNtHeaders(fileBuf);
    if (!nt) return 0;
    auto& dir = nt->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT];
    if (!dir.VirtualAddress || dir.Size < sizeof(IMAGE_EXPORT_DIRECTORY)) return 0;

    auto exp   = (PIMAGE_EXPORT_DIRECTORY)FileRva(fileBuf, dir.VirtualAddress);
    if (!exp) return 0;
    auto names = (ULONG*)FileRva(fileBuf, exp->AddressOfNames);
    auto ords  = (USHORT*)FileRva(fileBuf, exp->AddressOfNameOrdinals);
    auto funcs = (ULONG*)FileRva(fileBuf, exp->AddressOfFunctions);
    if (!names || !ords || !funcs) return 0;

    for (ULONG i = 0; i < exp->NumberOfNames; i++) {
        const char* n = (const char*)FileRva(fileBuf, names[i]);
        if (!n) continue;
        if (strcmp(n, name) == 0) return funcs[ords[i]];
    }
    return 0;
}

// ---------------------------------------------------------------------------
// Resolve the payload's imports and write the finished IAT into the mapped
// view with ONE MmCopyVirtualMemory call. Module bases are looked up in the
// target's PEB (attached); the IAT buffer is assembled in kernel pool.
// The IAT sits in .rdata, which the payload build marks RW — a plain data
// write, no protection change.
// ---------------------------------------------------------------------------
static NTSTATUS ResolveImportsIntoView(PEPROCESS target,
                                       pfn_MmCopyVirtualMemory MmCopy,
                                       PVOID remoteBase, PVOID fileBuf)
{
    auto nt = GetNtHeaders(fileBuf);
    if (!nt) return STATUS_INVALID_IMAGE_FORMAT;
    auto& impDir = nt->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];
    auto& iatDir = nt->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IAT];
    if (!impDir.VirtualAddress) return STATUS_SUCCESS;
    if (!iatDir.VirtualAddress || !iatDir.Size || iatDir.Size > 0x10000)
        return STATUS_INVALID_IMAGE_FORMAT;

    PUCHAR iatImg = (PUCHAR)ExAllocatePool2(POOL_FLAG_NON_PAGED, iatDir.Size, 'FMfn');
    if (!iatImg) return STATUS_INSUFFICIENT_RESOURCES;
    RtlZeroMemory(iatImg, iatDir.Size);

    NTSTATUS status = STATUS_SUCCESS;

    // Attached — module lookup walks the target's PEB.
    KAPC_STATE apc;
    KeStackAttachProcess((PRKPROCESS)target, &apc);

    auto desc = (PIMAGE_IMPORT_DESCRIPTOR)FileRva(fileBuf, impDir.VirtualAddress);

    while (desc && desc->Name) {
        const char* modName = (const char*)FileRva(fileBuf, desc->Name);
        if (!modName) { status = STATUS_INVALID_IMAGE_FORMAT; break; }

        // Lowercase wide name for the PEB walk (proven v2 logic).
        WCHAR wName[64] = {};
        for (int k = 0; modName[k] && k < 63; k++) {
            WCHAR c = (WCHAR)(unsigned char)modName[k];
            if (c >= L'A' && c <= L'Z') c |= 0x20;
            wName[k] = c;
        }

        PVOID modBase = GetUserModuleBase(wName);

        // API-set redirects (api-ms-win-* → real DLL)
        if (!modBase && modName[0] == 'a' && modName[1] == 'p' &&
            modName[2] == 'i' && modName[3] == '-') {
            if (_strnicmp(modName, "api-ms-win-crt-", 15) == 0)
                modBase = GetUserModuleBase(L"ucrtbase.dll");
            else if (_strnicmp(modName, "api-ms-win-core-", 16) == 0)
                modBase = GetUserModuleBase(L"kernelbase.dll");
        }

        // System DLLs that may not be in the PEB under the exact name at
        // injection time — retry the exact wide name (proven v2 logic).
        if (!modBase) {
            struct { const char* name; const WCHAR* wide; } sysRedirects[] = {
                { "d3d11.dll",           L"d3d11.dll"           },
                { "dxgi.dll",            L"dxgi.dll"            },
                { "d3d10.dll",           L"d3d10.dll"           },
                { "d3d9.dll",            L"d3d9.dll"            },
                { "d3dcompiler_47.dll",  L"d3dcompiler_47.dll"  },
                { "user32.dll",          L"user32.dll"          },
                { "gdi32.dll",           L"gdi32.dll"           },
                { "advapi32.dll",        L"advapi32.dll"        },
                { "ole32.dll",           L"ole32.dll"           },
                { "oleaut32.dll",        L"oleaut32.dll"        },
                { "shell32.dll",         L"shell32.dll"         },
                { "ws2_32.dll",          L"ws2_32.dll"          },
                { "winmm.dll",           L"winmm.dll"           },
                { "imm32.dll",           L"imm32.dll"           },
                { "version.dll",         L"version.dll"         },
                { "msvcrt.dll",          L"msvcrt.dll"          },
                { nullptr, nullptr }
            };
            for (int r = 0; sysRedirects[r].name; r++) {
                if (_stricmp(modName, sysRedirects[r].name) == 0) {
                    modBase = GetUserModuleBase(sysRedirects[r].wide);
                    if (modBase) break;
                }
            }
        }

        if (!modBase) {
            modBase = GetKernelModuleBase(modName);
        }

        if (!modBase) {
            char msg[96] = "DLL NOT FOUND: ";
            int ml = 15;
            for (int j = 0; modName[j] && ml < 90; j++) msg[ml++] = modName[j];
            msg[ml] = '\0';
            KLog(msg);
            status = STATUS_DLL_NOT_FOUND;
            break;
        }

        auto origThunk = desc->OriginalFirstThunk
            ? (PIMAGE_THUNK_DATA64)FileRva(fileBuf, desc->OriginalFirstThunk)
            : (PIMAGE_THUNK_DATA64)FileRva(fileBuf, desc->FirstThunk);
        ULONG ftRva = desc->FirstThunk;

        if (!origThunk) { status = STATUS_INVALID_IMAGE_FORMAT; break; }

        for (ULONG e = 0; origThunk[e].u1.AddressOfData; e++) {
            PVOID fn = nullptr;

            if (IMAGE_SNAP_BY_ORDINAL64(origThunk[e].u1.Ordinal)) {
                fn = GetExportByOrdinal(modBase,
                                        (ULONG)(origThunk[e].u1.Ordinal & 0xFFFF));
            } else {
                auto ibn = (PIMAGE_IMPORT_BY_NAME)FileRva(
                    fileBuf, (ULONG)origThunk[e].u1.AddressOfData);
                fn = ibn ? GetExportByName(modBase, ibn->Name) : nullptr;
            }

            if (!fn) {
                DbgPrintEx(0, 0, "[kmap] unresolved import in %s\n", modName);
                status = STATUS_PROCEDURE_NOT_FOUND;
                break;
            }

            ULONG slotOff = ftRva + e * sizeof(ULONG64) - iatDir.VirtualAddress;
            if (slotOff + sizeof(ULONG64) > iatDir.Size) {
                status = STATUS_INVALID_IMAGE_FORMAT;
                break;
            }
            *(ULONG64*)(iatImg + slotOff) = (ULONG64)fn;
        }

        if (!NT_SUCCESS(status)) break;
        desc++;
    }

    KeUnstackDetachProcess(&apc);

    if (NT_SUCCESS(status)) {
        SIZE_T wr = 0;
        status = MmCopy(PsInitialSystemProcess, iatImg, target,
                        (PVOID)((ULONG_PTR)remoteBase + iatDir.VirtualAddress),
                        iatDir.Size, KernelMode, &wr);
        if (NT_SUCCESS(status)) KLog("IAT resolved + written");
    }

    ExFreePool(iatImg);
    return status;
}

// ---------------------------------------------------------------------------
// MapUserDll — full pipeline
// ---------------------------------------------------------------------------
NTSTATUS MapUserDll(PEPROCESS target, PVOID dllBuf)
{
    if (KeGetCurrentIrql() != PASSIVE_LEVEL) {
        KLog("MapUserDll: not PASSIVE_LEVEL, refusing");
        return STATUS_INVALID_LEVEL;
    }

    typedef NTSTATUS (NTAPI *pfn_MmCopy)(PEPROCESS, PVOID, PEPROCESS, PVOID,
        SIZE_T, KPROCESSOR_MODE, PSIZE_T);
    UNICODE_STRING uMmCopy;
    RtlInitUnicodeString(&uMmCopy, L"MmCopyVirtualMemory");
    auto MmCopy = (pfn_MmCopy)MmGetSystemRoutineAddress(&uMmCopy);
    if (!MmCopy) { KLog("could not resolve MmCopyVirtualMemory"); return STATUS_NOT_FOUND; }

    // --- Parse PE from the raw file buffer (validation only) ---
    auto nt = GetNtHeaders(dllBuf);
    if (!nt) { KLog("invalid PE"); return STATUS_INVALID_IMAGE_FORMAT; }
    if (nt->OptionalHeader.Magic != IMAGE_NT_OPTIONAL_HDR64_MAGIC) {
        KLog("not a 64-bit image"); return STATUS_INVALID_IMAGE_FORMAT;
    }
    if (nt->OptionalHeader.SizeOfImage   < 0x1000
     || nt->OptionalHeader.SizeOfImage   > 0x2000000
     || nt->OptionalHeader.SizeOfHeaders < 0x200
     || nt->OptionalHeader.SizeOfHeaders > 0x2000
     || nt->FileHeader.NumberOfSections  == 0
     || nt->FileHeader.NumberOfSections  > 64) {
        KLog("PE header out of sane bounds");
        return STATUS_INVALID_IMAGE_FORMAT;
    }

    ULONG entryRva = nt->OptionalHeader.AddressOfEntryPoint;

    // Driver↔payload contract: the payload exports its hijack thunk, the
    // thunk's context cell and the DllMain success marker.
    ULONG thunkRva  = FindExportRva(dllBuf, "kmap_thunk");
    ULONG ctxRva    = FindExportRva(dllBuf, "kmap_ctx");
    ULONG markerRva = FindExportRva(dllBuf, "kmap_marker");
    if (!thunkRva || !ctxRva || !markerRva) {
        KLog("payload contract exports missing (thunk/ctx/marker)");
        return STATUS_INVALID_IMAGE_FORMAT;
    }
    KLogHex("thunk rva:  ", thunkRva);
    KLogHex("ctx rva:    ", ctxRva);
    KLogHex("marker rva: ", markerRva);

    // -----------------------------------------------------------------------
    // Phase 1: image section from the payload FILE — created in System
    // context (no attach active: this thread is a system thread), so image
    // notify callbacks attribute the section to PID 4, not the game.
    // -----------------------------------------------------------------------
    HANDLE hFile = nullptr;
    {
        UNICODE_STRING uPath;
        RtlInitUnicodeString(&uPath, GW2_PAYLOAD_PATH);
        OBJECT_ATTRIBUTES oa;
        InitializeObjectAttributes(&oa, &uPath,
            OBJ_CASE_INSENSITIVE | OBJ_KERNEL_HANDLE, nullptr, nullptr);

        IO_STATUS_BLOCK iosb = {};
        NTSTATUS s2 = ZwCreateFile(&hFile,
            FILE_READ_DATA | FILE_EXECUTE | SYNCHRONIZE, &oa, &iosb,
            nullptr, FILE_ATTRIBUTE_NORMAL,
            FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
            FILE_OPEN, FILE_SYNCHRONOUS_IO_NONALERT | FILE_NON_DIRECTORY_FILE,
            nullptr, 0);
        if (!NT_SUCCESS(s2)) { KLogHex("ZwCreateFile(payload): ", s2); return s2; }
    }

    HANDLE hSection = nullptr;
    {
        OBJECT_ATTRIBUTES soa;
        InitializeObjectAttributes(&soa, nullptr, OBJ_KERNEL_HANDLE, nullptr, nullptr);
        NTSTATUS s2 = ZwCreateSection(&hSection, SECTION_ALL_ACCESS, &soa, nullptr,
            PAGE_EXECUTE, SEC_IMAGE, hFile);
        ZwClose(hFile); hFile = nullptr;
        if (!NT_SUCCESS(s2)) {
            KLogHex("ZwCreateSection(SEC_IMAGE) failed: ", s2);
            return s2;
        }
    }
    KLog("image section created (System context)");

    // -----------------------------------------------------------------------
    // Phase 2: map the view into the target. The kernel applies per-section
    // protections AND relocations for this exact base. SINGLE view — all
    // later writes go through MmCopyVirtualMemory, never a second view
    // (a second view at a different base would make the MM re-read
    // relocated pages from the file and discard our writes).
    // -----------------------------------------------------------------------
    PVOID  base    = nullptr;
    SIZE_T viewSize = 0;
    {
        KAPC_STATE apc;
        KeStackAttachProcess((PRKPROCESS)target, &apc);

        // PAGE_READONLY — NOT PAGE_EXECUTE. On x64/NX, PAGE_EXECUTE is
        // execute-ONLY (reads fault). Pages covered by PE sections get
        // characteristics-derived protections regardless (.text RX from the
        // section's PAGE_EXECUTE max, .rdata/.data RW), but the HEADER page
        // belongs to no PE section, so it takes the view parameter: with
        // PAGE_EXECUTE the MZ readback faults with an access violation
        // (2026-08-27 run, "mapped view not readable"). PAGE_READONLY is what
        // the user-mode loader passes for image views.
        NTSTATUS s2 = ZwMapViewOfSection(hSection, ZwCurrentProcess(), &base, 0, 0,
            nullptr, &viewSize, ViewUnmap, 0, PAGE_READONLY);
        KeUnstackDetachProcess(&apc);

        if (!NT_SUCCESS(s2)) {
            KLogHex("ZwMapViewOfSection(image) failed: ", s2);
            ZwClose(hSection);
            return s2;
        }
    }
    ZwClose(hSection);  // the view holds its own reference on the section
    KLogHex("image mapped at: ", (ULONG_PTR)base);
    KLogHex("view size:       ", (ULONG_PTR)viewSize);
    // Sanity: headers readable through the target view (header page is
    // readable only because the view was mapped PAGE_READONLY — see above).
    // Retry a few times: distinguishes a cold-page/transient fault from EAAC
    // actively invalidating the view (persistent 0xC0000005).
    {
        bool mzOk = false;
        for (int i = 0; i < 5 && !mzOk; i++) {
            USHORT mz = 0; SIZE_T rd = 0;
            NTSTATUS s2 = MmCopy(target, base, PsInitialSystemProcess,
                                 &mz, sizeof(mz), KernelMode, &rd);
            if (NT_SUCCESS(s2) && mz == 0x5A4D) {
                mzOk = true;
                KLog("view readable (MZ ok)");
                break;
            }
            KLogHex("mz read status: ", (ULONG_PTR)s2);
            KLogHex("mz value:       ", mz);
            LARGE_INTEGER d; d.QuadPart = -3000000LL; // 300 ms
            KeDelayExecutionThread(KernelMode, FALSE, &d);
        }
        if (!mzOk) {
            KLog("mapped view not readable - giving up on this attempt");
            KAPC_STATE apc; KeStackAttachProcess((PRKPROCESS)target, &apc);
            ZwUnmapViewOfSection(ZwCurrentProcess(), base);
            KeUnstackDetachProcess(&apc);
            return STATUS_UNSUCCESSFUL;
        }
    }

    // -----------------------------------------------------------------------
    // Phase 3: resolve imports into the view (single MmCopy over the IAT).
    // -----------------------------------------------------------------------
    NTSTATUS s = ResolveImportsIntoView(target, MmCopy, base, dllBuf);
    KLogHex("ResolveImports: ", s);
    if (!NT_SUCCESS(s)) {
        KAPC_STATE apc; KeStackAttachProcess((PRKPROCESS)target, &apc);
        ZwUnmapViewOfSection(ZwCurrentProcess(), base);
        KeUnstackDetachProcess(&apc);
        return s;
    }

    // -----------------------------------------------------------------------
    // Phase 4: arm kmap_ctx {origRip=0 (set at hijack), entry, dllBase}.
    // -----------------------------------------------------------------------
    {
        ULONG64 ctx[3] = { 0, (ULONG64)base + entryRva, (ULONG64)base };
        SIZE_T wr = 0;
        s = MmCopy(PsInitialSystemProcess, ctx, target,
                   (PVOID)((ULONG_PTR)base + ctxRva), sizeof(ctx), KernelMode, &wr);
        if (!NT_SUCCESS(s)) {
            KLogHex("could not write kmap_ctx: ", s);
            KAPC_STATE apc; KeStackAttachProcess((PRKPROCESS)target, &apc);
            ZwUnmapViewOfSection(ZwCurrentProcess(), base);
            KeUnstackDetachProcess(&apc);
            return s;
        }
    }
    KLog("kmap_ctx armed");

    // -----------------------------------------------------------------------
    // Phase 5: hijack a waiting thread into the payload's thunk.
    // -----------------------------------------------------------------------
    {
        PVOID thunk = (PVOID)((ULONG_PTR)base + thunkRva);
        PVOID ctx   = (PVOID)((ULONG_PTR)base + ctxRva);
        NTSTATUS hs = HijackAndInject(target, thunk, ctx);
        KLogHex("HijackAndInject: ", (ULONG_PTR)hs);
        if (!NT_SUCCESS(hs)) {
            KAPC_STATE apc; KeStackAttachProcess((PRKPROCESS)target, &apc);
            ZwUnmapViewOfSection(ZwCurrentProcess(), base);
            KeUnstackDetachProcess(&apc);
            return hs;
        }
    }

    // -----------------------------------------------------------------------
    // Phase 6: poll the payload's kmap_marker byte for 15s. DllMain writes
    // 0x42 once the image entry has run.
    // NOTE: on timeout we deliberately LEAK the view instead of unmapping —
    // the hijacked thread may still be inside DllMain; ripping the image out
    // from under it would crash the game when it returns into unmapped
    // memory. The leaked view is cleaned up automatically at process exit.
    // -----------------------------------------------------------------------
    {
        LARGE_INTEGER wait;
        wait.QuadPart = -5000000LL; // 500 ms
        for (int check = 0; check < 30; check++) {
            KeDelayExecutionThread(KernelMode, FALSE, &wait);

            UCHAR marker = 0;
            SIZE_T rd = 0;
            NTSTATUS ms = MmCopy(target, (PVOID)((ULONG_PTR)base + markerRva),
                   PsInitialSystemProcess, &marker, 1, KernelMode, &rd);

            {
                char m[64] = "marker poll #";
                int pos = 12;
                int n = check;
                char tmp[8]; int tp = 0;
                if (n == 0) { tmp[tp++] = '0'; }
                while (n > 0) { tmp[tp++] = '0' + (n % 10); n /= 10; }
                while (tp > 0) m[pos++] = tmp[--tp];
                m[pos++] = ' '; m[pos++] = '=';
                m[pos++] = ' '; m[pos++] = '0'; m[pos++] = 'x';
                const char* hexd = "0123456789ABCDEF";
                m[pos++] = hexd[(marker >> 4) & 0xF];
                m[pos++] = hexd[marker & 0xF];
                m[pos++] = ' '; m[pos++] = 'r'; m[pos++] = 'd'; m[pos++] = '=';
                m[pos++] = hexd[(rd >> 4) & 0xF];
                m[pos++] = hexd[rd & 0xF];
                m[pos] = '\0';
                KLog(m);
            }

            if (ms == STATUS_SUCCESS && marker == 0x42) {
                KLog("SUCCESS: DLL injected!");
                WriteHitFile("kmap: DllMain executed inside target\r\n");
                return s;
            }
        }
        KLog("DllMain never executed after 15s - view left mapped (thread may still be in DllMain)");
        return STATUS_UNSUCCESSFUL;
    }
}
