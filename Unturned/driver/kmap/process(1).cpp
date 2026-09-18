#include "process.hpp"
#include "nt_structs.hpp"
#include "klog.hpp"
#include <string.h>
#include <wchar.h>


// ---------------------------------------------------------------------------
// Process enumeration via ZwQuerySystemInformation(5)
// PsGetNextProcess / PsGetNextProcessThread not exported on Win11 26200+
// ---------------------------------------------------------------------------

static PSYSTEM_PROCESS_INFORMATION QueryProcessList(PULONG outSize)
{
    ULONG needed = 0;
    ZwQuerySystemInformation(5, nullptr, 0, &needed);
    needed += 0x2000;

    auto buf = (PSYSTEM_PROCESS_INFORMATION)ExAllocatePool2(
        POOL_FLAG_NON_PAGED, needed, 'FMfn');
    if (!buf) return nullptr;

    NTSTATUS s = ZwQuerySystemInformation(5, buf, needed, &needed);
    if (!NT_SUCCESS(s)) { ExFreePool(buf); return nullptr; }

    if (outSize) *outSize = needed;
    return buf;
}

// ASCII name vs UNICODE_STRING image name comparison (case-insensitive)
static bool MatchProcessName(PUNICODE_STRING imgName, const char* name)
{
    if (!imgName->Buffer || !imgName->Length) return false;
    int wLen = imgName->Length / sizeof(WCHAR);
    int aLen = 0;
    for (; name[aLen]; aLen++);
    if (wLen != aLen) return false;
    for (int i = 0; i < aLen; i++) {
        WCHAR wc = imgName->Buffer[i];
        char  ac = name[i];
        if (wc >= L'A' && wc <= L'Z') wc |= 0x20;
        if (ac >= 'A'  && ac <= 'Z')  ac |= 0x20;
        if ((char)wc != ac) return false;
    }
    return true;
}

NTSTATUS FindProcessByName(const char* name, PEPROCESS* out)
{
    *out = nullptr;

    PSYSTEM_PROCESS_INFORMATION list = QueryProcessList(nullptr);
    if (!list) return STATUS_INSUFFICIENT_RESOURCES;

    NTSTATUS s = STATUS_NOT_FOUND;
    for (auto e = list; ; e = (PSYSTEM_PROCESS_INFORMATION)((PUCHAR)e + e->NextEntryOffset)) {
        if (MatchProcessName(&e->ImageName, name)) {
            s = PsLookupProcessByProcessId(e->UniqueProcessId, out);
            break;
        }
        if (!e->NextEntryOffset) break;
    }

    ExFreePool(list);
    return s;
}

// ---------------------------------------------------------------------------
// Export resolution
// ---------------------------------------------------------------------------

static int g_fwdDepth = 0;

static PVOID ResolveForward(const char* forward, ULONG_PTR /*modBase*/)
{
    if (g_fwdDepth >= 3) return nullptr;
    g_fwdDepth++;

    char dllName[64] = {};
    const char* funcName = nullptr;
    for (int i = 0; i < 63 && forward[i]; i++) {
        if (forward[i] == '.') { funcName = forward + i + 1; dllName[i] = '\0'; break; }
        dllName[i] = forward[i];
    }
    if (!funcName || !funcName[0]) { g_fwdDepth--; return nullptr; }

    WCHAR wDll[72] = {};
    int len = 0;
    for (int i = 0; dllName[i] && len < 63; i++) {
        WCHAR c = (WCHAR)(unsigned char)dllName[i];
        if (c >= L'A' && c <= L'Z') c |= 0x20;
        wDll[len++] = c;
    }
    if (len < 4 || wDll[len-4] != L'.' || wDll[len-3] != L'd' || wDll[len-2] != L'l' || wDll[len-1] != L'l') {
        wDll[len++] = L'.'; wDll[len++] = L'd'; wDll[len++] = L'l'; wDll[len++] = L'l';
    }
    wDll[len] = L'\0';

    PVOID targetMod = GetUserModuleBase(wDll);
    if (!targetMod) { g_fwdDepth--; return nullptr; }

    PVOID result = GetExportByName(targetMod, funcName);
    g_fwdDepth--;
    return result;
}

