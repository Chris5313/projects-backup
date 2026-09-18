#include "apc.hpp"
#include "nt_structs.hpp"
#include "process.hpp"
#include "klog.hpp"

// ---------------------------------------------------------------------------
// Thread hijacking — replaces APC-based execution.
//
// APCs only fire when the target thread returns to user mode.  WrQueue threads
// (NtRemoveIoCompletion) are idle at early startup — no completion packets
// arrive, so the thread never exits its kernel wait and the APC sits in the
// queue forever.  Thread hijacking is deterministic: suspend, modify RIP to
// shellcode, resume, alert to force the kernel wait to return.
// ---------------------------------------------------------------------------

// Runtime-resolved ntoskrnl exports (not in WDK public .lib).
//
// We deliberately do NOT use Ps{Get,Set}ContextThread. On modern Windows
// they queue a special kernel APC to the target that reads KTHREAD::TrapFrame;
// on a suspended non-current thread that capture returns STATUS_UNSUCCESSFUL
// when the frame isn't currently populated — verified in the field on
// Win 11 26200 against Unturned.exe (log: `GetCtx: 0xFFFFFFFFC0000001` on
// every candidate). Reference: KernelCactus writeup on KTRAP_FRAME hijacking
// (spikysabra.gitbook.io) — identical failure mode.
//
// Instead we read KTHREAD::TrapFrame directly, modify KTRAP_FRAME::Rip, and
// let sysret carry the modified RIP back to user mode. Offsets are stable
// Win 10 1903 -> Win 11 26200 (verified against the KTRAP_FRAME struct in
// this WDK's ntddk.h; KTHREAD::TrapFrame at 0x090 via public reversing).
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

// ---------------------------------------------------------------------------
// Hijack bookkeeping (2026-09-08 friend-run postmortem).
//
// Two failure modes measured in the field:
//  1. A hijacked thread parked in a LONG non-alertable wait does not run the
//     shellcode until its wait returns (26s in the captured run). The marker
//     poll then times out and the mapper retries — and picked the SAME thread
//     again. Track tried TIDs so a retry picks a different thread.
//  2. On retry, the stale trap-frame RIP of the first (leaked) image was
//     read as "origRip" and embedded into the new shellcode as the return
//     address — after DllMain the thread jumped into the OLD image's
//     shellcode (C0000005 at base+0x200 in the payload VEH log). Track every
//     leaked payload range and reject trap-frame RIPs that fall inside one.
// ---------------------------------------------------------------------------
static constexpr ULONG MAX_TRIED_TIDS = 32;
static HANDLE  g_triedTids[MAX_TRIED_TIDS];
static ULONG   g_nTried = 0;
static HANDLE  g_triedPid = nullptr;          // TID list validity scope

static constexpr ULONG MAX_LEAKED_RANGES = 4;
static ULONG64 g_leakBase[MAX_LEAKED_RANGES];
static ULONG64 g_leakSize[MAX_LEAKED_RANGES];
static ULONG   g_nLeaked = 0;

// Called by the mapper when an image is leaked (marker timeout): its pages
// stay mapped and executable, so any trap-frame RIP inside it is OUR stale
// shellcode — never a valid return address.
VOID ApcRegisterLeakedRange(ULONG64 Base, ULONG64 Size)
{
    if (g_nLeaked >= MAX_LEAKED_RANGES) return;
    g_leakBase[g_nLeaked] = Base;
    g_leakSize[g_nLeaked] = Size;
    g_nLeaked++;
}

static bool RipInsideKnownImage(ULONG64 rip, ULONG64 curBase, ULONG64 curSize)
{
    if (rip >= curBase && rip < curBase + curSize) return true;
    for (ULONG i = 0; i < g_nLeaked; i++)
        if (rip >= g_leakBase[i] && rip < g_leakBase[i] + g_leakSize[i]) return true;
    return false;
}

static void RememberTid(HANDLE pid, HANDLE tid)
{
    if (g_triedPid != pid) { g_nTried = 0; g_triedPid = pid; }
    if (g_nTried >= MAX_TRIED_TIDS) return;
    for (ULONG i = 0; i < g_nTried; i++)
        if (g_triedTids[i] == tid) return;
    g_triedTids[g_nTried++] = tid;
}

