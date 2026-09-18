#include "mapper.hpp"
#include "pe.hpp"
#include "process.hpp"
#include "apc.hpp"
#include "nt_structs.hpp"
#include "config.h"
#include "klog.hpp"
#include "beep.hpp"
#include <string.h>
#include <intrin.h>
// ---------------------------------------------------------------------------
// RW -> RX via PTE manipulation.
//
// EAAC sets process mitigation flags that reject ANY executable mapping
// (STATUS_DYNAMIC_CODE_BLOCKED on ZwMapViewOfSection/ZwProtectVirtualMemory
// with execute bits) and flag the attempt as tampering. So we map the
// payload as plain PAGE_READWRITE (looks like data to the VAD) and then
// clear the NX bit directly in the target's page tables. The VAD still says
// RW; the PTE says execute is fine. No protection API is ever called with
// an execute bit.
//
// (v3 note: kernel-mode ZwProtectVirtualMemory to RWX was tried and returns
// STATUS_SECTION_PROTECTION 0xC000004E regardless of the section's max
// protection. Dead end — this PTE method is the one that injects.)
// ---------------------------------------------------------------------------

// The page tables themselves are accessed through ntoskrnl's PTE self-map
// (MmPteBase). While attached to the target process, kernel VAs computed from
// MmPteBase read/write the TARGET's PTEs directly. No CR3 reads, no physical
// memory mapping (MmMapIoSpace was failing on this VM and returning 0 ->
// the walk silently cleared nothing), no leak risk.

static ULONG_PTR g_PteBase = 0;

// Observation dumps (diagnostic build): payload base/size captured on
// successful injection, read by the observation engine below.
static PVOID  g_payBase = nullptr;
static SIZE_T g_paySize = 0;
// Pages covered by a WRITABLE PE section — the payload legitimately mutates
// its own .data at runtime, so content-watching those pages would drown real
// tamper events in noise. 8192 pages = 32 MB max image (matches the sanity
// cap in MapUserDll).
static ULONG64 g_payMut[128];
static LONG    g_payMutCount = 0;

// MmPteBase is not exported by name on Win11 24H2/25H2. Fall back to scanning
// ntoskrnl .text for MiGetPteAddress (verified disassembly Win10 22H2+):
//   shr rcx,9; mov rax,0x7FFFFFFFF8; and rcx,rax;
//   mov rax,<imm64 = PTE base @ +0x13>; add rax,rcx; ret
// Alternate (older builds):
//   mov rax,[rip+PTE_BASE]; shr rcx,9; and ecx,7FFFFFF8h; add rax,rcx; ret

static ULONG_PTR FindPattern(const UCHAR* sig, const char* mask, SIZE_T len,
                             ULONG_PTR start, SIZE_T range)
{
    for (SIZE_T i = 0; i + len <= range; i++) {
        bool hit = true;
        for (SIZE_T j = 0; j < len; j++) {
            if (mask[j] == 'x' && *(UCHAR*)(start + i + j) != sig[j]) { hit = false; break; }
        }
        if (hit) return start + i;
    }
    return 0;
}

static ULONG_PTR ResolvePteBaseViaScan()
{
    PVOID ntos = GetKernelModuleBase("ntoskrnl.exe");
    if (!ntos) { KLog("scan: ntoskrnl not found"); return 0; }

    auto dos = (PIMAGE_DOS_HEADER)ntos;
    if (dos->e_magic != IMAGE_DOS_SIGNATURE) return 0;
    auto nthe = (PIMAGE_NT_HEADERS64)((ULONG_PTR)ntos + dos->e_lfanew);
    if (nthe->Signature != IMAGE_NT_SIGNATURE) return 0;
    auto sec = IMAGE_FIRST_SECTION(nthe);
    ULONG_PTR textBase = 0; SIZE_T textSize = 0;
    for (USHORT i = 0; i < nthe->FileHeader.NumberOfSections; i++) {
        if (sec[i].Name[0] == '.' && sec[i].Name[1] == 't' &&
            sec[i].Name[2] == 'e' && sec[i].Name[3] == 'x' &&
            sec[i].Name[4] == 't') {
            textBase = (ULONG_PTR)ntos + sec[i].VirtualAddress;
            textSize = sec[i].Misc.VirtualSize;
            break;
        }
    }
    if (!textBase) { KLog("scan: .text not found"); return 0; }

    // Primary: imm64 variant (Win10 22H2 + Win11 24H2/25H2)
    static const UCHAR sigB[] = {
        0x48, 0xC1, 0xE9, 0x09,
        0x48, 0xB8, 0xF8, 0xFF, 0xFF, 0xFF, 0x7F, 0x00, 0x00, 0x00,
        0x48, 0x23, 0xC8,
        0x48, 0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x48, 0x03, 0xC1,
        0xC3
    };
    static const char maskB[] = "xxxxxxxxxxxxxxxxxxx????????xxxxxxxx";
    ULONG_PTR hit = FindPattern(sigB, maskB, sizeof(sigB), textBase, textSize);
    if (hit) {
        ULONG_PTR pteBase = *(ULONG64*)(hit + 19);
        KLogHex("PteBase via scan B:", pteBase);
        return pteBase;
    }

    // Alternate: rip-relative variant (older builds)
    static const UCHAR sigA[] = {
        0x48, 0x8B, 0x05, 0x00, 0x00, 0x00, 0x00,
        0x48, 0xC1, 0xE9, 0x09,
        0x81, 0xE1, 0xF8, 0xFF, 0xFF, 0x7F,
        0x48, 0x03, 0xC1,
        0xC3
    };
    static const char maskA[] = "xxx????xxxxxxxxxxxxxxxx";
    hit = FindPattern(sigA, maskA, sizeof(sigA), textBase, textSize);
    if (hit) {
        LONG disp = *(LONG*)(hit + 3);
        ULONG_PTR pteBase = hit + 7 + disp;
        KLogHex("PteBase via scan A:", pteBase);
        return pteBase;
    }

    KLog("scan: MiGetPteAddress pattern not found");
    return 0;
}

static ULONG_PTR GetPteBase()
{
    if (g_PteBase) return g_PteBase;
    UNICODE_STRING u;
    RtlInitUnicodeString(&u, L"MmPteBase");
    g_PteBase = (ULONG_PTR)MmGetSystemRoutineAddress(&u);
    if (g_PteBase) {
        KLogHex("PteBase via export:", g_PteBase);
        return g_PteBase;
    }
    g_PteBase = ResolvePteBaseViaScan();
    return g_PteBase;
}


// PdeBase = PteBase + self-map offset of PteBase itself.
static ULONG_PTR PdeBaseOf(ULONG_PTR pteBase)
{
    return pteBase + ((pteBase >> 9) & 0x7FFFFFFFF8ULL);
}

// PTE self-map address for a 4K page at user VA.
static ULONG_PTR PteAddrOf(ULONG_PTR va, ULONG_PTR pteBase)
{
    return pteBase + ((va >> 9) & 0x7FFFFFFFF8ULL);
}

// PDE self-map address for a 2MB region containing user VA.
static ULONG_PTR PdeAddrOf(ULONG_PTR va, ULONG_PTR pdeBase)
{
    return pdeBase + ((va >> 18) & 0x7FFFFFFFF8ULL);
}

// Validate a candidate PTE self-map base BEFORE any write: read the
// self-map entry for a known present user page and compare its PFN field
// against MmGetPhysicalAddress. A wrong base (e.g. a pattern-scan false
// positive resolving to the PFN database, as seen on 22621.4317) reads
// garbage whose PFN won't match — we abort instead of corrupting kernel
// memory. MUST be called while attached to the target process.
static bool ValidatePteBase(ULONG_PTR pteBase, ULONG64 testVa)
{
    if (!pteBase) return false;

    PHYSICAL_ADDRESS pa = MmGetPhysicalAddress((PVOID)testVa);
    ULONG64 expectPfn = pa.QuadPart >> 12;
    if (!expectPfn) { KLog("validate: testVa not backed by RAM"); return false; }

    ULONG64 pva = PteAddrOf(testVa, pteBase);
    if (!MmIsAddressValid((PVOID)pva)) {
        KLogHex("validate: self-map addr not mapped: ", pva);
        return false;
    }
    ULONG64 pte = *(volatile ULONG64*)pva;
    if (!(pte & 1)) { KLog("validate: entry not present"); return false; }

    ULONG64 pfn = (pte >> 12) & 0xFFFFFFFFFULL;
    if (pfn != expectPfn) {
        KLogHex("validate: FAIL pte=", pte);
        KLogHex("validate: expectPfn=", expectPfn);
        KLogHex("validate: gotPfn=", pfn);
        return false;
    }
    return true;
}

