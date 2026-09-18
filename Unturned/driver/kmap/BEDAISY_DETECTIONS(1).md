# BEDaisy Kernel Detection Analysis

Source: UC thread 505404 "BattlEye kernel module detection in-depth analysis"

## Architecture

- BEDaisy collects detection data into internal linked lists
- Data only sent to BEClient when BEClient initiates IRP_MJ_READ of specific length
- BEClient then forwards to BEServer for ban decision
- **Key insight**: BEDaisy scans run CONTINUOUSLY from load, but reports queue until BEClient reads them

## Detections Relevant to Us

### Report Type 1: System Thread Start Address Detection (CRITICAL)

BEDaisy iterates all system threads via SystemProcessInformation. If a thread's start address
is NOT in any known module's address range (tracked via LoadImageNotify linked list), it reports:
- StartAddress
- 64 bytes of content at start address
- Thread running time (creation → now)
- Thread creation time

**Impact on us**: Our `PollingThreadRoutine` runs from unmapped pool memory (kdmapper doesn't
register with PsLoadedModuleList). Start address is NOT in any module → DETECTABLE.

**Mitigation**: Thread exits after injection completes. If BEDaisy's scan cycle hasn't hit
our thread before it terminates, it won't be found. User confirms kdmapper undetected on Unturned.

### Report Type 14: System Thread Stack Detection (CRITICAL)

BEDaisy inserts APC into ALL system threads, calls RtlWalkFrameChain, checks each caller
address against known module ranges. Also checks for:
- Blacklisted patterns in callers
- Multiple unconditional jumps (>= 5)
- int 3 breakpoints
- nop slides

**Impact on us**: Our thread's stack frames are in unmapped memory → detectable.

**Mitigation**: Same as above — thread must exit before APC-based stack scan fires.

### Report Type 3: Game Process Thread Creation (SAFE)

Monitors thread creation IN the game process. Reports if thread start address isn't in
any game module.

**Impact on us**: We use **thread hijacking** (`HijackAndInject`) — modify an existing
waiting thread's `KTHREAD::TrapFrame->Rip` directly. Does NOT create a new thread.
**We are safe from this check.**

### Report Type 7: Callback Address Range Detection

Checks if registered callbacks (process/thread/registry/image) are in known module ranges.

**Impact on us**: We do not register any callbacks. `callbacks.cpp` was removed
in the cleanup pass — it was never called. **We are safe.**

### Report Type 15: Hidden Driver Detection

Traverses \\Device directory, finds all Device objects, matches them against \\Driver and
\\FileSystem Driver objects. Reports if a Device's Driver is hidden.

**Impact on us**: kdmapper doesn't create a device object. **We are safe.**

### Report Type 8: PhysicalMemory Reference Detection

Detects apps referencing \\device\\PhysicalMemory.

**Impact on us**: We don't use physical memory access. **We are safe.**

## Detections NOT Relevant to Us

| Type | Detection | Why safe |
|------|-----------|----------|
| 0 | Dispatch function integrity | We don't hook dispatch functions |
| 4 | PsLookupThreadByThreadId hook | We don't hook this |
| 5 | Dispatch function hook | We don't hook dispatches |
| 6 | Callback hook detection | We don't hook callbacks |
| 9 | Syscall integrity | We don't hook syscalls |
| 10 | Driver handle open failed | We don't hide driver objects |
| 11 | Module abnormal instruction | Scans registered modules only |
| 12-13 | DxgCoreInterface | We don't touch dxgkrnl |
| 18 | BE driver integrity | Self-check, not about us |
| 19 | Module IAT hook | We don't hook IATs |
| 20-25 | Win32k tables | We don't touch win32k |
| 26 | PCI device | We're not DMA hardware |
| 27-30 | HAL tables | We don't hook HAL |
| 31-32 | FltMgrMsg | We don't hook filter comms |

## Detection of Our Payload DLL (Usermode)

BEClient (not BEDaisy) handles usermode scanning:
- Walks entire address space via NtQueryVirtualMemory
- Checks executable pages in MEM_PRIVATE regions
- Looks for PE headers (MZ signature)

