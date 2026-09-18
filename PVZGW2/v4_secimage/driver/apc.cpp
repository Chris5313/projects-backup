#include "apc.hpp"
#include "nt_structs.hpp"
#include "process.hpp"
#include "klog.hpp"

// ---------------------------------------------------------------------------
// Thread hijacking — redirects a waiting thread's RIP into the payload's
// exported kmap_thunk (payload .text, written in kmap_thunk.asm).
//
// The thunk saves the full user context, calls the image entry
// (DllMainCRTStartup → DllMain) with (base, DLL_PROCESS_ATTACH, NULL),
// restores the context and jumps back to the thread's original RIP. The
// driver's only job here: write origRip into the payload's kmap_ctx cell,
// then patch KTRAP_FRAME::Rip to the thunk. No shellcode, no RWX scratch.
//
// APCs only fire when the target thread returns to user mode. WrQueue threads
// (NtRemoveIoCompletion) are idle at early startup — no completion packets
// arrive, so the thread never exits its kernel wait and the APC sits in the
// queue forever. Thread hijacking is deterministic: modify RIP, alert to
// force the kernel wait to return.
//
// We deliberately do NOT use Ps{Get,Set}ContextThread. On modern Windows
// they queue a special kernel APC to the target that reads KTHREAD::TrapFrame;
// on a suspended non-current thread that capture returns STATUS_UNSUCCESSFUL
// when the frame isn't currently populated — verified in the field on
// Win 11 26200 against Unturned.exe (log: `GetCtx: 0xFFFFFFFFC0000001` on
// every candidate). Instead we read KTHREAD::TrapFrame directly, modify
// KTRAP_FRAME::Rip, and let sysret carry the modified RIP back to user mode.
// Offsets are stable Win 10 1903 -> Win 11 26200 (verified against the
// KTRAP_FRAME struct in this WDK's ntddk.h; KTHREAD::TrapFrame at 0x090 via
// public reversing).
typedef NTSTATUS (NTAPI *pfn_MmCopyVirtualMemory)(
    PEPROCESS FromProcess, PVOID FromAddress,
    PEPROCESS ToProcess, PVOID ToAddress,
    SIZE_T BufferSize, KPROCESSOR_MODE PreviousMode, PSIZE_T BytesCopied);

// Two calling conventions exist across Windows versions:
// Zw/NtSuspendThread(HANDLE, PULONG) — exported on some Win11 builds
// PsSuspendThread(PETHREAD, PULONG)  — exported on some Win10/11 builds
// Neither is universal, so we try all and use whichever resolves.
typedef NTSTATUS (NTAPI *pfn_ZwSuspend)(HANDLE ThreadHandle, PULONG PrevCount);
typedef NTSTATUS (NTAPI *pfn_ZwResume)(HANDLE ThreadHandle, PULONG PrevCount);
typedef NTSTATUS (NTAPI *pfn_PsSuspend)(PETHREAD Thread, PULONG PrevCount);
typedef NTSTATUS (NTAPI *pfn_PsResume)(PETHREAD Thread, PULONG PrevCount);

static pfn_MmCopyVirtualMemory g_MmCopy;
// Only one pair will be non-null after resolve
static pfn_ZwSuspend g_ZwSuspend;
static pfn_ZwResume  g_ZwResume;
static pfn_PsSuspend g_PsSuspend;
static pfn_PsResume  g_PsResume;
static bool g_useHandle;   // true = Zw/Nt (HANDLE), false = Ps (PETHREAD)
static bool g_resolved = false;

static constexpr ULONG_PTR KTHREAD_TRAPFRAME_OFFSET = 0x090;

static PVOID TryGet(const wchar_t* name)
{
    UNICODE_STRING u;
    RtlInitUnicodeString(&u, name);
    return MmGetSystemRoutineAddress(&u);
}

static bool ResolveFunctions()
{
    if (g_resolved) return true;

    g_MmCopy = (pfn_MmCopyVirtualMemory)TryGet(L"MmCopyVirtualMemory");
    if (!g_MmCopy) { KLog("resolve FAIL: MmCopyVirtualMemory"); return false; }

    // Try Zw* (HANDLE-based, works on some Win11)
    g_ZwSuspend = (pfn_ZwSuspend)TryGet(L"ZwSuspendThread");
    g_ZwResume  = (pfn_ZwResume)TryGet(L"ZwResumeThread");
    if (g_ZwSuspend && g_ZwResume) {
        g_useHandle = true;
        g_resolved = true;
        KLog("Hijack: using ZwSuspend/ResumeThread (HANDLE)");
        return true;
    }

    // Try Nt*
    g_ZwSuspend = (pfn_ZwSuspend)TryGet(L"NtSuspendThread");
    g_ZwResume  = (pfn_ZwResume)TryGet(L"NtResumeThread");
    if (g_ZwSuspend && g_ZwResume) {
        g_useHandle = true;
        g_resolved = true;
        KLog("Hijack: using NtSuspend/ResumeThread (HANDLE)");
        return true;
    }

    // Try Ps* (PETHREAD-based)
    g_PsSuspend = (pfn_PsSuspend)TryGet(L"PsSuspendThread");
    g_PsResume  = (pfn_PsResume)TryGet(L"PsResumeThread");
    if (g_PsSuspend && g_PsResume) {
        g_useHandle = false;
        g_resolved = true;
        KLog("Hijack: using PsSuspend/ResumeThread (PETHREAD)");
        return true;
    }

    // No suspend available — modify trap frame directly while thread is in
    // kernel wait. Safe: thread is blocked in WrQueue/WrAlertByTid, trap
    // frame is stable. Many injectors work this way.
    KLog("Hijack: no suspend found — using direct trap frame write");
    g_resolved = true;
    return true;
}

