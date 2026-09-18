# BEClient Shellcode Analysis

Source: weak1337/BE-Shellcode (GitHub), secret.club, research agents

## Architecture: Scanning is Server-Streamed

**CRITICAL**: BEClient_x64.dll does NOT scan memory itself. The scanning is done by
shellcode streamed from BEServer to BEClient via named pipe.

```
Game connects to multiplayer server
  → BEClient handshake with BEService (INIT 0x00, START 0x02, REQUEST 0x04)
  → Heartbeat begins (30s interval, ID 0x09)
  → BEServer streams shellcode modules to BEService
  → BEService relays to BEClient via \\.\namedpipe\Battleye (XOR encrypted)
  → BEClient allocates shellcode in MEM_PRIVATE (outside any module)
  → Shellcode executes → NtQueryVirtualMemory scanning begins
```

**KEY IMPLICATION**: No server connection = no memory scanning.
Main menu only = safe from usermode scans.

## What the Shellcode Scans (from weak1337 source)

### 1. Thread Scan (thread_scan.cpp)

```cpp
// For each thread in game process:
// 1. Try ResumeThread — if result > 0, thread was suspended → report
// 2. Get thread context, check RIP via NtQueryVirtualMemory
// 3. If RIP memory is NOT MEM_IMAGE and region > 0x2000 → report IllegalRip
```

**Impact on us**: Only fires if a thread's RIP is IN our memory at scan time.
Our APC-based DllMain executes briefly on an existing thread then returns.
After DllMain returns, the thread's RIP is back in normal game code. SAFE.

### 2. Signature Scan (sigs.cpp)

Walks ENTIRE address space via NtQueryVirtualMemory:
- For each committed page with execute permission (PAGE_EXECUTE*)
- If type is MEM_MAPPED or MEM_PRIVATE
- Scans for blacklisted string patterns

```cpp
// Scanned patterns (from source):
"ojects\\PUBGChinese", "BattleGroundsPrivate_CheatESP",
"Neck", "Chest", "Mouse 1", "PlayerESPColor", "HackMachine",
"VisualHacks.net", ".rdata$zzzdbg", "D3D11Present initialised",
"[ %.0fM ]", "[hp:%d]%dm", "POSITION", "COLOR"
// Plus server-streamed patterns that change dynamically
```

**Impact on us**: Our payload's .text section WILL be scanned (it's MEM_MAPPED + executable).
As long as our code doesn't contain these blacklisted strings, we're fine.
CAUTION: Server-streamed patterns can change without notice.

### 3. Module Blacklist (modules.cpp)

```cpp
// Checks if these DLLs are loaded (GetModuleHandleA):
"hal.dll", "nvToolsExt64_1.dll", "ws2detour_x96.dll",
"networkdllx64.dll", "nxdetours_64.dll", "Project1.dll",
"DxtoryMM_x64.dll", "mslib.dll", "frAQBc8W.dll",
"shimloader64.dll", "BE_DLL.dll"

// Checks if these driver devices exist:
"\\\\.\\Beep", "\\\\.\\Null"
```

**Impact on us**: Our payload is NOT in the module list (no LdrLoadDll). SAFE.
Our driver doesn't create a device object. SAFE.
NOTE: \\.\Beep check means our beep.hpp (IoBeep calls) must not be via device open.

### 4. Hidden System Thread Finder (systhreadfinder.cpp)

CPU time accounting detection:
- Takes two SystemProcessInformation snapshots 1 second apart
- Calculates total CPU time delta across all processes
- Compares against GetSystemTimes delta
- If unaccounted CPU time > 0x7270E0 (~7.5M ticks) for 10 consecutive checks → report

**Impact on us**: This detects HIDDEN system threads consuming CPU. Our polling thread
is a NORMAL system thread (visible in SystemProcessInformation). It's not hidden,
just in unmapped memory. This specific check won't catch us.

### 5. VEH Exception Handler (veh.cpp / beshellcode.cpp)

Registers a Vectored Exception Handler that monitors:
- GetAsyncKeyState, GetCursorPos, IsBadReadPtr
- NtUserGetAsyncKeyState, GetForegroundWindow, CallWindowProcW
- NtUserPeekMessage, NtSetEvent
- __stdio_common_vsprintf_s, sqrtf

These functions are set as hardware breakpoints. If our code calls them and triggers
the breakpoint, the VEH checks the return address against known module ranges.

**Impact on us**: Current payload only calls CreateFileW/WriteFile/CloseHandle. SAFE.
Future ImGui/input hooking WILL need to avoid triggering these breakpoints.

### 6. KiUserExceptionDispatcher Hook Check

Checks if KiUserExceptionDispatcher (ntdll) is hooked — detects exception-based hooks.

**Impact on us**: We don't hook exception dispatching. SAFE.

### 7. Function Integrity Check

Validates specific API functions haven't been hooked (checks first bytes).

**Impact on us**: We don't hook any usermode APIs currently. SAFE for now.

## Timing Summary

| Event | Usermode Scanning Active? |
|-------|--------------------------|
| Process created | NO |
| Unity initializing | NO |
| Mono loaded | NO |
| BEClient_x64.dll loaded | NO |
| Main menu visible | NO |
| Connected to multiplayer server | NO (handshaking) |
| Shellcode received from BEServer | NO (allocating) |
| Shellcode executing | **YES — scanning active** |

## What This Means For Us

1. **Our current 10s timing is irrelevant** — usermode scanning doesn't start until server connection
2. **BEDaisy kernel scans ARE active** from driver load (system thread checks, callback checks)
3. **Our injection window is HUGE** — from mono load until server connection
4. **Once on a server**: our payload is scanned. Must not contain blacklisted strings.
5. **MEM_MAPPED + executable IS scanned** by sigs.cpp — but only for patterns, not for existence