**Our mitigations:**
- Section-backed mapping (SEC_COMMIT) → appears as MEM_MAPPED, not MEM_PRIVATE
- PE header wiped after mapping (no MZ to find)
- Not in PEB.Ldr (but MEM_MAPPED sections aren't expected there)
- Per-section protections (not blanket RWX)

## Upload Types (Server-Streamed Patterns)

BEClient uploads blacklist patterns to BEDaisy via IRP_MJ_WRITE:
- Type 0: General patterns (matched against report list entries)
- Type 1: Callback header patterns (64-byte headers of callbacks)
- Type 2: Syscall header patterns (64-byte headers of syscall functions)
- Type 3: BE self-integrity patterns
- Type 4: Dxgkrnl internal function pattern
- Type 5: InfinityHook detection trigger

**Key insight**: Patterns are SERVER-STREAMED and can change without driver recompile.
Specific byte sequences in our code could be flagged if they match known cheat signatures.

## Hook Detection Method (All Types)

BEDaisy only detects these hook forms in the first 64 bytes:
```
FF 25 XX XX XX XX           - jmp [addr]
48 B8 XX XX XX XX XX XX XX XX FF E0  - mov rax, imm; jmp rax
```

**Only tracks 1 jump** — will not follow chains.
Middle hooks and tail hooks are NOT detected.
Non-standard hook forms (push/ret, call, etc.) are NOT detected.

## Timing Implications

1. BEDaisy loads and starts scanning IMMEDIATELY (before game process)
2. System thread scans run on a timer/loop (exact interval unknown from this analysis)
3. Reports queue in internal linked lists
4. BEClient must be loaded AND issue IRP_MJ_READ before reports reach server
5. **Our window**: If system thread exits before BEDaisy's next scan cycle, thread is never caught

## BEDaisy Report Mechanism (from BlindEye / zouxianyu)

BEDaisy's report function queues detection data into linked lists:

```cpp
// Custom report (no encryption):
Node = ExAllocatePool(PagedPool, 24);  // 24-byte node
Data = ExAllocatePool(PagedPool, Size); // variable-size payload
// Appends to linked list (Head/Tail), protected by g_Mutex

// Normal report (XOR encrypted, tag 'EB'):
Node = ExAllocatePoolWithTag(PagedPool, 24, 'EB');
Data = ExAllocatePoolWithTag(PagedPool, Size + 4, 'EB'); // +4 for key
// XOR encryption: key = RtlRandomEx(TickCount)
// cipher[i] = plaintext[i] ^ (i) ^ byteKey ^ 0xA5; byteKey = ~cipher[i]
```

**Key insight**: If ExAllocatePool returns NULL, the report is SILENTLY DROPPED.
No crash, no fallback, no retry. Detection data just disappears.

### BlindEye Bypass Method

1. Register PsSetLoadImageNotifyRoutine to watch for BEDaisy.sys loading
2. IAT-hook MmGetSystemRoutineAddress in BEDaisy's import table
3. When BEDaisy resolves ExAllocatePool/ExAllocatePoolWithTag, return hook function
4. Hook function: if (PoolType == PagedPool && Size == 24) return nullptr;
5. Result: ALL report nodes fail to allocate → NO detections ever sent to server

**This is a nuclear option** — completely disables BEDaisy's reporting.
Requires loading BEFORE BEDaisy (more complex setup).

### BEDaisy IAT Structure

BEDaisy only imports a few functions directly:
- MmGetSystemRoutineAddress
- FltGetRoutineAddress

ALL other imports are resolved dynamically via MmGetSystemRoutineAddress at runtime.
This makes IAT hooking MmGetSystemRoutineAddress a single point of control.

## Recommendations

1. **Minimize system thread lifetime** — inject fast, exit thread immediately
2. **Don't register kernel callbacks** — address range check will flag them
3. **Don't create threads in game process** — use APC to existing threads (already doing this)
4. **Payload must not have standard hook patterns** in first 64 bytes of any function
5. **Keep payload as MEM_MAPPED + header wiped** (already doing this)
6. **Consider: move polling logic to timer DPC** instead of system thread (lower profile)
7. **Nuclear option**: Hook BEDaisy's ExAllocatePool to block report allocation (BlindEye method)

## Alternative Bypass: Report Blocking vs Stealth

| Approach | Pros | Cons |
|----------|------|------|
| Stealth (our approach) | Lightweight, no BE hooking needed | Must exit thread before scan |
| BlindEye (report block) | Disables ALL kernel detections | Must load before BEDaisy, more complex |
| Hybrid | Best of both worlds | Most code to maintain |

## BEDaisy Scan Timing (CONFIRMED from devirtualized binary)

### Scan Loop Architecture

BEDaisy's system thread runs a 100ms poll loop:
```
PsCreateSystemThread → scan thread starts
  ↓
  loop:
    KeDelayExecutionThread(-1000000)   // 100ms sleep
    check flag_1 → if cleared, skip scan
    check flag_2 → if cleared, skip scan
    iterate memory/thread list
    goto loop
```

### Scan Gating: Two Boolean Flags

The scan loop does NOT execute until **both** flags are set. These flags are set when:
1. BEService opens `\\.\BattlEye` and sends **IOCTL 0x222000** (init handshake)
2. BEClient opens `\\.\BattlEye` and sends **IOCTL 0x222004** (enables scanning)

**This means**: BEDaisy's kernel memory scanning is INACTIVE until BEClient initializes.
Only the registered callbacks (process/thread/image notify, ObRegisterCallbacks) are
active from driver load.

### IOCTL Interface

| IOCTL | Value | Purpose |
|-------|-------|---------|
| Init | 0x222000 | BEService registration/handshake |
| Data | 0x222004 | Enables scan flags + data exchange |

### Communication with BEClient

| IRP | Purpose |
|-----|---------|
| IRP_MJ_CREATE | BEClient opens device (PID validation) |
| IRP_MJ_READ | BEClient receives scan commands from BEDaisy |
| IRP_MJ_WRITE | BEClient sends scan results to BEDaisy |
| IRP_MJ_DEVICE_CONTROL | IOCTL dispatch (init + data) |

BEDaisy uses `KeInitializeApc` + `KeInsertQueueApc` to wake BEClient when work is ready.

### Integrity-Checked Kernel Modules

BEDaisy validates these modules haven't been patched (string refs at 0x3313B8):
- dxgkrnl.sys, win32k.sys, win32kfull.sys, win32kbase.sys
- hal.dll, ACPI.sys, pci.sys, ntdll.dll

And these function tables:
- HalDispatchTable, HalPrivateDispatchTable
- DxgCoreInterface, KWIN32KENG_INTERFACE

### Validated Kernel APIs (integrity-checked each scan cycle)

BEDaisy hashes the first N bytes of these functions (report if modified):
- MmGetSystemRoutineAddress (index 0)
- MmIsAddressValid (index 1)
- ZwQuerySystemInformation (index 2)
- NtQuerySystemInformation (index 3)
- KeServiceDescriptorTable (index 4)
- KeInitializeEvent (index 7)
- KeInitializeApc (index 8)
- KeInsertQueueApc (index 9)
- RtlWalkFrameChain (index 10)
- KeSetEvent (index 11)
- KeWaitForSingleObject (index 12)

### What's Active BEFORE BEClient Init

| Component | Active? | Threat to us? |
|-----------|---------|---------------|
| PsSetCreateProcessNotifyRoutineEx | YES | LOW — just records our PID |
| PsSetCreateThreadNotifyRoutine | YES | LOW — we don't create threads in game |
| PsSetLoadImageNotifyRoutine | YES | SAFE — SEC_COMMIT doesn't trigger |
| ObRegisterCallbacks | YES | MEDIUM — strips handle access rights |
| Minifilter (altitude 321000) | YES | SAFE — we don't touch BE files |
| 100ms memory scan loop | **NO** | NOT active until IOCTL 0x222004 |
| System thread enumeration | **NO** | NOT active until scan flags set |
| Stack frame walk (APC-based) | **NO** | NOT active until scan flags set |

### Revised Timeline

```
[kdmapper loads kmap.sys]
  ↓ Callbacks active, scan loop GATED (flags=0)
[kmap system thread starts polling for Unturned]
  ↓ Thread is in unmapped memory BUT BEDaisy is NOT scanning yet
[Unturned.exe launches]
  ↓ PsSetCreateProcessNotifyRoutineEx fires, BEDaisy records PID
[mono-2.0-bdwgc.dll loads]
  ↓ We detect it, wait 1s
[kmap injects payload, system thread EXITS]
  ↓ Thread gone — even if scanning starts later, nothing to find
[BEClient_x64.dll loads]
  ↓ Init() called, IOCTL 0x222004 sent
[Scan flags SET — BEDaisy 100ms loop NOW ACTIVE]
  ↓ But our thread already exited. Nothing left to detect.
```

**For us**: Our system thread exists only during the GATED period when BEDaisy
is NOT scanning. By the time BEClient enables scanning, our thread is gone.
User confirms: kdmapper undetected on Unturned as of July 2026.