static bool TidAlreadyTried(HANDLE pid, HANDLE tid)
{
    if (g_triedPid != pid) return false;
    for (ULONG i = 0; i < g_nTried; i++)
        if (g_triedTids[i] == tid) return true;
    return false;
}

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
// Shellcode: saves ALL GPRs + RFLAGS, calls DllMain(base, 1, NULL),
// writes marker 0x42 to base+0x100, restores everything, jumps to origRip.
// 119 bytes.  Written to the wiped PE header area at base+0x200 (RWX).
// ---------------------------------------------------------------------------
static ULONG BuildShellcode(UCHAR* out, ULONG64 dllBase, ULONG64 ep, ULONG64 origRip)
{
    UCHAR sc[] = {
        // Save all GPRs + flags
        0x50,                               // push rax
        0x51,                               // push rcx
        0x52,                               // push rdx
        0x53,                               // push rbx
        0x55,                               // push rbp
        0x56,                               // push rsi
        0x57,                               // push rdi
        0x41,0x50,                          // push r8
        0x41,0x51,                          // push r9
        0x41,0x52,                          // push r10
        0x41,0x53,                          // push r11
        0x41,0x54,                          // push r12
        0x41,0x55,                          // push r13
        0x41,0x56,                          // push r14
        0x41,0x57,                          // push r15
        0x9C,                               // pushfq
        // Align stack + shadow space
        0x48,0x89,0xE5,                     // mov rbp, rsp
        0x48,0x83,0xE4,0xF0,                // and rsp, -16
        0x48,0x83,0xEC,0x20,                // sub rsp, 0x20
        // mov rcx, <dllBase>                  [patch @ +37]
        0x48,0xB9, 0,0,0,0,0,0,0,0,
        // mov edx, 1  (DLL_PROCESS_ATTACH)
        0xBA,0x01,0x00,0x00,0x00,
        // xor r8d, r8d  (reserved = NULL)
        0x45,0x31,0xC0,
        // mov rax, <entryPoint>               [patch @ +55]
        0x48,0xB8, 0,0,0,0,0,0,0,0,
        // call rax
        0xFF,0xD0,
        // mov rax, <dllBase + 0x100>          [patch @ +67]
        0x48,0xB8, 0,0,0,0,0,0,0,0,
        // mov byte [rax], 0x42
        0xC6,0x00,0x42,
        // Restore stack
        0x48,0x89,0xEC,                     // mov rsp, rbp
        // Restore all GPRs + flags
        0x9D,                               // popfq
        0x41,0x5F,                          // pop r15
        0x41,0x5E,                          // pop r14
        0x41,0x5D,                          // pop r13
        0x41,0x5C,                          // pop r12
        0x41,0x5B,                          // pop r11
        0x41,0x5A,                          // pop r10
        0x41,0x59,                          // pop r9
        0x41,0x58,                          // pop r8
        0x5F,                               // pop rdi
        0x5E,                               // pop rsi
        0x5D,                               // pop rbp
        0x5B,                               // pop rbx
        0x5A,                               // pop rdx
        0x59,                               // pop rcx
        0x58,                               // pop rax
        // jmp qword [rip+0]                   [patch @ +111]
        0xFF,0x25,0x00,0x00,0x00,0x00,
        // dq <originalRip>
        0,0,0,0,0,0,0,0,
    };

    *(ULONG64*)(sc + 37)  = dllBase;
    *(ULONG64*)(sc + 55)  = ep;
    *(ULONG64*)(sc + 67)  = dllBase + 0x100;
    *(ULONG64*)(sc + 111) = origRip;

    RtlCopyMemory(out, sc, sizeof(sc));
    return (ULONG)sizeof(sc);
}