// Clear NX (bit 63) for user VA range [va, va+size) via the PTE self-map.
// MUST be called while attached to the target process so the self-map
// reflects the target's user page tables. Handles 4K PTEs and 2MB large
// pages (PDE). 1GB pages are skipped — pagefile sections never use those.
static bool ClearNxRange(ULONG64 va, ULONG64 size)
{
    ULONG_PTR pteBase = GetPteBase();
    if (!pteBase) {
        KLog("FAIL: MmPteBase unresolved — cannot flip NX");
        return false;
    }

    // CRITICAL: never trust the resolved base blindly. A pattern-scan
    // false positive on some builds resolves to the PFN database
    // (0xFFFFF70000000000) — writing there corrupts PFN entries and the
    // machine dies minutes later with no dump. Validate against the
    // target's own page tables first; fall back to the arch constant for
    // 4-level paging; abort cleanly if neither validates.
    if (!ValidatePteBase(pteBase, va)) {
        KLog("WARN: resolved PTE base failed validation");
        const ULONG_PTR kArchPteBase = 0xFFFFF68000000000ULL;
        if (ValidatePteBase(kArchPteBase, va)) {
            KLog("INFO: using arch constant PTE base 0xFFFFF68000000000");
            pteBase = kArchPteBase;
            g_PteBase = kArchPteBase;
        } else {
            KLog("FAIL: no valid PTE base — aborting NX clear (no writes)");
            return false;
        }
    } else {
        KLogHex("PteBase validated: ", pteBase);
    }
    ULONG_PTR pdeBase = PdeBaseOf(pteBase);

    ULONG64 end = va + size;
    ULONG64 cur = va & ~0xFFFULL;
    ULONG64 cleared = 0, notPresent = 0;

    KLogHex("ClearNx va:  ", va);

    while (cur < end) {
        ULONG64* pde = (ULONG64*)PdeAddrOf(cur, pdeBase);
        ULONG64 pdeV = *pde;
        if (!(pdeV & 1)) {
            cur = (cur & ~0x1FFFFFULL) + 0x200000ULL;
            continue;
        }

        if (pdeV & 0x80) {
            // 2MB large page: clear NX in the PDE itself.
            if (pdeV & (1ULL << 63)) { *pde = pdeV & ~(1ULL << 63); cleared++; }
            cur = (cur & ~0x1FFFFFULL) + 0x200000ULL;
            continue;
        }

        ULONG64* pte = (ULONG64*)PteAddrOf(cur, pteBase);
        ULONG64 pteV = *pte;
        if (!(pteV & 1)) {
            notPresent++;
            cur += 0x1000;
            continue;
        }
        if (pteV & (1ULL << 63)) { *pte = pteV & ~(1ULL << 63); cleared++; }
        cur += 0x1000;
    }

    KLogHex("NX cleared on pages: ", cleared);
    KLogHex("NX not-present pages:", notPresent);
    return cleared > 0;
}


// Flush every core's TLB — stale NX entries would #PF on first execute.
static ULONG64 NTAPI FlushTbCallback(ULONG_PTR)
{
    __writecr3(__readcr3());
    return 0;
}

typedef ULONG64 (NTAPI *pfn_KeIpiGenericCall)(PVOID Callback, ULONG_PTR Context);

