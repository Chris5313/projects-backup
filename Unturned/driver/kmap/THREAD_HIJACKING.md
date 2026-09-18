# Thread Hijacking — Direct KTRAP_FRAME Modification

Verified working on Windows 11 26200 against Unturned.exe on a full BE+VAC server (23+ min in-game, no kick).

## Why APCs Failed

Original approach: queue a user-mode APC to a WrQueue thread via `KeInitializeApc` +
`KeInsertQueueApc`.

**Root cause of failure**: WrQueue threads at game startup are idle thread-pool workers
sitting in `NtRemoveIoCompletion`. No I/O completion packets arrive during early startup,
so the thread never exits its kernel wait. User APCs only deliver when the thread
transitions to user mode. Result: APC sits in the queue forever, DllMain never runs.

Attempted fixes that don't work:
- `KeAlertThread` alone: alerts the wait, but a user APC still needs the thread to reach
  the user-mode APC delivery point (`KiDeliverApc` called from `KiCheckForKernelApcDelivery`
  on syscall return). Alerting a queue-idle thread just completes the wait with
  `STATUS_ALERTED` — it doesn't force user-mode reentry with a user APC.
- Targeting `WrDelayExecution` threads: these do return to user mode but only after
  their sleep expires — non-deterministic timing.
- Forcing alertable state: you can't change a thread's alertable flag from another thread.

## Why `PsGetContextThread` Also Failed

Second approach: use `PsGetContextThread` / `PsSetContextThread` on a suspended thread.
These exports exist on Win 10/11 (`Zw{Get,Set}ContextThread` do NOT — verified via
`dumpbin /EXPORTS ntoskrnl.exe`).

**Root cause of failure**: `PsGetContextThread` queues a special kernel APC that captures
the target's `KTHREAD::TrapFrame`. On a suspended non-current thread, if the trap frame
isn't currently populated (which is the common state — `TrapFrame` is only populated when
the thread transitioned from user mode via syscall, or is being debugged), the capture
returns `STATUS_UNSUCCESSFUL` (`0xC0000001`).

Field-observed on Win 11 26200: every candidate thread failed with `0xC0000001` after a
successful `ZwSuspendThread`. Reference: KernelCactus writeup on `KTRAP_FRAME` hijacking
(`spikysabra.gitbook.io/kernelcactus/pocs/thread-hijacking-using-_ktrap_frame`) documents
the identical failure mode.

## Working Approach — Direct KTRAP_FRAME Modification

Skip the API entirely. Read and write the saved trap frame directly.

```
1. Read KTHREAD::TrapFrame at offset 0x090  (Win 10 1903 → Win 11 26200 stable)
2. Validate the pointer with MmIsAddressValid
3. Validate KTRAP_FRAME::Rip is a user-mode address
4. ObOpenObjectByPointer → HANDLE (still needed for Zw{Suspend,Resume}Thread)
5. ZwSuspendThread(hThread)               — thread reaches safe suspend point
6. Re-validate frame after suspend (race guard)
7. Read origRip = frame->Rip              — user-mode return address
8. Build shellcode with origRip baked in for clean return
9. MmCopyVirtualMemory shellcode into target (base+0x200, RWX from wiped PE header)
10. frame->Rip = shellcode_addr           — direct write, no API call
11. ZwResumeThread(hThread)
12. KeAlertThread(thread, UserMode)       — force wait to return STATUS_ALERTED
13. ZwClose(hThread)
```

After step 12, the kernel wait completes. On syscall exit, `sysret` pops `Rip` from the
trap frame — which now has our modified address — into user-mode RIP. Thread enters user
mode at the shellcode.

## Field Offsets

Verified against `ntddk.h` in WDK 10.0.26100 (WDK for Win 11 26100+ SDK) and cross-checked
against public reversing (vergiliusproject) for stability across Win 10 1903 → Win 11 26200:

