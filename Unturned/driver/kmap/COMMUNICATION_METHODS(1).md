# Kernel ↔ Usermode Communication Methods

## 1. IChooseYou's gDxgkInterface Hook (DETECTED — DO NOT USE)

Source: unknowncheats.me/forum/anti-cheat-bypass/335585

### How It Works

`win32kbase!gDxgkInterface` is a writable function pointer table (not in .rdata).
Hook entry 154 (`DxgkNetDispStopMiracastDisplayDevice`) → your kernel handler.
Call from usermode via `NtGdiDdDDINetDispStopMiracastDisplayDevice`.

```
Usermode                          Kernel
─────────                        ──────
NtGdiDdDDINetDispStopMiracastDisplayDevice()
  → syscall → win32kfull → gDxgkInterface[154]
    → YOUR hooked function receives arbitrary buffer
    → parse command, return data via shared buffer
```

### Implementation (reference only — NOT for use)

```cpp
// Kernel side:
typedef NTSTATUS (*fn_DxgkNetDispStopMiracastDisplayDevice)(PVOID);
fn_DxgkNetDispStopMiracastDisplayDevice original_154 = nullptr;

NTSTATUS HookedComm(PVOID pBuffer) {
    COMM_STRUCT* cmd = (COMM_STRUCT*)pBuffer;
    if (cmd->magic != MY_MAGIC) return original_154(pBuffer);
    // process command...
    return STATUS_SUCCESS;
}

void HookDxgk() {
    PVOID* table = (PVOID*)GetDxgkInterface(); // find gDxgkInterface addr
    original_154 = (fn_DxgkNetDispStopMiracastDisplayDevice)table[154];
    table[154] = (PVOID)HookedComm;
}

// Usermode side:
#include <d3dkmthk.h>
void SendCommand(COMM_STRUCT* cmd) {
    // D3DKMTNetDispStopMiracastDisplayDevice calls the syscall
    // which hits gDxgkInterface[154] → our hook
    NtGdiDdDDINetDispStopMiracastDisplayDevice(cmd);
}
```

### Why BEDaisy Detects This

BEDaisy explicitly validates gDxgkInterface:
- **Report type 20**: Checks gDxgkInterface pointer array integrity
- **Report type 21**: Validates specific entries haven't been modified
- **Report type 22-25**: Additional DirectX interface hook checks

From bedaisy-reversal, the check runs on EVERY system thread scan iteration (~1s interval).

### Verdict: DO NOT USE

We don't need kernel↔user communication right now. Our kernel driver injects and exits.
If we need comms later, better alternatives exist (see below).

---

## 2. HalDispatchTable Hook (DETECTED — DO NOT USE)

Classic method: overwrite `HalDispatchTable[1]` (called via `NtQueryIntervalProfile`).

BEDaisy checks:
- **Report type 27**: HalDispatchTable[1] not pointing to hal.dll range
- **Report type 28**: Additional HAL integrity check

### Verdict: DO NOT USE

---

## 3. Shared Memory (Safe — Use If Needed)

Allocate shared physical page visible to both kernel and usermode.

```
Kernel:
  MmAllocateContiguousMemory → physical page
  MmMapLockedPagesSpecifyCache(UserMode) → usermode VA
  
Usermode:
  Read/write the mapped VA directly (no syscalls needed)
```

**No hooks, no function table modification, no detectable artifacts.**
BEDaisy cannot distinguish shared memory from normal allocations.

Problem: how does usermode find the shared VA? Options:
- Write VA to a known registry key
- Write VA to a temp file
- Encode in window title / named event name
- Use NtQuerySystemInformation BigPool tag to find the allocation

### Verdict: SAFE — recommended if we need persistent comms

---

## 4. IOCTL via Third-Party Driver (Risky)

Find a legitimate signed driver with a vulnerable IOCTL handler.
Send IOCTL from usermode → driver interprets as your command.

**Problem**: BEDaisy monitors DeviceIoControl calls to known drivers.
Also requires the third-party driver to actually be loaded.

### Verdict: RISKY — not recommended

---

## 5. Our Approach: No Communication Needed

Current architecture:
1. kmap.sys maps payload into Unturned via ZwMapViewOfSection
2. Fires APC to call DllMain → payload initializes itself
3. kmap.sys system thread exits (via PsTerminateSystemThread)
4. Payload runs independently in usermode — no kernel comms needed

If future versions need kernel read/write (e.g., for reading protected game memory):
→ Use shared memory (method 3)
→ Or use a second BYOVD that provides arbitrary R/W (but adds detection surface)
