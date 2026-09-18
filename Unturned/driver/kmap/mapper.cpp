#include "mapper.hpp"
#include "pe.hpp"
#include "process.hpp"
#include "apc.hpp"
#include "nt_structs.hpp"
#include "klog.hpp"
#include <string.h>



static void ApplyRelocs(PVOID remoteBase, PVOID localBuf, ULONG64 delta)
{
    auto nt = GetNtHeaders(localBuf);
    auto& relDir = nt->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC];
    if (!relDir.VirtualAddress || delta == 0) return;

    ULONG remaining = relDir.Size;
    auto  block     = (PIMAGE_BASE_RELOCATION)((ULONG_PTR)localBuf + relDir.VirtualAddress);

    while (remaining >= sizeof(IMAGE_BASE_RELOCATION) && block->SizeOfBlock > 0) {
        ULONG count   = (block->SizeOfBlock - sizeof(IMAGE_BASE_RELOCATION)) / sizeof(USHORT);
        auto  entries = (USHORT*)(block + 1);

        for (ULONG i = 0; i < count; i++) {
            if ((entries[i] >> 12) != IMAGE_REL_BASED_DIR64) continue;
            ULONG offset = entries[i] & 0xFFF;
            auto  patch  = (ULONG64*)((ULONG_PTR)remoteBase + block->VirtualAddress + offset);
            *patch += delta;
        }

        remaining -= block->SizeOfBlock;
        block      = (PIMAGE_BASE_RELOCATION)((ULONG_PTR)block + block->SizeOfBlock);
    }
}


static NTSTATUS ResolveImports(PVOID remoteBase, PVOID localBuf)
{
    auto nt = GetNtHeaders(localBuf);
    auto& impDir = nt->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];
    if (!impDir.VirtualAddress) return STATUS_SUCCESS;

    auto desc = (PIMAGE_IMPORT_DESCRIPTOR)((ULONG_PTR)localBuf + impDir.VirtualAddress);

    while (desc->Name) {
        const char* modName = (const char*)((ULONG_PTR)localBuf + desc->Name);

        // Try user-space PEB first (ntdll, kernel32, user32, etc.)
        // Build a wide version of the name for GetUserModuleBase
        WCHAR wName[64] = {};
        for (int k = 0; modName[k] && k < 63; k++) {
            WCHAR c = (WCHAR)(unsigned char)modName[k];
            if (c >= L'A' && c <= L'Z') c |= 0x20;
            wName[k] = c;
        }

        PVOID modBase = GetUserModuleBase(wName);

        if (!modBase && modName[0] == 'a' && modName[1] == 'p' && modName[2] == 'i' && modName[3] == '-') {
            if (_strnicmp(modName, "api-ms-win-crt-", 15) == 0)
                modBase = GetUserModuleBase(L"ucrtbase.dll");
            else if (_strnicmp(modName, "api-ms-win-core-", 16) == 0)
                modBase = GetUserModuleBase(L"kernelbase.dll");
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
            return STATUS_DLL_NOT_FOUND;
        }

        // OriginalFirstThunk (name table in local buf) → FirstThunk (IAT in remote)
        auto origThunk = desc->OriginalFirstThunk
            ? (PIMAGE_THUNK_DATA64)((ULONG_PTR)localBuf + desc->OriginalFirstThunk)
            : (PIMAGE_THUNK_DATA64)((ULONG_PTR)localBuf + desc->FirstThunk);

        auto iat = (PIMAGE_THUNK_DATA64)((ULONG_PTR)remoteBase + desc->FirstThunk);

        while (origThunk->u1.AddressOfData) {
            PVOID fn = nullptr;

            if (IMAGE_SNAP_BY_ORDINAL64(origThunk->u1.Ordinal)) {
                fn = GetExportByOrdinal(modBase, (ULONG)(origThunk->u1.Ordinal & 0xFFFF));
            } else {
                auto ibn = (PIMAGE_IMPORT_BY_NAME)(
                    (ULONG_PTR)localBuf + origThunk->u1.AddressOfData);
                fn = GetExportByName(modBase, ibn->Name);

                if (!fn) {
                    char msg[128] = "UNRESOLVED: ";
                    int ml = 12;
                    for (int j = 0; modName[j] && ml < 60; j++) msg[ml++] = modName[j];
                    msg[ml++] = '!';
                    for (int j = 0; ibn->Name[j] && ml < 125; j++) msg[ml++] = ibn->Name[j];
                    msg[ml] = '\0';
                    KLog(msg);
                    return STATUS_PROCEDURE_NOT_FOUND;
                }
            }

            iat->u1.Function = (ULONG64)fn;
            origThunk++;
            iat++;
        }

        desc++;
    }

    return STATUS_SUCCESS;
}