| struct::field | offset | source |
|---|---|---|
| `_KTHREAD::TrapFrame` | `0x090` | vergiliusproject Win 10 1903..Win 11 26200 |
| `_KTRAP_FRAME::Rip` | `0x168` | this WDK's `ntddk.h` (computed: home 0x28 + regs 0x38 + xmm 0x60 + dr 0x30 + debug 0x28 + segs 0x08 + trapFrame/rbx/rdi/rsi/rbp/errorCode 0x30 = 0x168) |
| `_KTRAP_FRAME::Rsp` | `0x180` | same source |

We use the WDK's `KTRAP_FRAME` struct directly, so the compiler picks the right offset —
no hardcoding of 0x168 in code. `KTHREAD::TrapFrame` is not exposed in public WDK headers
and is read with a hardcoded 0x090 offset plus `MmIsAddressValid` guards.

## Shellcode Layout (119 bytes)

Written to `base+0x200` (wiped PE header area, page is RWX from `SEC_COMMIT` mapping).

```
Offset  Instruction                    Purpose
------  -----------                    -------
0x00    push rax..r15, pushfq          Save ALL GPRs + flags (14 regs + RFLAGS)
0x1D    mov rbp,rsp / and rsp,-16      Align stack to 16 bytes
0x24    sub rsp,0x20                   Shadow space for x64 calling convention
0x25    mov rcx, <dllBase>             [PATCHED @ +37] hInstance param
0x2F    mov edx, 1                     DLL_PROCESS_ATTACH
0x34    xor r8d, r8d                   lpReserved = NULL
0x37    mov rax, <entryPoint>          [PATCHED @ +55] DllMain address
0x41    call rax                       DllMain(base, 1, NULL)
0x43    mov rax, <base+0x100>          [PATCHED @ +67] marker address
0x4D    mov byte [rax], 0x42           Write success marker
0x50    mov rsp, rbp                   Restore stack
0x53    popfq, pop r15..rax            Restore ALL GPRs + flags
0x6D    jmp qword [rip+0]              [PATCHED @ +111] jump back to original RIP
0x6F    dq <originalRip>               8-byte absolute address
```

Total: 119 bytes. Position-independent. All addresses patched at build time from kernel.

## Thread Selection Priority

Non-main threads only (skip index 0). Must be in Waiting state (`ThreadState == 5`).
Scored by `WaitReason` — values from the `_KWAIT_REASON` enum in `ntddk.h`:

| Priority | WaitReason | Value | Why |
|----------|-----------|-------|-----|
| 3 (best) | `WrQueue` | 15 (0x0F) | Idle thread-pool worker — waiting in `NtRemoveIoCompletion`. Clean trap frame, no external side-effects on resume. |
| 2 | `UserRequest` | 6 | Waiting on user-space event via `NtWaitForSingleObject`. Trap frame valid. |
| 2 | `WrUserRequest` | 13 | Variant of the above. |
| 2 | `WrDelayExecution` | 11 | Sleeping via `NtDelayExecution`. Alertable. |
| 1 | any other Waiting | — | Fallback. |

**Historical bug (fixed)**: earlier code hardcoded `WR_QUEUE = 13`, but 13 is actually
`WrUserRequest` — real WrQueue is 15. As a result WrQueue threads always scored 1 and
never got picked. Fixed by re-reading the `_KWAIT_REASON` enum.

## Retry Logic

Up to 15 attempts at 2-second intervals. Each attempt re-enumerates threads (state may
change) and picks the best current candidate. `HijackThread` returns `STATUS_UNSUCCESSFUL`
early if `TrapFrame` is null, invalid, or contains a non-user-mode RIP — outer loop moves
to the next candidate.

## BE Detection Safety Analysis

### BEDaisy Kernel-Mode — SAFE