static void FlushTlbAllCores()
{
    UNICODE_STRING u;
    RtlInitUnicodeString(&u, L"KeIpiGenericCall");
    auto ipi = (pfn_KeIpiGenericCall)MmGetSystemRoutineAddress(&u);
    if (ipi) {
        ipi((PVOID)FlushTbCallback, 0);
        KLog("TLB flushed on all cores (KeIpiGenericCall)");
    } else {
        __writecr3(__readcr3());
        KLog("WARN: KeIpiGenericCall not found — flushed current core only");
    }
}

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

        // API-set redirects (api-ms-win-* → real DLL)
        if (!modBase && modName[0] == 'a' && modName[1] == 'p' && modName[2] == 'i' && modName[3] == '-') {
            if (_strnicmp(modName, "api-ms-win-crt-", 15) == 0)
                modBase = GetUserModuleBase(L"ucrtbase.dll");
            else if (_strnicmp(modName, "api-ms-win-core-", 16) == 0)
                modBase = GetUserModuleBase(L"kernelbase.dll");
        }

        // System DLLs that may not be in the PEB yet at injection time.
        // These are standard Windows components in System32 — the game will
        // load them eventually; we just need them resolved NOW for our payload.
        if (!modBase) {
            struct { const char* name; const WCHAR* wide; } sysRedirects[] = {
                { "d3dcompiler_47.dll",  L"d3dcompiler_47.dll" },
                { "d3dcompiler_46.dll",  L"d3dcompiler_46.dll" },
                { "d3dcompiler_43.dll",  L"d3dcompiler_43.dll" },
                { "d3d11.dll",           L"d3d11.dll" },
                { "d3d10.dll",           L"d3d10.dll" },
                { "d3d9.dll",            L"d3d9.dll" },
                { "dxgi.dll",            L"dxgi.dll" },
                { "xinput1_3.dll",       L"xinput1_3.dll" },
                { "xinput1_4.dll",       L"xinput1_4.dll" },
                { "xinput9_1_0.dll",     L"xinput9_1_0.dll" },
                { "msvcrt.dll",          L"msvcrt.dll" },
                { "advapi32.dll",        L"advapi32.dll" },
                { "ole32.dll",           L"ole32.dll" },
                { "oleaut32.dll",        L"oleaut32.dll" },
                { "shell32.dll",         L"shell32.dll" },
                { "user32.dll",          L"user32.dll" },
                { "gdi32.dll",           L"gdi32.dll" },
                { "ws2_32.dll",          L"ws2_32.dll" },
                { "winmm.dll",           L"winmm.dll" },
                { "imm32.dll",           L"imm32.dll" },
                { "version.dll",         L"version.dll" },
                { nullptr, nullptr }
            };
            for (int r = 0; sysRedirects[r].name; r++) {
                if (_stricmp(modName, sysRedirects[r].name) == 0) {
                    // Not in PEB — try loading it into the target via LdrLoadDll
                    // equivalent: just search PEB again with exact case, or fall
                    // back to GetKernelModuleBase. But these are USER DLLs, not
                    // kernel. The real fix: force-load them via ZwMapViewOfSection
                    // of the system DLL. Simpler: use MmGetSystemRoutineAddress
                    // to check ntoskrnl (won't work for user DLLs).
                    //
                    // Best approach: the DLL IS in the process but our PEB walk
                    // missed it (case sensitivity, or loaded under a different
                    // name). Try the wide name directly.
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
                    DbgPrintEx(0, 0, "[kmap] unresolved: %s!%s\n", modName, ibn->Name);
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
        KLog("MapUserDll: not PASSIVE_LEVEL, refusing");
        return STATUS_INVALID_LEVEL;
    }

    // Resolve MmCopyVirtualMemory for safe cross-process writes.
    // Direct RtlCopyMemory into the target's view causes BSOD when EAAC
    // invalidates the pages. MmCopy probes and returns NTSTATUS instead.
    typedef NTSTATUS (NTAPI *pfn_MmCopy)(PEPROCESS, PVOID, PEPROCESS, PVOID,
        SIZE_T, KPROCESSOR_MODE, PSIZE_T);
    UNICODE_STRING uMmCopy;
    RtlInitUnicodeString(&uMmCopy, L"MmCopyVirtualMemory");
    pfn_MmCopy MmCopy = (pfn_MmCopy)MmGetSystemRoutineAddress(&uMmCopy);
    if (!MmCopy) { KLog("FAIL: MmCopyVirtualMemory not found"); return STATUS_NOT_FOUND; }

    // --- Parse PE locally ---
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

    ULONG   imageSize   = nt->OptionalHeader.SizeOfImage;
    ULONG   headerSize  = nt->OptionalHeader.SizeOfHeaders;
    ULONG64 prefBase    = nt->OptionalHeader.ImageBase;
    ULONG   entryRva    = nt->OptionalHeader.AddressOfEntryPoint;
    USHORT  numSections = nt->FileHeader.NumberOfSections;
    auto    sections    = IMAGE_FIRST_SECTION(nt);

    // -----------------------------------------------------------------------
    // Phase 1: Create pagefile section → map into target → get base address
    // -----------------------------------------------------------------------
    HANDLE hSection = nullptr;
    {
        LARGE_INTEGER secSz; secSz.QuadPart = imageSize;
        OBJECT_ATTRIBUTES secOa;
        InitializeObjectAttributes(&secOa, nullptr, OBJ_KERNEL_HANDLE, nullptr, nullptr);
        NTSTATUS s2 = ZwCreateSection(&hSection, SECTION_ALL_ACCESS, &secOa,
            &secSz, PAGE_READWRITE, SEC_COMMIT, nullptr);
        if (!NT_SUCCESS(s2)) { KLogHex("ZwCreateSection failed: ", s2); return s2; }
    }

    PVOID    base     = nullptr;
    SIZE_T   viewSize = 0;
    NTSTATUS s        = STATUS_SUCCESS;

    // Attach ONLY to map the view and resolve imports (PEB walk). All WRITES
    // to the target go through MmCopyVirtualMemory from system context.
    {
        KAPC_STATE apcState;
        KeStackAttachProcess((PRKPROCESS)target, &apcState);

        s = ZwMapViewOfSection(hSection, ZwCurrentProcess(), &base, 0, 0,
            nullptr, &viewSize, ViewUnmap, 0, PAGE_READWRITE);
        ZwClose(hSection); hSection = nullptr;

        if (!NT_SUCCESS(s)) {
            KLogHex("ZwMapViewOfSection failed: ", s);
            KeUnstackDetachProcess(&apcState);
            return s;
        }
        KLogHex("mapped at: ", (ULONG_PTR)base);
        KeUnstackDetachProcess(&apcState);
    }

    // -----------------------------------------------------------------------
    // Phase 2: Prepare image in KERNEL pool (safe from EAAC interference)
    // -----------------------------------------------------------------------
    PVOID localImg = ExAllocatePool2(POOL_FLAG_NON_PAGED, imageSize, 'FMfn');
    if (!localImg) {
        KLog("FAIL: could not allocate local image buffer");
        KAPC_STATE apc; KeStackAttachProcess((PRKPROCESS)target, &apc);
        ZwUnmapViewOfSection(ZwCurrentProcess(), base);
        KeUnstackDetachProcess(&apc);
        return STATUS_INSUFFICIENT_RESOURCES;
    }
    RtlZeroMemory(localImg, imageSize);

    // Copy headers + sections into local buffer (kernel→kernel, safe)
    RtlCopyMemory(localImg, dllBuf, headerSize);
    for (USHORT i = 0; i < numSections; i++) {
        if (!sections[i].SizeOfRawData) continue;
        PVOID dst = (PVOID)((ULONG_PTR)localImg + sections[i].VirtualAddress);
        PVOID src = (PVOID)((ULONG_PTR)dllBuf + sections[i].PointerToRawData);
        RtlCopyMemory(dst, src, sections[i].SizeOfRawData);
    }

    // Fix relocations in the LOCAL buffer (pointers adjusted to remote base)
    ULONG64 delta = (ULONG64)base - prefBase;
    ApplyRelocs(localImg, localImg, delta);

    // Resolve imports — needs target's PEB for module lookup, but writes IAT
    // into the LOCAL buffer (not the remote view). Attach for reads only.
    {
        KAPC_STATE apcState;
        KeStackAttachProcess((PRKPROCESS)target, &apcState);
        s = ResolveImports(localImg, localImg);
        KeUnstackDetachProcess(&apcState);
    }
    KLogHex("ResolveImports: ", s);
    if (!NT_SUCCESS(s)) {
        ExFreePool(localImg);
        KAPC_STATE apc; KeStackAttachProcess((PRKPROCESS)target, &apc);
        ZwUnmapViewOfSection(ZwCurrentProcess(), base);
        KeUnstackDetachProcess(&apc);
        return s;
    }

    // KEEP the PE header intact (v4 behavior). The payload's
    // RegisterFunctionTable() walks its own DOS/NT headers to call
    // RtlAddFunctionTable — a zeroed header silently registers nothing and
    // every __try/__except in the payload becomes dead code.

    // Wipe .reloc in local buffer
    for (USHORT i = 0; i < numSections; i++) {
        if (SecNameEq(sections[i].Name, ".reloc") && sections[i].Misc.VirtualSize) {
            RtlZeroMemory((PVOID)((ULONG_PTR)localImg + sections[i].VirtualAddress),
                sections[i].Misc.VirtualSize);
        }
    }

    // -----------------------------------------------------------------------
    // Phase 3: Blast prepared image into target via MmCopyVirtualMemory
    //          This CANNOT BSOD — MmCopy probes and returns NTSTATUS on fault.
    // -----------------------------------------------------------------------
    SIZE_T written = 0;
    s = MmCopy(PsInitialSystemProcess, localImg, target, base, imageSize, KernelMode, &written);
    ExFreePool(localImg);

    if (!NT_SUCCESS(s)) {
        KLog("FAIL: MmCopyVirtualMemory into target failed — EAAC likely blocked the view");
        KLogHex("  status: ", s);
        KAPC_STATE apc; KeStackAttachProcess((PRKPROCESS)target, &apc);
        ZwUnmapViewOfSection(ZwCurrentProcess(), base);
        KeUnstackDetachProcess(&apc);
        return s;
    }
    KLog("Image copied to target via MmCopy");

    // Make the mapped image executable by clearing NX in the target's page
    // tables (v2.5 method — proven to inject on this machine, Win11 22H2).
    // VAD stays PAGE_READWRITE so EAAC sees plain data; only the PTEs allow
    // execute. No protection API is ever called with an execute bit.
    {
        KAPC_STATE apcState;
        KeStackAttachProcess((PRKPROCESS)target, &apcState);
        bool nxOk = ClearNxRange((ULONG64)base, imageSize);
        KeUnstackDetachProcess(&apcState);
        if (!nxOk) {
            KLog("FAIL: NX flip failed — unmapping view");
            KAPC_STATE apc; KeStackAttachProcess((PRKPROCESS)target, &apc);
            ZwUnmapViewOfSection(ZwCurrentProcess(), base);
            KeUnstackDetachProcess(&apc);
            return STATUS_UNSUCCESSFUL;
        }
    }
    FlushTlbAllCores();

    // -----------------------------------------------------------------------
    // Phase 4: Execute DllMain via thread hijack (apc.cpp). Shellcode at
    // base+0x200 calls DllMain(base, DLL_PROCESS_ATTACH, NULL), writes the
    // 0x42 marker at base+0x100, restores the thread and jumps back.
    // -----------------------------------------------------------------------
    {
        PVOID entry = (PVOID)((ULONG_PTR)base + entryRva);
        NTSTATUS hs = HijackAndInject(target, entry, base);
        KLogHex("HijackAndInject: ", (ULONG_PTR)hs);
        if (!NT_SUCCESS(hs)) {
            KAPC_STATE apc; KeStackAttachProcess((PRKPROCESS)target, &apc);
            ZwUnmapViewOfSection(ZwCurrentProcess(), base);
            KeUnstackDetachProcess(&apc);
            return hs;
        }
    }

    // -----------------------------------------------------------------------
    // Phase 5: Poll the DllMain marker byte (0x42 at base+0x100) for 15s.
    // -----------------------------------------------------------------------
    {
        LARGE_INTEGER wait;
        wait.QuadPart = -5000000LL; // 500 ms
        for (int check = 0; check < 30; check++) {
            KeDelayExecutionThread(KernelMode, FALSE, &wait);

            UCHAR marker = 0;
            SIZE_T rd = 0;
            NTSTATUS ms = MmCopy(target, (PVOID)((ULONG_PTR)base + 0x100),
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

            if (marker == 0x42) {
                g_payBase = base;      // captured for the observation engine
                g_paySize = imageSize;
                // Observation: mark pages covered by writable sections
                // (payload's own churn there is expected and uninteresting).
                RtlZeroMemory(g_payMut, sizeof g_payMut);
                g_payMutCount = 0;
                {
                    LONG np = (LONG)((imageSize + 0xFFFULL) >> 12);
                    if (np > 8192) np = 8192;
                    g_payMutCount = np;
                    const ULONG SCNW = 0x80000000UL;  // IMAGE_SCN_MEM_WRITE
                    for (USHORT si = 0; si < numSections; si++) {
                        if (!(sections[si].Characteristics & SCNW)) continue;
                        ULONG64 s = sections[si].VirtualAddress;
                        ULONG64 e = s + sections[si].Misc.VirtualSize;
                        ULONG64 firstPg = (s & ~0xFFFULL) >> 12;
                        ULONG64 endPg   = (e + 0xFFFULL) >> 12;
                        for (ULONG64 pg = firstPg; pg < endPg && pg < (ULONG64)np; pg++)
                            g_payMut[pg >> 6] |= (1ULL << (pg & 63));
                    }
                }
                KLog("SUCCESS: DLL injected!");
                WriteHitFile("kmap: DllMain executed inside target\r\n");
                return s;
            }
        }
        // v4 behavior: LEAK the view. The hijacked thread's trap-frame RIP
        // may still point at base+0x200 — if it resumes after we rip the
        // image out, that's an instant unhandleable AV in the target.
        // Register the leaked range with the hijacker so the RETRY never
        // embeds this image's stale shellcode RIP as a return address
        // (friend-run 2026-09-08: retry chained image-1 shellcode ->
        // C0000005 at base+0x200 after image-2's DllMain returned).
        ApcRegisterLeakedRange((ULONG64)(ULONG_PTR)base, (ULONG64)imageSize);
        KLog("FAIL: DllMain never executed after 15s — leaking view (thread RIP may still point inside)");
        return STATUS_UNSUCCESSFUL;
    }

    return s;
}

// ===========================================================================
// EAAC OBSERVATION INSTRUMENTATION (diagnostic build)
// ===========================================================================
// v2.5 injects reliably but the machine hard-resets ~3 minutes later, so
// EAAC's periodic scan is finding something. Stop guessing; capture ground
// truth. This engine polls the target EVERY SECOND for 10 minutes and
// writes CHANGE-TRIGGERED dumps:
//
//   gw2_dump.txt        baseline (pre-inject) + first snapshot + every change
//   gw2_dump_pay_N.bin  raw payload bytes: initial + on content change
//
// Watched signals (compared against the previous second):
//   REGION   payload VAD region presence/State/Protect/Type/Size, or the
//            private+executable region count/bytes changed
//   PTE      per-page PTE NX|RW bits (mask 0x8000000000000002 — residency
//            churn from working-set trimming is ignored)
//   CONTENT  per-page FNV over the FULL 4KB, only on pages NOT covered by a
//            writable PE section (g_payMut; the payload's own .data churn
//            would otherwise drown real events in noise)
//
// Why 1s: EAAC's mutation is a single event and the reset can follow within
// seconds. A 30s cadence can miss the flip entirely — the last dump before
// the reset may predate it; 1s bounds staleness to one second and gives the
// exact timeline. Change-triggered writes keep disk usage tiny.
// Everything is FILE_WRITE_THROUGH, written BEFORE the next sleep —
// whatever survives a hard reset is evidence to the second.
//
// Line formats:
//   V <base> <size> S=<state> P=<protect> T=<type>  region walk
//   P <offset> <pte>                                 payload PTE (full dump)
//   C <offset> <fnv-of-4KB> <status>                 payload page hash
//   PE <offset> <old> -> <new>                       changed PTE (event)
//   CE <offset> <old> -> <new>                       changed content (event)
//   PAY ...                                          payload region state
// ===========================================================================

typedef struct _MBI_X64 {
    PVOID  BaseAddress;
    PVOID  AllocationBase;
    ULONG  AllocationProtect;
    ULONG  __pad1;
    SIZE_T RegionSize;
    ULONG  State;
    ULONG  Protect;
    ULONG  Type;
    ULONG  __pad2;
} MBI_X64;

typedef NTSTATUS (NTAPI *pfn_ZwQVM)(HANDLE, PVOID, ULONG, PVOID, SIZE_T, PSIZE_T);
typedef NTSTATUS (NTAPI *pfn_MmCopyV)(PEPROCESS, PVOID, PEPROCESS, PVOID,
    SIZE_T, KPROCESSOR_MODE, PSIZE_T);

#define DUMP_MEM_FREE     0x10000UL
#define DUMP_MEM_PRIVATE  0x20000UL
#define DUMP_TEXT_PATH    L"\\??\\C:\\Users\\Public\\gw2_dump.txt"
#define DUMP_BIN_FMT      L"\\??\\C:\\Users\\Public\\gw2_dump_pay_%d.bin"

#define OBS_PTE_MASK      0x8000000000000002ULL   // NX | RW
#define OBS_PTE_INVALID   0xFFFFFFFFFFFFFFFEULL   // PTE VA not readable
#define OBS_ITERS         600                     // 600 x 1s = 10 min
#define OBS_BINS_MAX      32

static pfn_MmCopyV DumpMmCopy()
{
    UNICODE_STRING u;
    RtlInitUnicodeString(&u, L"MmCopyVirtualMemory");
    return (pfn_MmCopyV)MmGetSystemRoutineAddress(&u);
}

// Write a buffer to a file. append=TRUE appends at EOF (text log);
// append=FALSE creates/truncates (raw snapshots). FILE_WRITE_THROUGH so a
// hard reset cannot eat already-completed dumps.
static NTSTATUS DumpWriteAll(PCWSTR path, PVOID data, ULONG len, BOOLEAN append)
{
    UNICODE_STRING uPath;
    RtlInitUnicodeString(&uPath, path);
    OBJECT_ATTRIBUTES oa;
    InitializeObjectAttributes(&oa, &uPath,
        OBJ_CASE_INSENSITIVE | OBJ_KERNEL_HANDLE, NULL, NULL);

    HANDLE h = nullptr;
    IO_STATUS_BLOCK iosb = {};
    NTSTATUS s = ZwCreateFile(&h,
        append ? (FILE_APPEND_DATA | SYNCHRONIZE) : (GENERIC_WRITE | SYNCHRONIZE),
        &oa, &iosb, NULL, FILE_ATTRIBUTE_NORMAL,
        FILE_SHARE_READ | FILE_SHARE_WRITE,
        append ? FILE_OPEN_IF : FILE_OVERWRITE_IF,
        FILE_WRITE_THROUGH | FILE_SYNCHRONOUS_IO_NONALERT | FILE_NON_DIRECTORY_FILE,
        NULL, 0);
    if (!NT_SUCCESS(s)) return s;

    LARGE_INTEGER offset;
    offset.QuadPart = append ? -1 : 0;
    s = ZwWriteFile(h, NULL, NULL, NULL, &iosb, data, len, &offset, NULL);
    ZwClose(h);
    return s;
}

static ULONG DumpFnv(const UCHAR* p, SIZE_T n)
{
    ULONG h = 2166136261u;
    for (SIZE_T i = 0; i < n; i++) { h ^= p[i]; h *= 16777619u; }
    return h;
}

// Facts from one region walk — the same walk a VirtualQuery scanner does.
typedef struct _OBS_ACC {
    BOOLEAN  payFound;
    ULONG64  payRegionBase;
    ULONG64  payRegionSize;
    ULONG    payState;
    ULONG    payProtect;
    ULONG    payType;
    LONG     regions;
    LONG     privExec;
    ULONG64  privExecBytes;
    NTSTATUS walkStatus;
} OBS_ACC;

// Caller must be attached to the target. Facts only — no text.
static VOID ObsWalk(pfn_ZwQVM QVM, OBS_ACC* acc)
{
    RtlZeroMemory(acc, sizeof *acc);
    acc->walkStatus = STATUS_SUCCESS;

    ULONG64 addr = 0x10000;
    const ULONG64 top = 0x00007FF000000000ULL;
    for (LONG guard = 0; guard < 500000 && addr < top; guard++) {
        MBI_X64 m;
        RtlZeroMemory(&m, sizeof m);
        SIZE_T rl = 0;
        NTSTATUS s = QVM(ZwCurrentProcess(), (PVOID)addr, 0, &m, sizeof m, &rl);
        if (!NT_SUCCESS(s)) { acc->walkStatus = s; break; }
        if (m.RegionSize == 0) { acc->walkStatus = STATUS_UNSUCCESSFUL; break; }

        ULONG64 rb = (ULONG64)(ULONG_PTR)m.BaseAddress;
        acc->regions++;
        if (m.State != DUMP_MEM_FREE) {
            // private + any execute protection = scanner bait
            if ((m.Type & DUMP_MEM_PRIVATE) && (m.Protect & 0xF0)) {
                acc->privExec++;
                acc->privExecBytes += m.RegionSize;
            }
            if (g_payBase
                && (ULONG64)(ULONG_PTR)g_payBase >= rb
                && (ULONG64)(ULONG_PTR)g_payBase <  rb + m.RegionSize) {
                acc->payFound = TRUE;
                acc->payRegionBase = rb;
                acc->payRegionSize = m.RegionSize;
                acc->payState = m.State;
                acc->payProtect = m.Protect;
                acc->payType = m.Type;
            }
        }
        addr = rb + m.RegionSize;
    }
}

// Caller must be attached. V lines into p/rem.
static VOID ObsWalkText(pfn_ZwQVM QVM, char** p, SIZE_T* rem)
{
    ULONG64 addr = 0x10000;
    const ULONG64 top = 0x00007FF000000000ULL;
    for (LONG guard = 0; guard < 500000 && addr < top && *rem > 256; guard++) {
        MBI_X64 m;
        RtlZeroMemory(&m, sizeof m);
        SIZE_T rl = 0;
        NTSTATUS s = QVM(ZwCurrentProcess(), (PVOID)addr, 0, &m, sizeof m, &rl);
        if (!NT_SUCCESS(s)) {
            RtlStringCbPrintfExA(*p, *rem, p, rem, 0,
                "V WALK_ERR %016llX %08lX\r\n", addr, (ULONG)s);
            break;
        }
        if (m.RegionSize == 0) {
            RtlStringCbPrintfExA(*p, *rem, p, rem, 0,
                "V WALK_ZERO %016llX\r\n", addr);
            break;
        }
        if (m.State != DUMP_MEM_FREE) {
            RtlStringCbPrintfExA(*p, *rem, p, rem, 0,
                "V %016llX %08llX S=%lX P=%lX T=%lX\r\n",
                (unsigned long long)(ULONG_PTR)m.BaseAddress,
                (unsigned long long)m.RegionSize,
                m.State, m.Protect, m.Type);
        }
        addr = (unsigned long long)(ULONG_PTR)m.BaseAddress + m.RegionSize;
    }
}

// Full snapshot (baseline pre-inject, and first post-inject): region walk +
// every payload PTE + every payload page hash. idx<0 = no payload yet.
VOID DumpSnapshot(PEPROCESS target, int idx, const char* tag)
{
    if (KeGetCurrentIrql() != PASSIVE_LEVEL) return;

    UNICODE_STRING uQvm;
    RtlInitUnicodeString(&uQvm, L"ZwQueryVirtualMemory");
    pfn_ZwQVM QVM = (pfn_ZwQVM)MmGetSystemRoutineAddress(&uQvm);
    if (!QVM) { KLog("dump: ZwQueryVirtualMemory unavailable"); return; }

    const SIZE_T bufSz = 2 * 1024 * 1024;
    char* buf = (char*)ExAllocatePool2(POOL_FLAG_NON_PAGED, bufSz, 'PMuD');
    if (!buf) { KLog("dump: text buffer alloc failed"); return; }

    char*  p   = buf;
    SIZE_T rem = bufSz;
    RtlStringCbPrintfExA(p, rem, &p, &rem, 0,
        "==== DUMP %d [%s] payload=0x%llX size=0x%llX ====\r\n",
        idx, tag ? tag : "?",
        (unsigned long long)(ULONG_PTR)g_payBase,
        (unsigned long long)g_paySize);

    OBS_ACC acc;
    RtlZeroMemory(&acc, sizeof acc);

    KAPC_STATE apc;
    KeStackAttachProcess((PRKPROCESS)target, &apc);
    {
        ObsWalkText(QVM, &p, &rem);
        ObsWalk(QVM, &acc);

        // PTE bits. Guarded: if EAAC unmapped the region, its page tables
        // may be gone and a raw self-map read would bugcheck — the very
        // reset we are trying to observe.
        if (g_payBase && g_paySize) {
            ULONG_PTR pteBase = GetPteBase();
            if (pteBase) {
                for (ULONG64 off = 0; off < g_paySize && rem > 256; off += 0x1000) {
                    ULONG64 va = (ULONG64)(ULONG_PTR)g_payBase + off;
                    ULONG64 pva = PteAddrOf(va, pteBase);
                    ULONG64 pteV = OBS_PTE_INVALID;
                    if (MmIsAddressValid((PVOID)pva))
                        pteV = *(volatile ULONG64*)pva;
                    RtlStringCbPrintfExA(p, rem, &p, &rem, 0,
                        "P %06llX %016llX\r\n", off, pteV);
                }
            } else {
                RtlStringCbPrintfExA(p, rem, &p, &rem, 0, "P NO_PTEBASE\r\n");
            }
        }
    }
    KeUnstackDetachProcess(&apc);

    // Full-page content hashes (MmCopy: probe-safe; failed pages hash 0).
    if (g_payBase && g_paySize) {
        pfn_MmCopyV mc = DumpMmCopy();
        UCHAR* stage = (UCHAR*)ExAllocatePool2(POOL_FLAG_NON_PAGED, 0x1000, 'PMuD');
        if (mc && stage) {
            for (ULONG64 off = 0; off < g_paySize && rem > 256; off += 0x1000) {
                SIZE_T rd = 0;
                NTSTATUS s = mc(target,
                    (PVOID)((ULONG64)(ULONG_PTR)g_payBase + off),
                    PsInitialSystemProcess, stage, 0x1000, KernelMode, &rd);
                ULONG h = (NT_SUCCESS(s) && rd == 0x1000)
                        ? DumpFnv(stage, 0x1000) : 0;
                RtlStringCbPrintfExA(p, rem, &p, &rem, 0,
                    "C %06llX %08lX %08lX\r\n", off, h, (ULONG)s);
            }
        }
        if (stage) ExFreePool(stage);
    }

    RtlStringCbPrintfExA(p, rem, &p, &rem, 0,
        "SUMMARY regions=%ld privExec=%ld privExecBytes=%016llX walk=%08lX\r\n",
        acc.regions, acc.privExec,
        (unsigned long long)acc.privExecBytes, (ULONG)acc.walkStatus);
    RtlStringCbPrintfExA(p, rem, &p, &rem, 0, "==== END DUMP ====\r\n");

    NTSTATUS ws = DumpWriteAll(DUMP_TEXT_PATH, buf, (ULONG)(bufSz - rem), TRUE);
    if (!NT_SUCCESS(ws)) { KLogHex("dump: text write failed: ", ws); }
    ExFreePool(buf);

    char line[128];
    RtlStringCbPrintfA(line, sizeof line,
        "observation dump %d done: regions=%ld privExec=%ld",
        idx, acc.regions, acc.privExec);
    KLog(line);
}

// Raw payload-region bytes to gw2_dump_pay_<idx>.bin (page-by-page MmCopy so
// a single invalid page cannot kill the whole dump; failed pages read 0).
static VOID DumpPayloadRaw(PEPROCESS target, int idx)
{
    if (!g_payBase || !g_paySize) return;
    if (KeGetCurrentIrql() != PASSIVE_LEVEL) return;

    pfn_MmCopyV mc = DumpMmCopy();
    if (!mc) return;

    PVOID buf = ExAllocatePool2(POOL_FLAG_NON_PAGED, g_paySize, 'PMuD');
    if (!buf) { KLog("dump: raw buffer alloc failed"); return; }
    RtlZeroMemory(buf, g_paySize);

    LONG okPages = 0;
    LONG totalPages = (LONG)(g_paySize >> 12);
    for (ULONG64 off = 0; off < g_paySize; off += 0x1000) {
        SIZE_T rd = 0;
        NTSTATUS s = mc(target,
            (PVOID)((ULONG64)(ULONG_PTR)g_payBase + off),
            PsInitialSystemProcess, (PVOID)((UCHAR*)buf + off),
            0x1000, KernelMode, &rd);
        if (NT_SUCCESS(s)) okPages++;
    }

    WCHAR path[96];
    RtlStringCbPrintfW(path, sizeof path, DUMP_BIN_FMT, idx);
    NTSTATUS ws = DumpWriteAll(path, buf, (ULONG)g_paySize, FALSE);
    ExFreePool(buf);

    if (!NT_SUCCESS(ws)) { KLogHex("dump: raw write failed: ", ws); return; }

    char line[96];
    RtlStringCbPrintfA(line, sizeof line,
        "raw dump %d: %d/%d pages read", idx, okPages, totalPages);
    KLog(line);
}

// Compact event dump: only what changed since the previous second.
static VOID ObsWriteEvent(PEPROCESS target, pfn_ZwQVM QVM, LONG it,
    const OBS_ACC* acc, const OBS_ACC* prev,
    BOOLEAN chgRegion, BOOLEAN chgPte, BOOLEAN chgChk,
    const ULONG64* pte, const ULONG64* ptePrev,
    const ULONG* chk, const ULONG* chkPrev, LONG pages, int binIdx)
{
    const SIZE_T bufSz = 1024 * 1024;
    char* buf = (char*)ExAllocatePool2(POOL_FLAG_NON_PAGED, bufSz, 'PMuD');
    if (!buf) { KLog("dump: event buffer alloc failed"); return; }

    char*  p   = buf;
    SIZE_T rem = bufSz;
    RtlStringCbPrintfExA(p, rem, &p, &rem, 0,
        "==== EVENT it=%ld cat:%s%s%s bin=%d ====\r\n",
        it, chgRegion ? " REGION" : "", chgPte ? " PTE" : "",
        chgChk ? " CONTENT" : "", binIdx);

    RtlStringCbPrintfExA(p, rem, &p, &rem, 0,
        "PAY found=%d base=%016llX size=%016llX S=%lX P=%lX T=%lX\r\n",
        (int)acc->payFound,
        (unsigned long long)acc->payRegionBase,
        (unsigned long long)acc->payRegionSize,
        acc->payState, acc->payProtect, acc->payType);
    if (chgRegion) {
        RtlStringCbPrintfExA(p, rem, &p, &rem, 0,
            "PAYprev found=%d base=%016llX size=%016llX S=%lX P=%lX T=%lX\r\n",
            (int)prev->payFound,
            (unsigned long long)prev->payRegionBase,
            (unsigned long long)prev->payRegionSize,
            prev->payState, prev->payProtect, prev->payType);
        RtlStringCbPrintfExA(p, rem, &p, &rem, 0,
            "SUMMARY regions=%ld privExec=%ld privExecBytes=%016llX\r\n",
            acc->regions, acc->privExec,
            (unsigned long long)acc->privExecBytes);
        KAPC_STATE apc;
        KeStackAttachProcess((PRKPROCESS)target, &apc);
        ObsWalkText(QVM, &p, &rem);
        KeUnstackDetachProcess(&apc);
    }

    if (chgPte) {
        for (LONG i = 0; i < pages && rem > 128; i++) {
            if (((pte[i] ^ ptePrev[i]) & OBS_PTE_MASK) == 0) continue;
            RtlStringCbPrintfExA(p, rem, &p, &rem, 0,
                "PE %06llX %016llX -> %016llX\r\n",
                (unsigned long long)((ULONG64)i << 12),
                (unsigned long long)ptePrev[i],
                (unsigned long long)pte[i]);
        }
    }
    if (chgChk) {
        for (LONG i = 0; i < pages && rem > 128; i++) {
            if (chk[i] == chkPrev[i]) continue;
            RtlStringCbPrintfExA(p, rem, &p, &rem, 0,
                "CE %06llX %08lX -> %08lX\r\n",
                (unsigned long long)((ULONG64)i << 12),
                chkPrev[i], chk[i]);
        }
    }
    RtlStringCbPrintfExA(p, rem, &p, &rem, 0, "==== END EVENT ====\r\n");

    NTSTATUS ws = DumpWriteAll(DUMP_TEXT_PATH, buf, (ULONG)(bufSz - rem), TRUE);
    if (!NT_SUCCESS(ws)) { KLogHex("dump: event write failed: ", ws); }
    ExFreePool(buf);
}

// 1s polling for 10 minutes; writes only on change (plus heartbeat).
VOID RunObservationDumps(PEPROCESS target)
{
    if (KeGetCurrentIrql() != PASSIVE_LEVEL) return;
    if (!g_payBase || !g_paySize) {
        KLog("observation: payload not captured - nothing to watch");
        return;
    }

    UNICODE_STRING uQvm;
    RtlInitUnicodeString(&uQvm, L"ZwQueryVirtualMemory");
    pfn_ZwQVM QVM = (pfn_ZwQVM)MmGetSystemRoutineAddress(&uQvm);
    pfn_MmCopyV mc = DumpMmCopy();
    if (!QVM || !mc) { KLog("observation: QVM or MmCopy unavailable"); return; }

    const LONG pages = (LONG)((g_paySize + 0xFFFULL) >> 12);
    if (pages <= 0 || pages > 8192) {
        KLog("observation: page count out of range");
        return;
    }

    ULONG64* pte     = (ULONG64*)ExAllocatePool2(POOL_FLAG_NON_PAGED, (SIZE_T)pages * 8, 'PMuD');
    ULONG64* ptePrev = (ULONG64*)ExAllocatePool2(POOL_FLAG_NON_PAGED, (SIZE_T)pages * 8, 'PMuD');
    ULONG*   chk     = (ULONG*)ExAllocatePool2(POOL_FLAG_NON_PAGED, (SIZE_T)pages * 4, 'PMuD');
    ULONG*   chkPrev = (ULONG*)ExAllocatePool2(POOL_FLAG_NON_PAGED, (SIZE_T)pages * 4, 'PMuD');
    UCHAR*   stage   = (UCHAR*)ExAllocatePool2(POOL_FLAG_NON_PAGED, 0x1000, 'PMuD');
    if (!pte || !ptePrev || !chk || !chkPrev || !stage) {
        if (pte) ExFreePool(pte);
        if (ptePrev) ExFreePool(ptePrev);
        if (chk) ExFreePool(chk);
        if (chkPrev) ExFreePool(chkPrev);
        if (stage) ExFreePool(stage);
        KLog("observation: buffer alloc failed");
        return;
    }

    KLog("observation: 1s polling for 600s - change-triggered dumps");

    OBS_ACC prevAcc;
    RtlZeroMemory(&prevAcc, sizeof prevAcc);
    BOOLEAN havePrev = FALSE;
    int     binCount = 0;

    for (LONG it = 0; it < OBS_ITERS; it++) {
        if (PsGetProcessExitStatus(target) != STATUS_PENDING) {
            char line[96];
            RtlStringCbPrintfA(line, sizeof line,
                "observation: target exited at it=%ld", it);
            KLog(line);
            break;
        }

        // ---- gather this second's facts ----
        OBS_ACC acc;
        {
            KAPC_STATE apc;
            KeStackAttachProcess((PRKPROCESS)target, &apc);
            ObsWalk(QVM, &acc);

            ULONG_PTR pteBase = GetPteBase();
            for (LONG i = 0; i < pages; i++) {
                pte[i] = OBS_PTE_INVALID;
                if (!acc.payFound || !pteBase) continue;
                ULONG64 va = (ULONG64)(ULONG_PTR)g_payBase + ((ULONG64)i << 12);
                ULONG64 pva = PteAddrOf(va, pteBase);
                if (MmIsAddressValid((PVOID)pva))
                    pte[i] = *(volatile ULONG64*)pva;
            }
            KeUnstackDetachProcess(&apc);
        }

        for (LONG i = 0; i < pages; i++) {
            chk[i] = 0;
            SIZE_T rd = 0;
            NTSTATUS s = mc(target,
                (PVOID)((ULONG64)(ULONG_PTR)g_payBase + ((ULONG64)i << 12)),
                PsInitialSystemProcess, stage, 0x1000, KernelMode, &rd);
            if (NT_SUCCESS(s) && rd == 0x1000)
                chk[i] = DumpFnv(stage, 0x1000);
        }

        // ---- compare & write ----
        if (!havePrev) {
            DumpSnapshot(target, 0, "first");
            if (binCount < OBS_BINS_MAX) DumpPayloadRaw(target, binCount++);
            KLog("observation: first snapshot + raw dump written");
        } else {
            BOOLEAN chgRegion =
                   (acc.payFound      != prevAcc.payFound)
                || (acc.payRegionSize != prevAcc.payRegionSize)
                || (acc.payState      != prevAcc.payState)
                || (acc.payProtect    != prevAcc.payProtect)
                || (acc.payType       != prevAcc.payType)
                || (acc.privExec      != prevAcc.privExec)
                || (acc.privExecBytes != prevAcc.privExecBytes);
            BOOLEAN chgPte = FALSE;
            BOOLEAN chgChk = FALSE;
            for (LONG i = 0; i < pages; i++) {
                if (((pte[i] ^ ptePrev[i]) & OBS_PTE_MASK) != 0) chgPte = TRUE;
                if ((i < g_payMutCount)
                    && ((g_payMut[i >> 6] >> (i & 63)) & 1)) continue;
                if (chk[i] != chkPrev[i]) chgChk = TRUE;
            }

            if (chgRegion || chgPte || chgChk) {
                int binIdx = -1;
                if (chgChk && binCount < OBS_BINS_MAX) {
                    binIdx = binCount;
                    DumpPayloadRaw(target, binCount++);
                }
                ObsWriteEvent(target, QVM, it, &acc, &prevAcc,
                    chgRegion, chgPte, chgChk,
                    pte, ptePrev, chk, chkPrev, pages, binIdx);
                char line[128];
                RtlStringCbPrintfA(line, sizeof line,
                    "observation: EVENT it=%ld region=%d pte=%d content=%d",
                    it, (int)chgRegion, (int)chgPte, (int)chgChk);
                KLog(line);
            } else if (it % 30 == 0) {
                char line[160];
                RtlStringCbPrintfA(line, sizeof line,
                    "observation: heartbeat it=%ld regions=%ld privExec=%ld payFound=%d P=%lX",
                    it, acc.regions, acc.privExec,
                    (int)acc.payFound, acc.payProtect);
                KLog(line);
            }
        }

        // ---- this second becomes the previous ----
        RtlCopyMemory(ptePrev, pte, (SIZE_T)pages * 8);
        RtlCopyMemory(chkPrev, chk, (SIZE_T)pages * 4);
        prevAcc = acc;
        havePrev = TRUE;

        LARGE_INTEGER t;
        t.QuadPart = -10000000LL;  // 1 s
        KeDelayExecutionThread(KernelMode, FALSE, &t);
    }

    if (PsGetProcessExitStatus(target) == STATUS_PENDING
        && binCount < OBS_BINS_MAX) {
        DumpPayloadRaw(target, binCount++);
        KLog("observation: final raw dump written");
    }

    // Late image dump: catches pages Arxan only decrypts on demand once the
    // game is actually being played (boot dump 0 has the boot-decrypted set).
    if (PsGetProcessExitStatus(target) == STATUS_PENDING) {
        DumpGameImage(target, 1);
    }

    KLog("observation: loop complete");
    ExFreePool(pte);
    ExFreePool(ptePrev);
    ExFreePool(chk);
    ExFreePool(chkPrev);
    ExFreePool(stage);
}

// ===========================================================================
// GAME IMAGE DUMP — decrypted runtime image for offline RE (Ghidra)
// ===========================================================================
// Arxan ships the exe encrypted on disk (.text hollow; .srdata/.ytext/.xtls
// at entropy 8.00), so a static import of GW2.Main_Win64_Retail.exe yields
// no code. In memory the image is decrypted. This finds the module through
// the target's PEB and copies the FULL image [DllBase, DllBase+SizeOfImage):
//
//   C:\Users\Public\gw2_image_dump_<idx>.bin   linear raw image at runtime VAs
//   C:\Users\Public\gw2_image_manifest.txt     base/size/section map/page stats
//
// Every page is a separate MmCopyVirtualMemory (the DumpPayloadRaw pattern,
// proven against EAAC): one unreadable page becomes 4 KB of zeros and is
// counted in the manifest — it can never kill the dump. The runtime IAT in
// .idata comes along resolved, which is what makes the network stack findable
// offline.
//
// Ghidra: Import File → gw2_image_dump_<idx>.bin → raw binary, language
// x86:LE:64:default, base = runtime base from the manifest. Sections then sit
// at their true runtime VAs with decrypted content.
// ===========================================================================

#define IMG_BIN_FMT   L"\\??\\C:\\Users\\Public\\gw2_image_dump_%d.bin"
#define IMG_MANIFEST  L"\\??\\C:\\Users\\Public\\gw2_image_manifest.txt"
#define IMG_CHUNK     (1024 * 1024)            // 1 MB per ZwWriteFile
#define IMG_MAX_SZ    (256ULL * 1024 * 1024)   // sanity cap
#define IMG_HDR_MIN   0x2000                   // first header read
#define IMG_SECS_MAX  96

// Open a dump file for sequential overwrite (fresh each launch).
static NTSTATUS ImgOpen(PCWSTR path, HANDLE* out)
{
    UNICODE_STRING u;
    RtlInitUnicodeString(&u, path);
    OBJECT_ATTRIBUTES oa;
    InitializeObjectAttributes(&oa, &u,
        OBJ_CASE_INSENSITIVE | OBJ_KERNEL_HANDLE, NULL, NULL);
    IO_STATUS_BLOCK iosb = {};
    return ZwCreateFile(out,
        GENERIC_WRITE | SYNCHRONIZE, &oa, &iosb, NULL, FILE_ATTRIBUTE_NORMAL,
        FILE_SHARE_READ, FILE_OVERWRITE_IF,
        FILE_WRITE_THROUGH | FILE_SYNCHRONOUS_IO_NONALERT | FILE_NON_DIRECTORY_FILE,
        NULL, 0);
}

// Write one chunk at the running offset; advance on success.
static NTSTATUS ImgWriteChunk(HANDLE h, PVOID data, ULONG len, LARGE_INTEGER* pos)
{
    IO_STATUS_BLOCK iosb = {};
    NTSTATUS s = ZwWriteFile(h, NULL, NULL, NULL, &iosb, data, len, pos, NULL);
    if (NT_SUCCESS(s)) pos->QuadPart += len;
    return s;
}

VOID DumpGameImage(PEPROCESS target, int idx)
{
    if (KeGetCurrentIrql() != PASSIVE_LEVEL) return;

    pfn_MmCopyV mc = DumpMmCopy();
    if (!mc) { KLog("imgdump: MmCopyVirtualMemory unavailable"); return; }

    // ---- locate the module through the PEB (attach-held, proven pattern) ----
    PVOID base = nullptr;
    {
        KAPC_STATE apc;
        KeStackAttachProcess((PRKPROCESS)target, &apc);
        base = GetUserModuleBase(GW2_IMAGE_MODULE);
        KeUnstackDetachProcess(&apc);
    }
    if (!base) { KLog("imgdump: module not found in PEB"); return; }

    // ---- pull the PE headers cross-process (probe-safe, faults pages in) ----
    UCHAR* hdr = (UCHAR*)ExAllocatePool2(POOL_FLAG_NON_PAGED, IMG_HDR_MIN, 'GMuD');
    if (!hdr) { KLog("imgdump: header alloc failed"); return; }
    SIZE_T rd = 0;
    NTSTATUS s = mc(target, base, PsInitialSystemProcess, hdr, IMG_HDR_MIN,
                    KernelMode, &rd);
    if (!NT_SUCCESS(s) || rd != IMG_HDR_MIN) {
        ExFreePool(hdr);
        KLogHex("imgdump: header copy failed: ", s);
        return;
    }

    USHORT e_lfanew = *(USHORT*)(hdr + 0x3C);
    if (*(USHORT*)hdr != IMAGE_DOS_SIGNATURE
        || e_lfanew < sizeof(IMAGE_DOS_HEADER)
        || e_lfanew >= IMG_HDR_MIN) {
        ExFreePool(hdr);
        KLog("imgdump: bad DOS header");
        return;
    }
    auto nth = (PIMAGE_NT_HEADERS64)(hdr + e_lfanew);
    if (nth->Signature != IMAGE_NT_SIGNATURE) {
        ExFreePool(hdr);
        KLog("imgdump: bad PE signature");
        return;
    }

    LONG     nSecs       = nth->FileHeader.NumberOfSections;
    ULONG64  sizeOfImage = nth->OptionalHeader.SizeOfImage;

    // Section table may straddle the first 2 pages: re-copy enough for it.
    SIZE_T need = e_lfanew + sizeof(IMAGE_NT_HEADERS64)
                + (SIZE_T)nSecs * sizeof(IMAGE_SECTION_HEADER);
    if (need > IMG_HDR_MIN) {
        UCHAR* big = (UCHAR*)ExAllocatePool2(POOL_FLAG_NON_PAGED, need, 'GMuD');
        if (!big) { ExFreePool(hdr); KLog("imgdump: big header alloc failed"); return; }
        ExFreePool(hdr);
        hdr = big;
        s = mc(target, base, PsInitialSystemProcess, hdr, (ULONG)need,
               KernelMode, &rd);
        if (!NT_SUCCESS(s) || rd != need) {
            ExFreePool(hdr);
            KLogHex("imgdump: big header copy failed: ", s);
            return;
        }
        nth = (PIMAGE_NT_HEADERS64)(hdr + e_lfanew);
    }

    if (nSecs < 1 || nSecs > IMG_SECS_MAX
        || sizeOfImage < 0x1000 || sizeOfImage > IMG_MAX_SZ) {
        ExFreePool(hdr);
        KLog("imgdump: sanity check failed (sections/image size)");
        return;
    }

    // Copy the section table out before freeing the header buffer.
    struct { char name[9]; ULONG va; ULONG vsize; ULONG chars; } secs[IMG_SECS_MAX];
    {
        auto src = IMAGE_FIRST_SECTION(nth);
        for (LONG i = 0; i < nSecs; i++) {
            RtlCopyMemory(secs[i].name, src[i].Name, 8);
            secs[i].name[8] = '\0';
            secs[i].va    = src[i].VirtualAddress;
            secs[i].vsize = src[i].Misc.VirtualSize;
            secs[i].chars = src[i].Characteristics;
        }
    }
    ExFreePool(hdr);

    // ---- page-by-page image copy into a 1 MB write buffer ----
    HANDLE h = nullptr;
    WCHAR path[96];
    RtlStringCbPrintfW(path, sizeof path, IMG_BIN_FMT, idx);
    s = ImgOpen(path, &h);
    if (!NT_SUCCESS(s)) { KLogHex("imgdump: open failed: ", s); return; }

    UCHAR* chunk = (UCHAR*)ExAllocatePool2(POOL_FLAG_NON_PAGED, IMG_CHUNK, 'GMuD');
    UCHAR* page  = (UCHAR*)ExAllocatePool2(POOL_FLAG_NON_PAGED, 0x1000, 'GMuD');
    if (!chunk || !page) {
        if (chunk) ExFreePool(chunk);
        if (page)  ExFreePool(page);
        ZwClose(h);
        KLog("imgdump: buffer alloc failed");
        return;
    }

    ULONG64 total = (sizeOfImage + 0xFFFULL) & ~0xFFFULL;
    LONG totalPages = (LONG)(total >> 12);
    LONG okPages = 0, badPages = 0;
    ULONG chunkUsed = 0;
    LARGE_INTEGER pos; pos.QuadPart = 0;
    NTSTATUS wfail = STATUS_SUCCESS;

    for (ULONG64 off = 0; off < total && NT_SUCCESS(wfail); off += 0x1000) {
        rd = 0;
        NTSTATUS cs = mc(target, (PVOID)((ULONG64)(ULONG_PTR)base + off),
                         PsInitialSystemProcess, page, 0x1000, KernelMode, &rd);
        if (NT_SUCCESS(cs) && rd == 0x1000) {
            RtlCopyMemory(chunk + chunkUsed, page, 0x1000);
            okPages++;
        } else {
            RtlZeroMemory(chunk + chunkUsed, 0x1000);   // hole, counted below
            badPages++;
        }
        chunkUsed += 0x1000;
        if (chunkUsed == IMG_CHUNK) {
            wfail = ImgWriteChunk(h, chunk, chunkUsed, &pos);
            chunkUsed = 0;
        }
    }
    if (chunkUsed && NT_SUCCESS(wfail))
        wfail = ImgWriteChunk(h, chunk, chunkUsed, &pos);

    ZwClose(h);
    ExFreePool(chunk);
    ExFreePool(page);

    // ---- manifest: everything Ghidra needs to place the dump ----
    const SIZE_T mbSz = 64 * 1024;
    char* mb = (char*)ExAllocatePool2(POOL_FLAG_NON_PAGED, mbSz, 'GMuD');
    if (mb) {
        char*  p   = mb;
        SIZE_T rem = mbSz;
        RtlStringCbPrintfExA(p, rem, &p, &rem, 0,
            "==== GAME IMAGE DUMP %d ====\r\n", idx);
        RtlStringCbPrintfExA(p, rem, &p, &rem, 0,
            "base     %016llX\r\n",
            (unsigned long long)(ULONG_PTR)base);
        RtlStringCbPrintfExA(p, rem, &p, &rem, 0,
            "image    %016llX (%llu bytes)\r\n",
            (unsigned long long)sizeOfImage,
            (unsigned long long)sizeOfImage);
        RtlStringCbPrintfExA(p, rem, &p, &rem, 0,
            "sections %ld\r\n", nSecs);
        RtlStringCbPrintfExA(p, rem, &p, &rem, 0,
            "pages    ok=%ld bad=%ld total=%ld\r\n",
            okPages, badPages, totalPages);
        RtlStringCbPrintfExA(p, rem, &p, &rem, 0,
            "file     gw2_image_dump_%d.bin (%llu bytes)\r\n",
            idx, (unsigned long long)total);
        RtlStringCbPrintfExA(p, rem, &p, &rem, 0,
            "ghidra   Import File -> raw binary -> x86:LE:64:default -> base %016llX\r\n"
            "         (relocations are already applied at this base)\r\n",
            (unsigned long long)(ULONG_PTR)base);
        for (LONG i = 0; i < nSecs && rem > 128; i++) {
            RtlStringCbPrintfExA(p, rem, &p, &rem, 0,
                "SEC %-8s va=%08lX vsz=%08lX chars=%08lX\r\n",
                secs[i].name, secs[i].va, secs[i].vsize, secs[i].chars);
        }
        RtlStringCbPrintfExA(p, rem, &p, &rem, 0, "==== END ====\r\n");

        NTSTATUS ws = DumpWriteAll(IMG_MANIFEST, mb, (ULONG)(mbSz - rem), TRUE);
        if (!NT_SUCCESS(ws)) KLogHex("imgdump: manifest write failed: ", ws);
        ExFreePool(mb);
    }

    char line[128];
    RtlStringCbPrintfA(line, sizeof line,
        "imgdump %d: base=%016llX size=%llX ok=%ld bad=%ld %s",
        idx, (unsigned long long)(ULONG_PTR)base,
        (unsigned long long)total, okPages, badPages,
        NT_SUCCESS(wfail) ? "written" : "WRITE-FAILED");
    KLog(line);

    if (NT_SUCCESS(wfail)) BeepImageDumped();
}

// ---------------------------------------------------------------------------
// DumpThreadRips — kernel-side thread inventory for the target process.
//
// ZwQuerySystemInformation(SystemProcessInformation) returns StartAddress,
// ThreadState and WaitReason for every thread WITHOUT opening thread handles,
// which sidesteps the user-mode GetThreadContext block entirely. StartAddress
// is where the thread began; for the EAAC-gate hunt the interesting part is
// which threads exist, their start addresses inside the game module, and
// their wait state during the "Connecting to EA servers" hang.
// ---------------------------------------------------------------------------
VOID DumpThreadRips(PEPROCESS target)
{
    if (KeGetCurrentIrql() != PASSIVE_LEVEL) return;

    ULONG need = 0;
    ZwQuerySystemInformation(5, nullptr, 0, &need);
    if (!need) { KLog("thrd: size query failed"); return; }

    ULONG bufSz = need + 0x10000;
    PSYSTEM_PROCESS_INFORMATION spi =
        (PSYSTEM_PROCESS_INFORMATION)ExAllocatePool2(POOL_FLAG_NON_PAGED, bufSz, 'PMuD');
    if (!spi) { KLog("thrd: alloc failed"); return; }

    NTSTATUS s = ZwQuerySystemInformation(5, spi, bufSz, &need);
    if (!NT_SUCCESS(s)) {
        ExFreePool(spi);
        KLogHex("thrd: query failed: ", s);
        return;
    }

    HANDLE wantPid = PsGetProcessId(target);

    // Game module base for offset resolution (same attach pattern as imgdump).
    PVOID modBase = nullptr;
    {
        KAPC_STATE apc;
        KeStackAttachProcess((PRKPROCESS)target, &apc);
        modBase = GetUserModuleBase(GW2_IMAGE_MODULE);
        KeUnstackDetachProcess(&apc);
    }
    ULONG64 lo = (ULONG64)(ULONG_PTR)modBase;
    ULONG64 hi = lo + 0x3A00000ULL; // SizeOfImage

    char hdr[96];
    RtlStringCbPrintfA(hdr, sizeof hdr,
        "==== THREADS pid=%llu base=%016llX ====",
        (unsigned long long)(ULONG_PTR)wantPid,
        (unsigned long long)lo);
    KLog(hdr);

    PSYSTEM_PROCESS_INFORMATION e = spi;
    for (;;) {
        if (e->UniqueProcessId == wantPid) {
            for (ULONG i = 0; i < e->NumberOfThreads; i++) {
                SYSTEM_THREAD_INFORMATION* t = &e->Threads[i];

                // Resolve ETHREAD and read the saved user trap frame —
                // the thread's REAL parked RIP (same pattern as HijackThread).
                PETHREAD th = nullptr;
                ULONG64 rip = 0;
                if (NT_SUCCESS(PsLookupThreadByThreadId(t->ClientId.UniqueThread, &th))) {
                    PKTRAP_FRAME frame =
                        *(PKTRAP_FRAME*)((PUCHAR)th + 0x090); // KTHREAD::TrapFrame
                    if (frame && MmIsAddressValid(frame) && MmIsAddressValid(&frame->Rip)) {
                        ULONG64 r = frame->Rip;
                        if (r != 0 && r < 0x00007FFFFFFF0000ULL) rip = r;
                    }
                    ObDereferenceObject(th);
                }

                char line[176];
                if (rip && modBase && rip >= lo && rip < hi) {
                    RtlStringCbPrintfA(line, sizeof line,
                        "TID %6llu RIP GW2+0x%llX state=%lu wait=%lu",
                        (unsigned long long)(ULONG_PTR)t->ClientId.UniqueThread,
                        (unsigned long long)(rip - lo),
                        t->ThreadState, t->WaitReason);
                } else if (rip) {
                    RtlStringCbPrintfA(line, sizeof line,
                        "TID %6llu RIP %016llX state=%lu wait=%lu",
                        (unsigned long long)(ULONG_PTR)t->ClientId.UniqueThread,
                        (unsigned long long)rip,
                        t->ThreadState, t->WaitReason);
                } else {
                    RtlStringCbPrintfA(line, sizeof line,
                        "TID %6llu RIP none state=%lu wait=%lu",
                        (unsigned long long)(ULONG_PTR)t->ClientId.UniqueThread,
                        t->ThreadState, t->WaitReason);
                }
                KLog(line);

                // Walk this thread's user stack for return addresses into the
                // game module — names the game function blocked in this wait.
                if (modBase) {
                    PETHREAD th2 = nullptr;
                    if (NT_SUCCESS(PsLookupThreadByThreadId(
                            t->ClientId.UniqueThread, &th2))) {
                        PKTRAP_FRAME f2 =
                            *(PKTRAP_FRAME*)((PUCHAR)th2 + 0x090);
                        if (f2 && MmIsAddressValid(f2)) {
                            ULONG64 rsp = f2->Rsp;
                            if (rsp && rsp < 0x00007FFFFFFF0000ULL) {
                                pfn_MmCopyV mc = DumpMmCopy();
                                if (mc) {
                                    ULONG64 stack[256];
                                    SIZE_T got = 0;
                                    SIZE_T want = sizeof(stack);
                                    if (rsp + want > 0x00007FFFFFFF0000ULL)
                                        want = (SIZE_T)(0x00007FFFFFFF0000ULL - rsp);
                                    NTSTATUS cs = mc(target, (PVOID)rsp,
                                        PsGetCurrentProcess(), stack, want,
                                        KernelMode, &got);
                                    if (NT_SUCCESS(cs) && got >= 8) {
                                        ULONG nq = (ULONG)(got / 8);
                                        ULONG shown = 0;
                                        for (ULONG k = 0; k < nq && shown < 8; k++) {
                                            ULONG64 v = stack[k];
                                            if (v >= lo && v < hi) {
                                                char ret_line[96];
                                                RtlStringCbPrintfA(ret_line, sizeof ret_line,
                                                    "    STACK[%llu] GW2+0x%llX",
                                                    (unsigned long long)k,
                                                    (unsigned long long)(v - lo));
                                                KLog(ret_line);
                                                shown++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        ObDereferenceObject(th2);
                    }
                }
            }
            break;
        }
        if (!e->NextEntryOffset) break;
        e = (PSYSTEM_PROCESS_INFORMATION)((PUCHAR)e + e->NextEntryOffset);
    }

    KLog("==== THREADS END ====");
    ExFreePool(spi);
}