// ---------------------------------------------------------------------------
// HijackThread — suspend, write shellcode, modify RIP, resume + alert
// ---------------------------------------------------------------------------
static NTSTATUS HijackThread(PEPROCESS target, PETHREAD thread,
                              ULONG64 dllBase, ULONG64 ep)
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
    // POISON GUARD: a RIP inside the image we are about to map, or inside a
    // previously LEAKED image, is our own stale shellcode from an earlier
    // hijack attempt (the trap frame was patched but the thread never
    // returned to user mode). Using it as the return address chains the
    // thread through a dead image after DllMain — instant AV. Skip it.
    if (RipInsideKnownImage(origRip, dllBase, 0x400000ull)) {
        KLogHex("Rip inside payload image (stale hijack) — skip: ", origRip);
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
    } else {
    }

    KLogHex("RIP: ", origRip);

    {
        UCHAR scBuf[128];
        ULONG scSize = BuildShellcode(scBuf, dllBase, ep, origRip);
        ULONG64 scDst = dllBase + 0x200;
        SIZE_T written = 0;
        s = g_MmCopy(PsGetCurrentProcess(), scBuf,
                      target, (PVOID)scDst,
                      scSize, KernelMode, &written);
        if (!NT_SUCCESS(s)) {
            KLogHex("MmCopy SC: ", s);
            if (hasSuspend) goto cleanup_resume;
            return s;
        }

        frame->Rip = scDst;
    }

    // Resume WITHOUT alerting — v4-proven behavior. The 2026-09-08 alert
    // experiment force-broke a thread out of an ntdll lock-wait mid-acquire
    // (RtlWaitOnAddress family); the wait machinery was left corrupted and
    // the game died in a C0000005 storm inside that same machinery
    // (ntdll+0x443C1, 0x38D from the parked RIP). With a natural wake the
    // wait completes cleanly and the thread lands on our shellcode with a
    // coherent state — that is exactly how the stable runs behaved.
    // Slow threads are handled by selection + the tried-TID blacklist, not
    // by force-waking.
    if (hasSuspend) {
        if (g_useHandle && hThread) { g_ZwResume(hThread, &prevCount); ZwClose(hThread); }
        else if (g_PsResume) g_PsResume(thread, &prevCount);
    } else {
        KeAlertThread(thread, KernelMode);
    }
    KLog("Hijacked (natural wake)");
    return STATUS_SUCCESS;


cleanup_resume:
    if (g_useHandle && hThread) { g_ZwResume(hThread, &prevCount); ZwClose(hThread); }
    else if (g_PsResume) g_PsResume(thread, &prevCount);
    return STATUS_UNSUCCESSFUL;
}

// ---------------------------------------------------------------------------
// HijackAndInject — public entry point. Thread hijacking replaces APCs
// because APCs never fire on WrQueue threads idle in NtRemoveIoCompletion.
// ---------------------------------------------------------------------------

// KWAIT_REASON values from ntddk.h (_KWAIT_REASON enum). v4-exact scoring —
// the 2026-09-08 rework was speculative and is reverted; only the tried-TID
// exclusion is kept (it fixes the measured re-pick of a dead thread).
#define KWR_USER_REQUEST      6   // UserRequest    — NtWaitForSingleObject etc.
#define KWR_DELAY_EXECUTION  11   // WrDelayExecution — NtDelayExecution
#define KWR_USER_REQUEST_2   13   // WrUserRequest  — variant, also in syscall
#define KWR_QUEUE            15   // WrQueue        — idle in NtRemoveIoCompletion

static constexpr ULONG MAX_HIJACK_RETRIES = 15;

NTSTATUS HijackAndInject(PEPROCESS target, PVOID entryPoint, PVOID dllBaseIn)
{
    if (KeGetCurrentIrql() != PASSIVE_LEVEL) return STATUS_INVALID_LEVEL;
    if (PsGetProcessExitStatus(target) != STATUS_PENDING) return STATUS_PROCESS_IS_TERMINATING;
    if (!ResolveFunctions()) return STATUS_PROCEDURE_NOT_FOUND;

    ULONG64 dllBase = (ULONG64)dllBaseIn;
    ULONG64 ep      = (ULONG64)entryPoint;
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

        // Find target process and pick the best non-main waiting thread.
        // Threads already tried (earlier attempt or a previous MapUserDll
        // retry — the TID list is per-process) are skipped so a failed
        // long-wait thread is never picked twice.
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
                    HANDLE tid = threads[i].ClientId.UniqueThread;
                    if (TidAlreadyTried(pid, tid)) continue;   // never re-hijack
                    int score = 1;
                    ULONG wr = threads[i].WaitReason;
                    if (wr == KWR_DELAY_EXECUTION)  score = 2;
                    if (wr == KWR_USER_REQUEST)     score = 2;
                    if (wr == KWR_USER_REQUEST_2)   score = 2;
                    if (wr == KWR_QUEUE)            score = 3;
                    if (score > bestScore) {
                        bestScore = score;
                        bestTid = tid;
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
                RememberTid(pid, bestTid);
                s = HijackThread(target, thread, dllBase, ep);
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