| Check | What It Does | Why We're Safe |
|-------|-------------|----------------|
| `PsSetCreateThreadNotifyRoutine` | Fires on thread CREATION only, checks start address | We don't create threads — we modify an existing one. |
| Kernel APC stack walk | `RtlWalkFrameChain` on system threads | Only targets SYSTEM threads. Our polling thread exits before the scan gate opens. |
| Function integrity checks | Compares first bytes of specific APIs | Checked (from local reversal): `MmGetSystemRoutineAddress`, `MmIsAddressValid`, `ZwQuerySystemInformation`, `NtQuerySystemInformation`, `KeInitializeApc`, `KeInsertQueueApc`, `KeInitializeEvent`, `KeSetEvent`, `KeWaitForSingleObject`, `RtlWalkFrameChain`. **NOT checked**: `ZwSuspendThread`, `ZwResumeThread`, `MmCopyVirtualMemory`. And we no longer call ANY thread-context API — we write `KTRAP_FRAME::Rip` directly. |
| `ObRegisterCallbacks` | Strips `THREAD_SUSPEND_RESUME` etc. from user-mode handles | We use kernel handles (`OBJ_KERNEL_HANDLE`) — callbacks don't fire on them. |
| `PsLookupThreadByThreadId` hook check | Checks if function starts with `0x25FF` (`jmp [addr]`) | We CALL it, we don't hook it. |
| Scan gating | 100 ms poll loop inactive until `IOCTL 0x222000` + `0x222004` from BEClient | Irrelevant — we don't trigger any BE scan regardless. |

### BEClient User-Mode — SAFE

| Check | What It Does | Why We're Safe |
|-------|-------------|----------------|
| `SuspendedThread` | `ResumeThread(handle)` → if non-zero, thread was suspended | Sub-millisecond suspend window (~microseconds from kernel side). Scan runs only after multiplayer connect and shellcode arrival — long after our resume. |
| `IllegalRip` | `GetThreadContext` → check if RIP is in non-MEM_IMAGE region > 0x2000 | Shellcode executes in microseconds then `jmp [rip+0]` back to the original RIP (in ntdll = MEM_IMAGE). By the time BE scans, RIP is back to normal. |
| `IllegalCaller` | VEH INT3 on function epilogues, check return addr on stack | Our shellcode calls only DllMain via a direct absolute-address `call rax`. It doesn't touch any BE-hooked API. |
| Signature scan | Scans MEM_MAPPED/MEM_PRIVATE executable pages for strings | Payload is currently a 6-instruction marker write. Any real payload must avoid the strings listed in `BE_SHELLCODE.md` (`"Neck"`, `"Chest"`, `"POSITION"`, `"COLOR"`, etc.). |

## Implementation Details

### Function Resolution

APIs that aren't in the WDK public `.lib` files, resolved at runtime via
`MmGetSystemRoutineAddress`:

- `MmCopyVirtualMemory`
- `ZwSuspendThread`
- `ZwResumeThread`

Everything else (`MmIsAddressValid`, `KeAlertThread`, `PsThreadType`,
`ObOpenObjectByPointer`, `PsLookupThreadByThreadId`, `PsGetProcessExitStatus`) is a
static import from ntoskrnl.

### Kernel Handle Bypass

`ObOpenObjectByPointer` with `OBJ_KERNEL_HANDLE` flag produces a kernel handle that:

- Lives in the system handle table (not any user process's handle table).
- Is invisible to user-mode `NtQueryObject`/`NtQuerySystemInformation`.
- Bypasses `ObRegisterCallbacks` entirely (callbacks only fire for user-mode handle
  operations).
- Has `THREAD_ALL_ACCESS` without restriction.

## File Layout

```
apc.cpp     — Thread hijacking implementation (name kept from original APC approach)
apc.hpp     — Public API: HijackAndInject(PEPROCESS, PVOID EntryPoint, PVOID DllBase)
```

## Callsite (mapper.cpp)

```cpp
s = HijackAndInject(target, entryPoint, dllBase);
```

## Key Takeaways

1. **Direct trap-frame write, not `PsGetContextThread`** — that API is a Microsoft trap
   on modern Windows for pool-mapped drivers.
2. **Deterministic** — no reliance on thread scheduling or alertable waits.
3. **Zero new threads created** — completely invisible to `PsSetCreateThreadNotifyRoutine`.
4. **Sub-millisecond execution window** — thread suspended for ~microseconds total.
5. **Kernel handles** — invisible to all user-mode enumeration and `ObRegisterCallbacks`.
6. **No hooked APIs used** — `ZwSuspend`/`Resume` and `MmCopyVirtualMemory` are NOT in
   BEDaisy's integrity list.
7. **`KeAlertThread` solves the delivery problem** — forces idle threads to return to
   user mode.