// ---------------------------------------------------------------------------
// HijackThread — arm kmap_ctx.origRip, patch KTRAP_FRAME::Rip to the thunk,
// resume + alert.
// ---------------------------------------------------------------------------
static NTSTATUS HijackThread(PEPROCESS target, PETHREAD thread,
                              ULONG64 thunkVa, ULONG64 ctxVa)
{
    // Locate the saved user-mode trap frame on the thread's kernel stack.
    // If the thread never syscalled from user mode (pure system thread) or
    // TrapFrame was cleared before we sampled, bail so the outer loop tries
    // a different thread.
    PKTRAP_FRAME frame =
        *(PKTRAP_FRAME*)((PUCHAR)thread + KTHREAD_TRAPFRAME_OFFSET);
    if (!frame || !MmIsAddressValid(frame) || !MmIsAddressValid(&frame->Rip)) {
        KLogHex("bad TrapFrame ptr: ", (ULONG_PTR)frame);
        return STATUS_UNSUCCESSFUL;
    }

    ULONG64 origRip = frame->Rip;
    // The saved Rip must be a user-mode address. If it's kernel or zero the
    // thread wasn't paused at a syscall boundary — skip it.
    if (origRip == 0 || origRip >= 0x00007FFFFFFF0000ULL) {
        KLogHex("Rip not user-mode: ", origRip);
        return STATUS_UNSUCCESSFUL;
    }
    // ── Suspend (optional — skip if no API available) ──
    bool hasSuspend = g_ZwSuspend || g_PsSuspend;
    HANDLE hThread = nullptr;
    ULONG prevCount = 0;
    NTSTATUS s = STATUS_SUCCESS;

    if (hasSuspend) {
        if (g_useHandle && g_ZwSuspend) {
            s = ObOpenObjectByPointer(thread, OBJ_KERNEL_HANDLE, nullptr,
                THREAD_ALL_ACCESS, *PsThreadType, KernelMode, &hThread);
            if (!NT_SUCCESS(s)) { KLogHex("ObOpen: ", s); return s; }
            s = g_ZwSuspend(hThread, &prevCount);
        } else if (g_PsSuspend) {
            s = g_PsSuspend(thread, &prevCount);
        }
        if (!NT_SUCCESS(s)) {
            KLogHex("Suspend: ", s);
            if (hThread) ZwClose(hThread);
            return s;
        }

        // Re-read after suspend
        if (!MmIsAddressValid(&frame->Rip)) {
            KLog("TrapFrame vanished under suspend");
            goto cleanup_resume;
        }
        origRip = frame->Rip;
        if (origRip == 0 || origRip >= 0x00007FFFFFFF0000ULL) {
            KLogHex("post-suspend Rip not user: ", origRip);
            goto cleanup_resume;
        }
    }

    KLogHex("RIP: ", origRip);

    // Arm kmap_ctx[0] = origRip FIRST. If this write fails the thread is
    // still completely untouched — bail without patching anything.
    {
        SIZE_T written = 0;
        s = g_MmCopy(PsInitialSystemProcess, &origRip, target,
                     (PVOID)ctxVa, sizeof(origRip), KernelMode, &written);
        if (!NT_SUCCESS(s)) {
            KLogHex("MmCopy ctx: ", s);
            if (hasSuspend) goto cleanup_resume;
            return s;
        }
    }

    // Redirect the thread into the payload's thunk. The thunk restores
    // everything and jumps back to origRip when DllMain returns.
    frame->Rip = thunkVa;

    // Resume if we suspended; ALERT if we didn't. Without an alert the
    // thread stays blocked in its kernel wait (e.g. WrQueue) forever and
    // the thunk never runs — the trap frame RIP stays patched but the
    // thread never returns to user mode. KeAlertThread forces the wait to
    // return and the thread resumes at the thunk.
    if (hasSuspend) {
        if (g_useHandle && hThread) { g_ZwResume(hThread, &prevCount); ZwClose(hThread); }
        else if (g_PsResume) g_PsResume(thread, &prevCount);
    } else {
        KeAlertThread(thread, KernelMode);
    }
    KLog("Hijacked into kmap_thunk + alerted");
    return STATUS_SUCCESS;

cleanup_resume:
    if (g_useHandle && hThread) { g_ZwResume(hThread, &prevCount); ZwClose(hThread); }
    else if (g_PsResume) g_PsResume(thread, &prevCount);
    return STATUS_UNSUCCESSFUL;
}