// ---------------------------------------------------------------------------
// MapUserDll — full pipeline
// ---------------------------------------------------------------------------
NTSTATUS MapUserDll(PEPROCESS target, PVOID dllBuf)
{
    if (KeGetCurrentIrql() != PASSIVE_LEVEL) {
        KLog("irql");
        return STATUS_INVALID_LEVEL;
    }
    // --- Parse PE locally ---
    auto nt = GetNtHeaders(dllBuf);
    if (!nt) {
        KLog("pe1");
        return STATUS_INVALID_IMAGE_FORMAT;
    }
    if (nt->OptionalHeader.Magic != IMAGE_NT_OPTIONAL_HDR64_MAGIC) {
        KLog("pe2");
        return STATUS_INVALID_IMAGE_FORMAT;
    }

    // Sanity clamps — no SEH, so a truncated / malformed payload has to fail
    // clean here rather than fault somewhere inside the copy/reloc loop.
    if (nt->OptionalHeader.SizeOfImage   < 0x1000
     || nt->OptionalHeader.SizeOfImage   > 0x2000000     // 32 MB ceiling
     || nt->OptionalHeader.SizeOfHeaders < 0x200
     || nt->OptionalHeader.SizeOfHeaders > 0x2000
     || nt->FileHeader.NumberOfSections  == 0
     || nt->FileHeader.NumberOfSections  > 64) {
        KLog("pe3");
        return STATUS_INVALID_IMAGE_FORMAT;
    }

    ULONG  imageSize   = nt->OptionalHeader.SizeOfImage;
    ULONG  headerSize  = nt->OptionalHeader.SizeOfHeaders;
    ULONG64 prefBase   = nt->OptionalHeader.ImageBase;
    ULONG  entryRva    = nt->OptionalHeader.AddressOfEntryPoint;
    USHORT numSections = nt->FileHeader.NumberOfSections;
    auto   sections    = IMAGE_FIRST_SECTION(nt);

    // -----------------------------------------------------------------------
    // Phase 1: Create pagefile-backed section → map into target → copy + fix
    //
    // Why section instead of ZwAllocateVirtualMemory:
    //   ZwAllocateVirtualMemory sets VAD.PrivateMemory = TRUE (MEM_PRIVATE).
    //   BEDaisy's VAD walk flags "private executable memory not in PEB.Ldr".
    //   ZwMapViewOfSection of a pagefile-backed section sets PrivateMemory = FALSE
    //   (appears as MEM_MAPPED), bypassing that specific check.
    // -----------------------------------------------------------------------

    // Create a pagefile-backed section large enough for the image.
    // PAGE_EXECUTE_READWRITE as max protection so we can later set per-section
    // protections (including PAGE_EXECUTE_READ for .text) via ZwProtectVirtualMemory.
    HANDLE hSection = nullptr;
    {
        LARGE_INTEGER secSz;
        secSz.QuadPart = imageSize;
        OBJECT_ATTRIBUTES secOa;
        InitializeObjectAttributes(&secOa, nullptr, OBJ_KERNEL_HANDLE, nullptr, nullptr);
        NTSTATUS s2 = ZwCreateSection(
            &hSection, SECTION_ALL_ACCESS, &secOa,
            &secSz, PAGE_EXECUTE_READWRITE, SEC_COMMIT, nullptr);
        if (!NT_SUCCESS(s2)) {
            KLogHex("sec:", s2);
            return s2;
        }
    }

    // Attach to target and map the section as RWX.
    // We copy/reloc/import, then tighten per-section protections.
    // Mapping as RWX guarantees code is executable even if ZwProtectVirtualMemory
    // fails (DEP would kill execution from PAGE_READWRITE pages).
    KAPC_STATE apcState;
    KeStackAttachProcess((PRKPROCESS)target, &apcState);

    PVOID    base     = nullptr;
    SIZE_T   viewSize = 0;
    NTSTATUS s        = STATUS_SUCCESS;

    do {
        s = ZwMapViewOfSection(
            hSection, ZwCurrentProcess(),
            &base, 0, 0, nullptr, &viewSize,
            ViewUnmap, 0, PAGE_EXECUTE_READWRITE);

        ZwClose(hSection); hSection = nullptr;

        if (!NT_SUCCESS(s)) {
            KLogHex("map:", s);
            break;
        }
        KLogHex("@", (ULONG_PTR)base);

        // Copy headers
        RtlCopyMemory(base, dllBuf, headerSize);

        // Copy sections
        for (USHORT i = 0; i < numSections; i++) {
            if (!sections[i].SizeOfRawData) continue;
            PVOID dst = (PVOID)((ULONG_PTR)base + sections[i].VirtualAddress);
            PVOID src = (PVOID)((ULONG_PTR)dllBuf + sections[i].PointerToRawData);
            RtlCopyMemory(dst, src, sections[i].SizeOfRawData);
        }

        // Fix relocations — read reloc data from `base` (sections at VA offsets),
        // NOT from dllBuf (raw file where data is at PointerToRawData offsets).
        ULONG64 delta = (ULONG64)base - prefBase;
        ApplyRelocs(base, base, delta);

        // Resolve imports — same: read import descriptors/thunks from mapped image.
        s = ResolveImports(base, base);
        KLogHex("imp:", s);

        if (!NT_SUCCESS(s)) {
            ZwUnmapViewOfSection(ZwCurrentProcess(), base);
            base = nullptr;
            break;
        }

        // Wipe PE header
        SIZE_T wipeSize = min(headerSize, 0x1000u);
        RtlZeroMemory(base, wipeSize);

        // Per-section protections + wipe .reloc
        for (USHORT i = 0; i < numSections; i++) {
            PVOID  secBase = (PVOID)((ULONG_PTR)base + sections[i].VirtualAddress);
            SIZE_T secSize = (SIZE_T)sections[i].Misc.VirtualSize;
            if (!secSize) continue;

            NTSTATUS protStatus;
            if (SecNameEq(sections[i].Name, ".reloc")) {
                RtlZeroMemory(secBase, secSize);
                ULONG old = 0;
                protStatus = ZwProtectVirtualMemory(NtCurrentProcess(), &secBase, &secSize,
                                       PAGE_NOACCESS, &old);
            } else {
                ULONG prot = CharsToProt(sections[i].Characteristics);
                ULONG old  = 0;
                protStatus = ZwProtectVirtualMemory(NtCurrentProcess(), &secBase, &secSize, prot, &old);
            }
            if (!NT_SUCCESS(protStatus)) {
                KLogHex("vp:", i);
                KLogHex("vs:", protStatus);
            }
        }

        // Verify entry point page is executable by re-protecting and reading old value
        {
            PVOID epPage = (PVOID)((ULONG_PTR)base + entryRva);
            SIZE_T epSize = 1;
            ULONG oldProt = 0;
            ZwProtectVirtualMemory(
                NtCurrentProcess(), &epPage, &epSize, PAGE_EXECUTE_READ, &oldProt);
        }
    } while (0);

    if (hSection) { ZwClose(hSection); hSection = nullptr; }
    KeUnstackDetachProcess(&apcState);

    // Bail if the attach block failed — otherwise base is null (or garbage)
    // and we'd queue a user APC into Unturned at ~entryRva (a low userspace
    // RVA), faulting the game and looking like a BE detection.
    if (!NT_SUCCESS(s) || !base) {
        KLogHex("skip:", (ULONG_PTR)s);
        return s;
    }

    // -----------------------------------------------------------------------
    // Phase 4: Execute DllMain via thread hijacking
    // -----------------------------------------------------------------------
    if (entryRva) {
        PVOID ep = (PVOID)((ULONG_PTR)base + entryRva);
        KLogHex("ep:", (ULONG_PTR)ep);
        s = HijackAndInject(target, ep, base);
    } else {
        KLog("noep");
        s = STATUS_SUCCESS;
    }

    // -----------------------------------------------------------------------
    // Phase 5: Verify DllMain executed by reading marker byte
    // Payload writes 0x42 to hInst+0x100 (in wiped PE header region, was 0x00).
    // Wait, re-attach, read it back.
    // -----------------------------------------------------------------------
    if (NT_SUCCESS(s) && base) {
        for (int check = 0; check < 5; check++) {
            LARGE_INTEGER wait;
            wait.QuadPart = -30000000LL;
            KeDelayExecutionThread(KernelMode, FALSE, &wait);

            KAPC_STATE verifyApc;
            KeStackAttachProcess((PRKPROCESS)target, &verifyApc);
            UCHAR marker = *(volatile UCHAR*)((ULONG_PTR)base + 0x100);
            KeUnstackDetachProcess(&verifyApc);

            if (marker == 0x42) {
                KLog("done");
                WriteHitFile("completed\r\n");
                return s;
            }
        }
        KLog("timeout");
    }

    return s;
}
