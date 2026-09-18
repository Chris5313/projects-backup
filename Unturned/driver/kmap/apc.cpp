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
    if (!g_MmCopy) { KLog("r1"); return false; }

    // Try Zw* (HANDLE-based, works on some Win11)
    g_ZwSuspend = (pfn_ZwSuspend)TryGet(L"ZwSuspendThread");
    g_ZwResume  = (pfn_ZwResume)TryGet(L"ZwResumeThread");
    if (g_ZwSuspend && g_ZwResume) {
        g_useHandle = true;
        g_resolved = true;
        KLog("h: zw");
        return true;
    }

    // Try Nt*
    g_ZwSuspend = (pfn_ZwSuspend)TryGet(L"NtSuspendThread");
    g_ZwResume  = (pfn_ZwResume)TryGet(L"NtResumeThread");
    if (g_ZwSuspend && g_ZwResume) {
        g_useHandle = true;
        g_resolved = true;
        KLog("h: nt");
        return true;
    }

    // Try Ps* (PETHREAD-based)
    g_PsSuspend = (pfn_PsSuspend)TryGet(L"PsSuspendThread");
    g_PsResume  = (pfn_PsResume)TryGet(L"PsResumeThread");
    if (g_PsSuspend && g_PsResume) {
        g_useHandle = false;
        g_resolved = true;
        KLog("h: ps");
        return true;
    }

    // No suspend available — modify trap frame directly while thread is in
    // kernel wait. Safe: thread is blocked in WrQueue/WrAlertByTid, trap
    // frame is stable. Many injectors work this way.
    KLog("h: direct");
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
        KLogHex("tf:", (ULONG_PTR)frame);
        return STATUS_UNSUCCESSFUL;
    }

    ULONG64 origRip = frame->Rip;
    // The saved Rip must be a user-mode address. If it's kernel or zero the
    // thread wasn't paused at a syscall boundary — skip it.
    if (origRip == 0 || origRip >= 0x00007FFFFFFF0000ULL) {
        KLogHex("rip:", origRip);
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
            if (!NT_SUCCESS(s)) { KLogHex("ob:", s); return s; }
            s = g_ZwSuspend(hThread, &prevCount);
        } else if (g_PsSuspend) {
            s = g_PsSuspend(thread, &prevCount);
        }
        if (!NT_SUCCESS(s)) {
            KLogHex("sus:", s);
            if (hThread) ZwClose(hThread);
            return s;
        }

        // Re-read after suspend
        if (!MmIsAddressValid(&frame->Rip)) {
            KLog("tf lost");
            goto cleanup_resume;
        }
        origRip = frame->Rip;
        if (origRip == 0 || origRip >= 0x00007FFFFFFF0000ULL) {
            KLogHex("rip2:", origRip);
            goto cleanup_resume;
        }
    } else {
    }

    KLogHex("r:", origRip);

    {
        UCHAR scBuf[128];
        ULONG scSize = BuildShellcode(scBuf, dllBase, ep, origRip);
        ULONG64 scDst = dllBase + 0x200;
        SIZE_T written = 0;
        s = g_MmCopy(PsGetCurrentProcess(), scBuf,
                      target, (PVOID)scDst,
                      scSize, KernelMode, &written);
        if (!NT_SUCCESS(s)) {
            KLogHex("cpy:", s);
            if (hasSuspend) goto cleanup_resume;
            return s;
        }

        frame->Rip = scDst;
    }

    // Resume if we suspended
    if (hasSuspend) {
        if (g_useHandle && hThread) { g_ZwResume(hThread, &prevCount); ZwClose(hThread); }
        else if (g_PsResume) g_PsResume(thread, &prevCount);
    }
    KLog("hj ok");
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
            POOL_FLAG_NON_PAGED, needed, 'WrSd');
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
                    KLogHex("t=", numThreads);

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
                KLogHex("->", (ULONG_PTR)bestTid);
                s = HijackThread(target, thread, dllBase, ep);
                ObDereferenceObject(thread);
                if (NT_SUCCESS(s)) return STATUS_SUCCESS;
            }
        }

        KLogHex("a:", attempt + 1);
        LARGE_INTEGER delay;
        delay.QuadPart = -20000000LL; // 2s
        KeDelayExecutionThread(KernelMode, FALSE, &delay);
    }

    KLog("exh");
    return STATUS_UNSUCCESSFUL;
}
