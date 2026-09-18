#include "mapper.hpp"
#include "process.hpp"
#include "klog.hpp"
#include "beep.hpp"
#include "config.h"

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
// Wait for the target's renderer module, settle, then inject.
// Assumes target has been referenced by caller.
// ---------------------------------------------------------------------------
static NTSTATUS WaitAndInject(PEPROCESS target)
{
    KLog("WaitAndInject: entered");
    if (KeGetCurrentIrql() != PASSIVE_LEVEL) { KLog("WaitAndInject: BAD IRQL"); return STATUS_INVALID_LEVEL; }
    if (PsGetProcessExitStatus(target) != STATUS_PENDING) {
        KLog("target already terminating, aborting inject");
        return STATUS_PROCESS_IS_TERMINATING;
    }

    int maxIters = GW2_WAIT_MODULE_SECS * 2; // 500ms per iteration
    bool moduleReady = false;
    KAPC_STATE waitApc;
    KLog("WaitAndInject: starting module wait loop (d3d11.dll)");
    for (int w = 0; w < maxIters; w++) {
        LARGE_INTEGER d; d.QuadPart = -5000000LL; // 500ms
        KeDelayExecutionThread(KernelMode, FALSE, &d);

        if (PsGetProcessExitStatus(target) != STATUS_PENDING) {
            KLog("target died during module wait");
            return STATUS_PROCESS_IS_TERMINATING;
        }

        // Log every 10th iteration (every 5s) and the first 3
        if (w < 3 || w % 10 == 0) {
            char iterMsg[64] = "WaitAndInject: module poll #";
            char num[12];
            int n = w, pos = 0;
            if (n == 0) { num[pos++] = '0'; }
            else { char tmp[12]; int tp = 0; while (n > 0) { tmp[tp++] = '0' + (n % 10); n /= 10; } for (int j = tp - 1; j >= 0; j--) num[pos++] = tmp[j]; }
            num[pos] = '\0';
            int base = 27;
            for (int j = 0; num[j]; j++) iterMsg[base++] = num[j];
            iterMsg[base] = '\0';
            KLog(iterMsg);
        }

        KLog("WaitAndInject: attaching to target process");
        KeStackAttachProcess((PRKPROCESS)target, &waitApc);
        KLog("WaitAndInject: attached, calling GetUserModuleBase");
        moduleReady = (GetUserModuleBase(GW2_WAIT_MODULE) != nullptr);
        KLog("WaitAndInject: detaching");
        KeUnstackDetachProcess(&waitApc);

        if (moduleReady) {
            KLog("WaitAndInject: d3d11.dll FOUND");
            break;
        }
    }

    if (!moduleReady) KLog("WARN: wait-module NOT found within timeout");

    // Settle — wait for EAAC to finish its address-space scan before mapping.
    // The old 1s settle was too short; the dump showed EAAC invalidating the
    // view within 2.7s of process creation.
    KLog("WaitAndInject: settling for EAAC...");
    LARGE_INTEGER settle;
    settle.QuadPart = -(LONGLONG)GW2_SETTLE_SECS * 10000000LL;
    KeDelayExecutionThread(KernelMode, FALSE, &settle);

    if (PsGetProcessExitStatus(target) != STATUS_PENDING) {
        KLog("target died before inject");
        return STATUS_PROCESS_IS_TERMINATING;
    }

    // Baseline memory snapshot BEFORE injection. diff baseline -> dump 0 is
    // exactly what WE add to the process, as a scanner would see it.
    KLog("observation: capturing pre-inject baseline");
    DumpSnapshot(target, -1, "baseline");

    // Try injection up to 2 times — first attempt may fail if EAAC tears down
    // our view; second attempt after EAAC settles often succeeds.
    NTSTATUS s = STATUS_UNSUCCESSFUL;
    for (int attempt = 1; attempt <= 2; attempt++) {
        if (attempt > 1) {
            KLog("Retrying injection after 3s...");
            LARGE_INTEGER retry; retry.QuadPart = -30000000LL;
            KeDelayExecutionThread(KernelMode, FALSE, &retry);
            if (PsGetProcessExitStatus(target) != STATUS_PENDING) {
                KLog("target died before retry");
                return STATUS_PROCESS_IS_TERMINATING;
            }
        }

        KLog("Injecting DLL...");
        s = MapUserDll(target, g_dllBuffer);
        if (NT_SUCCESS(s)) {
            KLog("SUCCESS: DLL injected!");
            BeepDoubleSuccess();
            return s;
        }
        KLog("ERROR: Injection failed");
        KLogHex("  Status: ", s);
    }

    BeepError();
    return s;
}

// ---------------------------------------------------------------------------
// Polling thread — runs at PASSIVE_LEVEL, waits up to 1h for the game.
//
// NOTE: This copies the Unturned driver flow exactly (proven against BattlEye).
// The previous GW2 build polled synchronously INSIDE DriverEntry to avoid an
// EAAC system-thread scan, but that blocks kdmapper's IOCTL (iqvw64e waits for
// the entry call to return), so the loader's 30s wait timed out and the driver
// never even finished loading. Unturned spawns this thread and returns from
// DriverEntry immediately, which is what kdmapper requires.
// ---------------------------------------------------------------------------
static VOID PollingThreadRoutine(PVOID Context)
{
    UNREFERENCED_PARAMETER(Context);

    KLog("Polling thread started");

    for (int i = 0; i < 1800; i++) {
        PEPROCESS target = nullptr;
        NTSTATUS s = FindProcessByName(GW2_TARGET_PROCESS, &target);

        if (NT_SUCCESS(s)) {
            KLog("====================================");
            KLog("target process detected!");
            KLogHex("  PID: ", (ULONG_PTR)PsGetProcessId(target));
            KLog("====================================");

            if (_InterlockedCompareExchange(&g_injected, 1, 0) == 0) {
                // Phase 2a harvest run: inject the full payload during a
                // GOOD login (EAAC running) so the in-process harvester can
                // capture the real ACHT UUIDs.
                //
                // The game spawns a short-lived launcher STUB with the same
                // image name (observed 2026-09-04 23:37: stub lived 0.5s).
                // The stub death must NOT consume the one-shot: on failure,
                // reset g_injected and keep polling for the real instance.
                NTSTATUS injectStatus = WaitAndInject(target);
                if (NT_SUCCESS(injectStatus)) {
                    ObDereferenceObject(target);
                    KLog("injection complete - polling thread done");
                    break;
                }
                _InterlockedExchange(&g_injected, 0);
                KLog("inject attempt failed (stub died or transient) - will retry on next detection");
            }
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
// DriverEntry — LOADS BEFORE the game, uses polling to inject later.
// Returns immediately after starting the polling thread (kdmapper requirement).
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
    NTSTATUS s = ReadFileToKernelBuffer(GW2_PAYLOAD_PATH, &g_dllBuffer, &g_dllSize);
    if (!NT_SUCCESS(s)) {
        KLog("ERROR: Failed to read payload DLL");
        KLogHex("  Status: ", s);
        return s;
    }
    KLog("SUCCESS: Payload DLL loaded into memory");
    KLogHex("  Size: ", g_dllSize);

    // ALWAYS the polling thread — never inject from DriverEntry directly:
    // injection + the ~6-minute observation dump loop would block kdmapper's
    // entry call until the loader's wait times out. The polling thread finds
    // an already-running target within 2s anyway.
    KLog("Step 3: starting polling thread (driver stays loaded)");

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