// ---------------------------------------------------------------------------
// HijackAndInject — public entry point.
// ---------------------------------------------------------------------------

// KWAIT_REASON values from ntddk.h (_KWAIT_REASON enum). The old code had
// these WRONG — WR_QUEUE was 13 (=WrUserRequest), WR_DELAY_EXECUTION was 4
// (=DelayExecution not the Wr* variant). So real WrQueue threads (wr=15,
// the ideal hijack target: idle thread-pool workers in NtRemoveIoCompletion)
// always scored 1 and lost to random other waits.
#define KWR_USER_REQUEST      6   // UserRequest    — NtWaitForSingleObject etc.
#define KWR_DELAY_EXECUTION  11   // WrDelayExecution — NtDelayExecution
#define KWR_USER_REQUEST_2   13   // WrUserRequest  — variant, also in syscall
#define KWR_QUEUE            15   // WrQueue        — idle in NtRemoveIoCompletion

static constexpr ULONG MAX_HIJACK_RETRIES = 15;

NTSTATUS HijackAndInject(PEPROCESS target, PVOID thunk, PVOID ctx)
{
    if (KeGetCurrentIrql() != PASSIVE_LEVEL) return STATUS_INVALID_LEVEL;
    if (PsGetProcessExitStatus(target) != STATUS_PENDING) return STATUS_PROCESS_IS_TERMINATING;
    if (!ResolveFunctions()) return STATUS_PROCEDURE_NOT_FOUND;

    ULONG64 thunkVa = (ULONG64)thunk;
    ULONG64 ctxVa   = (ULONG64)ctx;
    HANDLE  pid     = PsGetProcessId(target);

    for (ULONG attempt = 0; attempt < MAX_HIJACK_RETRIES; attempt++) {
        ULONG needed = 0;
        ZwQuerySystemInformation(5, nullptr, 0, &needed);
        needed += 0x2000;

        auto buf = (PSYSTEM_PROCESS_INFORMATION)ExAllocatePool2(
            POOL_FLAG_NON_PAGED, needed, 'FMfn');
        if (!buf) return STATUS_INSUFFICIENT_RESOURCES;

        NTSTATUS s = ZwQuerySystemInformation(5, buf, needed, &needed);
        if (!NT_SUCCESS(s)) { ExFreePool(buf); return s; }

        // Find target process and pick the best non-main waiting thread
        HANDLE bestTid = nullptr;
        int    bestScore = 0;

        for (auto e = buf; ; e = (PSYSTEM_PROCESS_INFORMATION)((PUCHAR)e + e->NextEntryOffset)) {
            if (e->UniqueProcessId == pid) {
                auto threads = e->Threads;
                ULONG numThreads = e->NumberOfThreads;

                if (attempt == 0)
                    KLogHex("Hijack: threads=", numThreads);

                for (ULONG i = 1; i < numThreads; i++) {
                    if (threads[i].ThreadState != 5) continue; // must be Waiting
                    int score = 1;
                    ULONG wr = threads[i].WaitReason;
                    if (wr == KWR_DELAY_EXECUTION)  score = 2;
                    if (wr == KWR_USER_REQUEST)     score = 2;
                    if (wr == KWR_USER_REQUEST_2)   score = 2;
                    if (wr == KWR_QUEUE)            score = 3;
                    if (score > bestScore) {
                        bestScore = score;
                        bestTid = threads[i].ClientId.UniqueThread;
                    }
                }
                break;
            }
            if (!e->NextEntryOffset) break;
        }
        ExFreePool(buf);

        if (bestTid) {
            PETHREAD thread = nullptr;
            s = PsLookupThreadByThreadId(bestTid, &thread);
            if (NT_SUCCESS(s)) {
                KLogHex("Hijack -> T", (ULONG_PTR)bestTid);
                s = HijackThread(target, thread, thunkVa, ctxVa);
                ObDereferenceObject(thread);
                if (NT_SUCCESS(s)) return STATUS_SUCCESS;
                KLogHex("Hijack fail: ", s);
            }
        }

        KLogHex("No thread, attempt ", attempt + 1);
        LARGE_INTEGER delay;
        delay.QuadPart = -20000000LL; // 2s
        KeDelayExecutionThread(KernelMode, FALSE, &delay);
    }

    KLog("Hijack: all attempts exhausted");
    return STATUS_UNSUCCESSFUL;
}
