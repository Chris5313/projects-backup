#include "mapper.hpp"
#include "process.hpp"
#include "klog.hpp"
#include "beep.hpp"

#define TARGET_PROCESS  "Unturned.exe"
#define DLL_PATH        L"\\??\\C:\\Users\\Public\\payload.dll"
#define POOL_TAG        'FMfn'

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
// Wait for Mono to load in target, settle, then inject.
// Assumes target has been referenced by caller.
// ---------------------------------------------------------------------------
static NTSTATUS WaitAndInject(PEPROCESS target)
{
    if (KeGetCurrentIrql() != PASSIVE_LEVEL) return STATUS_INVALID_LEVEL;
    if (PsGetProcessExitStatus(target) != STATUS_PENDING) {
        KLog("target already terminating, aborting inject");
        return STATUS_PROCESS_IS_TERMINATING;
    }

    KLog("Waiting for mono-2.0-bdwgc.dll...");
    bool monoReady = false;
    KAPC_STATE waitApc;
    for (int w = 0; w < 120; w++) {
        LARGE_INTEGER d; d.QuadPart = -5000000LL; // 500ms
        KeDelayExecutionThread(KernelMode, FALSE, &d);

        if (PsGetProcessExitStatus(target) != STATUS_PENDING) {
            KLog("target died during mono wait");
            return STATUS_PROCESS_IS_TERMINATING;
        }

        KeStackAttachProcess((PRKPROCESS)target, &waitApc);
        monoReady = (GetUserModuleBase(L"mono-2.0-bdwgc.dll") != nullptr);
        KeUnstackDetachProcess(&waitApc);

        if (monoReady) break;
    }

    if (!monoReady) KLog("WARN: mono-2.0-bdwgc.dll NOT found after 60s");

    // +1s settle — let Mono finish init
    LARGE_INTEGER settle; settle.QuadPart = -10000000LL;
    KeDelayExecutionThread(KernelMode, FALSE, &settle);

    if (PsGetProcessExitStatus(target) != STATUS_PENDING) {
        KLog("target died before inject");
        return STATUS_PROCESS_IS_TERMINATING;
    }

    KLog("Injecting DLL...");
    NTSTATUS s = MapUserDll(target, g_dllBuffer);

    if (NT_SUCCESS(s)) {
        KLog("SUCCESS: DLL injected!");
        BeepDoubleSuccess();
    } else {
        KLog("ERROR: Injection failed");
        KLogHex("  Status: ", s);
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

    KLog("Polling thread started");

    for (int i = 0; i < 1800; i++) {
        PEPROCESS target = nullptr;
        NTSTATUS s = FindProcessByName(TARGET_PROCESS, &target);

        if (NT_SUCCESS(s)) {
            KLog("====================================");
            KLog("Unturned.exe detected!");
            KLogHex("  PID: ", (ULONG_PTR)PsGetProcessId(target));
            KLog("====================================");

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

    KLog("Polling thread exiting");
    PsTerminateSystemThread(STATUS_SUCCESS);
}

// ---------------------------------------------------------------------------
// DriverEntry — LOADS BEFORE Unturned, uses polling to inject later
// ---------------------------------------------------------------------------
extern "C" NTSTATUS CustomDriverEntry(
    _In_ PDRIVER_OBJECT  DriverObject,
    _In_ PUNICODE_STRING RegistryPath)
{
    UNREFERENCED_PARAMETER(DriverObject);
    UNREFERENCED_PARAMETER(RegistryPath);

    KLog("====================================");
    KLog("kmap driver loaded - DriverEntry");
    KLog("====================================");

    BeepDriverLoaded();

    KLog("Step 2: Reading payload DLL from disk");
    NTSTATUS s = ReadFileToKernelBuffer(DLL_PATH, &g_dllBuffer, &g_dllSize);
    if (!NT_SUCCESS(s)) {
        KLog("ERROR: Failed to read payload DLL");
        KLogHex("  Status: ", s);
        return s;
    }
    KLog("SUCCESS: Payload DLL loaded into memory");
    KLogHex("  Size: ", g_dllSize);

    KLog("Step 3: Checking if Unturned is already running");
    PEPROCESS target = nullptr;
    s = FindProcessByName(TARGET_PROCESS, &target);

    if (NT_SUCCESS(s)) {
        KLogHex("  Unturned.exe found - PID: ", (ULONG_PTR)PsGetProcessId(target));
        NTSTATUS injectStatus = WaitAndInject(target);
        ObDereferenceObject(target);
        ExFreePool(g_dllBuffer);
        g_dllBuffer = nullptr;
        return injectStatus;
    }

    KLog("  Unturned.exe not found - starting polling thread");

    HANDLE hThread = nullptr;
    s = PsCreateSystemThread(&hThread, THREAD_ALL_ACCESS, NULL, NULL, NULL,
        PollingThreadRoutine, NULL);
    if (!NT_SUCCESS(s)) {
        KLog("ERROR: Failed to create polling thread");
        KLogHex("  Status: ", s);
        ExFreePool(g_dllBuffer);
        return s;
    }
    ZwClose(hThread);

    KLog("SUCCESS: Polling thread started - driver stays loaded");
    return STATUS_SUCCESS;
}
