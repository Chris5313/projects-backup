#include "mapper.hpp"
#include "process.hpp"
#include "klog.hpp"
#include "beep.hpp"

#define TARGET_PROCESS  "Unturned.exe"
#define DLL_PATH        L"\\??\\C:\\Users\\Public\\msec_data.bin"
#define POOL_TAG        'CmRe'

// Undocumented structures for ZwQuerySection
typedef struct _SECTION_BASIC_INFORMATION {
    PVOID BaseAddress;
    ULONG AllocationAttributes;
    LARGE_INTEGER MaximumSize;
} SECTION_BASIC_INFORMATION, *PSECTION_BASIC_INFORMATION;

typedef enum _SECTION_INFORMATION_CLASS {
    SectionBasicInformation = 0,
    SectionImageInformation = 1
} SECTION_INFORMATION_CLASS;

extern "C" NTSTATUS NTAPI ZwQuerySection(
    HANDLE SectionHandle,
    SECTION_INFORMATION_CLASS InformationClass,
    PVOID InformationBuffer,
    SIZE_T InformationBufferSize,
    PSIZE_T ResultLength);

static PVOID g_dllBuffer = nullptr;
static SIZE_T g_dllSize = 0;
static volatile LONG g_injected = 0;

// ---------------------------------------------------------------------------
// Read DLL from disk into kernel pool
// ---------------------------------------------------------------------------
static NTSTATUS ReadFileToKernelBuffer(PCWSTR path, PVOID* outBuf, PSIZE_T outSize)
{
    UNICODE_STRING uPath;
    RtlInitUnicodeString(&uPath, path);
    OBJECT_ATTRIBUTES oa;
    InitializeObjectAttributes(&oa, &uPath,
        OBJ_CASE_INSENSITIVE | OBJ_KERNEL_HANDLE, NULL, NULL);

    HANDLE hFile = nullptr;
    IO_STATUS_BLOCK iosb = {};
    NTSTATUS s = ZwCreateFile(&hFile,
        GENERIC_READ | SYNCHRONIZE, &oa, &iosb,
        NULL, FILE_ATTRIBUTE_NORMAL, FILE_SHARE_READ,
        FILE_OPEN, FILE_SYNCHRONOUS_IO_NONALERT | FILE_NON_DIRECTORY_FILE,
        NULL, 0);
    if (!NT_SUCCESS(s)) return s;

    FILE_STANDARD_INFORMATION fsi = {};
    s = ZwQueryInformationFile(hFile, &iosb, &fsi, sizeof(fsi), FileStandardInformation);
    if (!NT_SUCCESS(s)) { ZwClose(hFile); return s; }

    SIZE_T size = (SIZE_T)fsi.EndOfFile.QuadPart;
    PVOID buf = ExAllocatePool2(POOL_FLAG_NON_PAGED, size, POOL_TAG);
    if (!buf) { ZwClose(hFile); return STATUS_INSUFFICIENT_RESOURCES; }

    LARGE_INTEGER offset = {};
    s = ZwReadFile(hFile, NULL, NULL, NULL, &iosb, buf, (ULONG)size, &offset, NULL);
    ZwClose(hFile);
    if (!NT_SUCCESS(s)) { ExFreePool(buf); return s; }

    *outBuf = buf; *outSize = size;
    return STATUS_SUCCESS;
}