PVOID GetExportByName(PVOID moduleBase, const char* exportName)
{
    if (!moduleBase) return nullptr;
    auto dos = (PIMAGE_DOS_HEADER)moduleBase;
    if (dos->e_magic != IMAGE_DOS_SIGNATURE) return nullptr;
    auto nt  = (PIMAGE_NT_HEADERS64)((ULONG_PTR)moduleBase + dos->e_lfanew);
    if (nt->Signature != IMAGE_NT_SIGNATURE) return nullptr;

    auto& dir = nt->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT];
    if (!dir.VirtualAddress) return nullptr;

    auto exp    = (PIMAGE_EXPORT_DIRECTORY)((ULONG_PTR)moduleBase + dir.VirtualAddress);
    auto names  = (ULONG*)((ULONG_PTR)moduleBase + exp->AddressOfNames);
    auto ords   = (USHORT*)((ULONG_PTR)moduleBase + exp->AddressOfNameOrdinals);
    auto funcs  = (ULONG*)((ULONG_PTR)moduleBase + exp->AddressOfFunctions);
    ULONG_PTR expStart = (ULONG_PTR)moduleBase + dir.VirtualAddress;
    ULONG_PTR expEnd   = expStart + dir.Size;

    for (ULONG i = 0; i < exp->NumberOfNames; i++) {
        const char* n = (const char*)((ULONG_PTR)moduleBase + names[i]);
        if (_stricmp(n, exportName) != 0) continue;
        ULONG rva = funcs[ords[i]];
        ULONG_PTR addr = (ULONG_PTR)moduleBase + rva;
        if (addr >= expStart && addr < expEnd)
            return ResolveForward((const char*)addr, (ULONG_PTR)moduleBase);
        return (PVOID)addr;
    }
    return nullptr;
}

PVOID GetExportByOrdinal(PVOID moduleBase, ULONG ordinal)
{
    if (!moduleBase) return nullptr;
    auto dos = (PIMAGE_DOS_HEADER)moduleBase;
    if (dos->e_magic != IMAGE_DOS_SIGNATURE) return nullptr;
    auto nt  = (PIMAGE_NT_HEADERS64)((ULONG_PTR)moduleBase + dos->e_lfanew);
    if (nt->Signature != IMAGE_NT_SIGNATURE) return nullptr;

    auto& dir = nt->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT];
    if (!dir.VirtualAddress) return nullptr;

    auto exp   = (PIMAGE_EXPORT_DIRECTORY)((ULONG_PTR)moduleBase + dir.VirtualAddress);
    auto funcs = (ULONG*)((ULONG_PTR)moduleBase + exp->AddressOfFunctions);

    ULONG idx = ordinal - exp->Base;
    if (idx >= exp->NumberOfFunctions) return nullptr;
    ULONG rva = funcs[idx];
    if (!rva) return nullptr;
    return (PVOID)((ULONG_PTR)moduleBase + rva);
}

// ---------------------------------------------------------------------------
// Kernel module lookup
// ---------------------------------------------------------------------------

PVOID GetKernelModuleBase(const char* name)
{
    ULONG needed = 0;
    ZwQuerySystemInformation(11, nullptr, 0, &needed);
    needed += 0x200;

    auto buf = (PRTL_PROCESS_MODULES)ExAllocatePool2(
        POOL_FLAG_NON_PAGED, needed, 'FMfn');
    if (!buf) return nullptr;

    PVOID result = nullptr;
    if (NT_SUCCESS(ZwQuerySystemInformation(11, buf, needed, &needed))) {
        for (ULONG i = 0; i < buf->NumberOfModules; i++) {
            const char* full = (const char*)buf->Modules[i].FullPathName;
            const char* base = full + buf->Modules[i].OffsetToFileName;
            if (_stricmp(base, name) == 0) {
                result = buf->Modules[i].ImageBase;
                break;
            }
        }
    }
    ExFreePool(buf);
    return result;
}

// ---------------------------------------------------------------------------
// User-space module lookup — MUST be called while KeStackAttachProcess active
// ---------------------------------------------------------------------------

PVOID GetUserModuleBase(const WCHAR* name)
{
    // No SEH available (IFT registration triggers PatchGuard 0x109), so we
    // guard every user-space read explicitly. Attach is held by the caller,
    // so pages can't be freed under us — only paged out — and MmIsAddressValid
    // is a sufficient probe for that at PASSIVE_LEVEL.
    PVOID pebPtr = PsGetProcessPeb(PsGetCurrentProcess());
    if (!pebPtr || !MmIsAddressValid(pebPtr)) return nullptr;

    PVOID ldrSlot = (PVOID)((ULONG_PTR)pebPtr + 0x18);
    if (!MmIsAddressValid(ldrSlot)) return nullptr;
    PKMAP_PEB_LDR ldr = *(PKMAP_PEB_LDR*)ldrSlot;
    if (!ldr || !MmIsAddressValid(&ldr->InMemoryOrderModuleList)) return nullptr;

    PLIST_ENTRY head  = &ldr->InMemoryOrderModuleList;
    PLIST_ENTRY entry = head->Flink;
    ULONG guard = 0;

    while (entry && entry != head && guard++ < 512) {
        if (!MmIsAddressValid(entry)) return nullptr;
        auto e = CONTAINING_RECORD(entry, KMAP_LDR_ENTRY, InMemoryOrderLinks);
        if (!MmIsAddressValid(&e->BaseDllName)) { entry = entry->Flink; continue; }
        if (e->BaseDllName.Buffer && e->BaseDllName.Length > 0
            && MmIsAddressValid(e->BaseDllName.Buffer))
        {
            SIZE_T len = e->BaseDllName.Length / sizeof(WCHAR);
            if (_wcsnicmp(e->BaseDllName.Buffer, name, len) == 0 && name[len] == L'\0') {
                return e->DllBase;
            }
        }
        entry = entry->Flink;
    }
    return nullptr;
}