// ---------------------------------------------------------------------------
// Read DLL from named kernel section (created by user-mode loader)
// Falls back to disk read if section doesn't exist (backward compatibility)
// ---------------------------------------------------------------------------
static NTSTATUS ReadDllFromSection(PVOID* outBuf, PSIZE_T outSize)
{
    UNICODE_STRING sectionName;
    RtlInitUnicodeString(&sectionName, L"\\BaseNamedObjects\\WmiPerfProvider");

    OBJECT_ATTRIBUTES oa;
    InitializeObjectAttributes(&oa, &sectionName,
        OBJ_CASE_INSENSITIVE | OBJ_KERNEL_HANDLE, NULL, NULL);

    HANDLE hSection = nullptr;
    NTSTATUS s = ZwOpenSection(&hSection, SECTION_MAP_READ | SECTION_QUERY, &oa);
    if (!NT_SUCCESS(s)) {
        KLog("sec: not found, will use disk");
        return s;
    }

    // Query section size
    SECTION_BASIC_INFORMATION sbi = {};
    s = ZwQuerySection(hSection, SectionBasicInformation, &sbi, sizeof(sbi), nullptr);
    if (!NT_SUCCESS(s)) {
        KLog("ERROR: Failed to query section size");
        ZwClose(hSection);
        return s;
    }

    SIZE_T sectionSize = (SIZE_T)sbi.MaximumSize.QuadPart;
    if (sectionSize == 0 || sectionSize > 64 * 1024 * 1024) { // sanity: max 64MB
        KLog("ERROR: Section size invalid");
        ZwClose(hSection);
        return STATUS_INVALID_PARAMETER;
    }

    // Map the section into kernel space
    PVOID mappedBase = nullptr;
    SIZE_T viewSize = 0;
    s = ZwMapViewOfSection(hSection, ZwCurrentProcess(), &mappedBase, 0, 0,
        nullptr, &viewSize, ViewUnmap, 0, PAGE_READONLY);
    if (!NT_SUCCESS(s)) {
        KLog("ERROR: Failed to map payload section");
        ZwClose(hSection);
        return s;
    }

    // Allocate kernel pool and copy
    PVOID buf = ExAllocatePool2(POOL_FLAG_NON_PAGED, sectionSize, POOL_TAG);
    if (!buf) {
        ZwUnmapViewOfSection(ZwCurrentProcess(), mappedBase);
        ZwClose(hSection);
        return STATUS_INSUFFICIENT_RESOURCES;
    }

    RtlCopyMemory(buf, mappedBase, sectionSize);

    ZwUnmapViewOfSection(ZwCurrentProcess(), mappedBase);
    ZwClose(hSection);

    *outBuf = buf;
    *outSize = sectionSize;

    KLog("SUCCESS: Payload DLL loaded from memory section");
    KLogHex("  Size: ", sectionSize);
    return STATUS_SUCCESS;
}

// ---------------------------------------------------------------------------
// Wait for Mono to load in target, settle, then inject.
// Assumes target has been referenced by caller.
// ---------------------------------------------------------------------------
static NTSTATUS WaitAndInject(PEPROCESS target)
{
    if (KeGetCurrentIrql() != PASSIVE_LEVEL) return STATUS_INVALID_LEVEL;
    if (PsGetProcessExitStatus(target) != STATUS_PENDING) {
        KLog("proc exit");
        return STATUS_PROCESS_IS_TERMINATING;
    }

    KLog("wait mdl...");
    bool monoReady = false;
    KAPC_STATE waitApc;
    for (int w = 0; w < 120; w++) {
        LARGE_INTEGER d; d.QuadPart = -5000000LL; // 500ms
        KeDelayExecutionThread(KernelMode, FALSE, &d);

        if (PsGetProcessExitStatus(target) != STATUS_PENDING) {
            KLog("proc term");
            return STATUS_PROCESS_IS_TERMINATING;
        }

        KeStackAttachProcess((PRKPROCESS)target, &waitApc);
        monoReady = (GetUserModuleBase(L"mono-2.0-bdwgc.dll") != nullptr);
        KeUnstackDetachProcess(&waitApc);

        if (monoReady) break;
    }

    if (!monoReady) KLog("mdl timeout");

    // +1s settle — let Mono finish init
    LARGE_INTEGER settle; settle.QuadPart = -10000000LL;
    KeDelayExecutionThread(KernelMode, FALSE, &settle);

    if (PsGetProcessExitStatus(target) != STATUS_PENDING) {
        KLog("proc term2");
        return STATUS_PROCESS_IS_TERMINATING;
    }

    KLog("map...");
    NTSTATUS s = MapUserDll(target, g_dllBuffer);

    if (NT_SUCCESS(s)) {
        KLog("ok");
        BeepDoubleSuccess();
    } else {
        KLog("fail");
        KLogHex("s:", s);
        BeepError();
    }
    return s;
}

// ---------------------------------------------------------------------------
// Polling thread — runs at PASSIVE_LEVEL, waits up to 1h for Unturned.
// ponytail: PsSetCreateProcessNotifyRoutineEx would be zero-poll, but the
// callback pointer lives inside our (unhidden) driver image — that's exactly
// what BEDaisy's executable-region scan flags. Polling keeps us invisible.
// ---------------------------------------------------------------------------
static VOID PollingThreadRoutine(PVOID Context)
{
    UNREFERENCED_PARAMETER(Context);

    KLog("poll+");

    for (int i = 0; i < 1800; i++) {
        PEPROCESS target = nullptr;
        NTSTATUS s = FindProcessByName(TARGET_PROCESS, &target);

        if (NT_SUCCESS(s)) {
            KLog("---");
            KLog("found");
            KLogHex("p:", (ULONG_PTR)PsGetProcessId(target));
            KLog("---");

            if (_InterlockedCompareExchange(&g_injected, 1, 0) == 0) {
                WaitAndInject(target);
            }
            ObDereferenceObject(target);
            break;
        }

        LARGE_INTEGER interval; interval.QuadPart = -20000000LL; // 2s
        KeDelayExecutionThread(KernelMode, FALSE, &interval);
    }

    if (g_dllBuffer) {
        ExFreePool(g_dllBuffer);
        g_dllBuffer = nullptr;
    }

    KLog("poll-");
    PsTerminateSystemThread(STATUS_SUCCESS);
}

// ---------------------------------------------------------------------------
// DriverEntry — LOADS BEFORE Unturned, uses polling to inject later
// ---------------------------------------------------------------------------
extern "C" NTSTATUS SystemServiceEntry(
    _In_ PDRIVER_OBJECT  DriverObject,
    _In_ PUNICODE_STRING RegistryPath)
{
    UNREFERENCED_PARAMETER(DriverObject);
    UNREFERENCED_PARAMETER(RegistryPath);

    KLog("---");
    KLog("init");
    KLog("---");

    BeepDriverLoaded();

    KLog("s2: read");
    NTSTATUS s = ReadDllFromSection(&g_dllBuffer, &g_dllSize);
    if (!NT_SUCCESS(s)) {
        KLog("no sec");
        KLogHex("st:", s);
        s = ReadFileToKernelBuffer(DLL_PATH, &g_dllBuffer, &g_dllSize);
    }
    if (!NT_SUCCESS(s)) {
        KLog("fail read");
        KLogHex("st:", s);
        return s;
    }
    KLog("ok read");
    KLogHex("sz:", g_dllSize);

    KLog("s3: check");
    PEPROCESS target = nullptr;
    s = FindProcessByName(TARGET_PROCESS, &target);

    if (NT_SUCCESS(s)) {
        KLogHex("pid:", (ULONG_PTR)PsGetProcessId(target));
        NTSTATUS injectStatus = WaitAndInject(target);
        ObDereferenceObject(target);
        ExFreePool(g_dllBuffer);
        g_dllBuffer = nullptr;
        return injectStatus;
    }

    KLog("no proc, poll");

    HANDLE hThread = nullptr;
    s = PsCreateSystemThread(&hThread, THREAD_ALL_ACCESS, NULL, NULL, NULL,
        PollingThreadRoutine, NULL);
    if (!NT_SUCCESS(s)) {
        KLog("thread fail");
        KLogHex("st:", s);
        ExFreePool(g_dllBuffer);
        return s;
    }
    ZwClose(hThread);

    KLog("poll started");
    return STATUS_SUCCESS;
}
