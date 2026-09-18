# Injection Timing Research

## Unturned Process Module Load Order (confirmed)

| Phase | Module | Timing (approx) |
|-------|--------|-----------------|
| 1 | ntdll.dll, kernel32.dll, kernelbase.dll | Immediate (loader) |
| 2 | ucrtbase.dll, vcruntime140.dll | Immediate (CRT init) |
| 3 | UnityPlayer.dll | Near-instant (load-time dep of EXE) |
| 4 | d3d11.dll, dxgi.dll | Early (graphics init by UnityPlayer) |
| 5 | mono-2.0-bdwgc.dll | After graphics (loaded by UnityPlayer) |
| 6 | Managed assemblies (Assembly-CSharp.dll etc) | After mono init |
| 7 | BEClient_x64.dll | LAST (loaded by game managed code) |

## Unturned Details

- Unity 2021.3.29f1 LTS
- Scripting backend: Mono (NOT IL2CPP)
- Mono DLL: `mono-2.0-bdwgc.dll` (in `Unturned_Data/MonoBleedingEdge/EmbedRuntime/`)
- Rendering: DirectX 11 (d3d11.dll, dxgi.dll)
- BattlEye: BEClient_x64.dll (in `BattlEye/` subdirectory)

## BattlEye Architecture

1. BEService.exe (Windows service) opens `\\.\pipe\BattlEye`
2. BEService loads BEDaisy.sys via ZwLoadDriver
3. BEDaisy registers kernel callbacks immediately:
   - PsSetCreateProcessNotifyRoutineEx
   - PsSetCreateThreadNotifyRoutine
   - PsSetLoadImageNotifyRoutine (fires for SEC_IMAGE sections only)
   - ObRegisterCallbacks (handle access filtering)
   - Minifilter at altitude 321000
4. Game process created by BEService
5. Game initializes (all DLLs load)
6. Game managed code loads BEClient_x64.dll and calls Init export
7. BEClient starts memory scanning

## Why Our Approach Works

- We use ZwMapViewOfSection with SEC_COMMIT (pagefile-backed) — NOT SEC_IMAGE
- PsSetLoadImageNotifyRoutine does NOT fire for pagefile sections
- Manual mapping = no LdrLoadDll call = not visible to standard module enumeration
- PE header wiped after mapping
- Section-backed memory appears as MEM_MAPPED (not MEM_PRIVATE)
- Not in PEB.Ldr — but MEM_MAPPED sections aren't expected there

## Current Implementation

```
1. Detect Unturned.exe (poll every 2s)
2. Poll for mono-2.0-bdwgc.dll in PEB every 500ms (up to 30s)
3. Wait 1s after mono appears (domain init settle time)
4. Inject via MapUserDll
```

## Injection Window

```
[mono loads] -----> [+1s inject here] -----> [BEClient loads] -----> [scanning begins]
     ^                     ^                        ^                       ^
  SAFE ZONE          OUR INJECTION             TOO LATE?              DEFINITELY TOO LATE
```

## Key Constraints

- Too early: DLLs not loaded (vcruntime140 missing at 500ms — confirmed)
- Too late: BEClient scanning active (but see below — timing is generous)
- Sweet spot: After mono, before BEClient
- **RESOLVED**: BEClient scanning architecture fully reversed (see below)

---

## CRITICAL FINDING: BEClient Scanning Requires Server Connection

**BEClient_x64.dll does NOT scan memory autonomously.**

### Scanning Architecture

BEClient scanning operates in TWO layers, both requiring server interaction:

#### Layer 1: Game-driven `run()` callback

When the game loads BEClient_x64.dll, it calls the `Init()` export:
```c
Init(integration_version, becl_game_data*, becl_be_data*)
```

Init returns a struct containing a `run()` function pointer. The game calls `run()`
every tick/frame. BEClient uses this pump to:
- Process packets from BEService (via `\\.\pipe\BattlEye`)
- Execute internal IOTask-based scans
- Send heartbeat every 30 seconds

**This only starts when the game's managed code calls Init AND begins pumping run().**
For Unturned: happens after BEClient_x64.dll loads (phase 7 in module order).

#### Layer 2: Server-streamed shellcode (BEClient2)

The heavy memory scanning (NtQueryVirtualMemory full sweep, string sigs, thread RIP checks)
is performed by SHELLCODE delivered from BEServer:

```
Game connects to multiplayer server
  → BEClient Init() + handshake (INIT 0x00, START 0x02, REQUEST 0x04)
  → BEServer decides to send detection shellcode (selective, behavior-based)
  → Shellcode arrives via named pipe (fragmented if >0x400 bytes, XOR encrypted)
  → BEClient allocates MEM_PRIVATE region
  → Executes shellcode → NtQueryVirtualMemory scanning begins
```

**KEY**: Not all clients receive shellcode at the same time. Deployment is:
- Selective (debugging tools detected → more aggressive scanning)
- Behavior-triggered (not on fixed schedule)
- Server-controlled (BEServer decides per-client)

### BEClient2 Shellcode Timing (once received)

| Phase | Timing |
|-------|--------|
| Initial delay after allocation | Sleep(100ms) |
| RDTSC calibration | Sleep(1000ms) |
| CPUID benchmark | 26260 iterations, Sleep(10ms) between passes |
| Memory scan loop | NO sleep — full sweep ASAP |
| TCP table scan | 500 iterations × 10ms = ~5s total |
| Tick integrity check | Sleep(1000ms) |

The memory scan loop itself has NO internal delay — it walks the entire address space
as fast as possible once initiated.

### Protocol Details

| Packet ID | Name | Direction | Purpose |
|-----------|------|-----------|---------|
| 0x00 | INIT | Server→Client | Initial handshake |
| 0x02 | START | Server→Client | Session GUID setup |
| 0x04 | REQUEST | Server→Client | Detection requests + shellcode delivery |
| 0x05 | RESPONSE | Client→Server | Scan results / reports |
| 0x09 | HEARTBEAT | Bidirectional | Keep-alive (30s interval) |

### What This Means For Our Timing

```
[mono loads]  →  [+1s INJECT]  →  [BEClient loads]  →  [Init + handshake]  →  [SERVER JOIN]  →  [shellcode arrives]
     |                |                  |                     |                    |                    |
   0.5-2s          OUR SPOT          5-15s later         Immediate              USER ACTION        SCAN STARTS
   from start                        from start          after load              (manual)           (if server sends)
```

**Our injection window is MASSIVE:**
- mono appears: 0.5-2s after process creation
- We inject: 1.5-3s after process creation
- BEClient loads: 5-15s after process creation
- BEClient Init: immediately after load
- Memory scanning: ONLY after joining a multiplayer server AND receiving shellcode

**In single-player / main menu: NO usermode memory scanning occurs. Ever.**

### Remaining Threat: BEDaisy Kernel Scans

BEDaisy.sys scans run from driver load regardless of server connection:
- System thread start address check (report type 1) — ~1s interval
- Thread stack frame walk (report type 14)
- Game thread creation monitoring (report type 3)
- Kernel function integrity validation

These are the ONLY scans active before server connection. Our mitigation is
the **scan-gate window**: BEDaisy's 100ms memory-scan loop only fires after
BEClient sends IOCTL 0x222004 (post-Init). Our polling thread injects and
exits during that gated window, so by the time the scan flags turn on there
is nothing left in unmapped kernel memory to find.

## DLLs Payload Needs (current + future)

| DLL | Purpose | When loaded |
|-----|---------|-------------|
| kernel32.dll | CreateFileW, WriteFile | Always loaded |
| vcruntime140.dll | CRT runtime | After UnityPlayer loads |
| ucrtbase.dll | Universal CRT | After UnityPlayer loads |
| mono-2.0-bdwgc.dll | Mono API (future: hooking) | Phase 5 |
| d3d11.dll | Rendering (future: ImGui) | Phase 4 |

## Build Notes

- Payload built with /MD (dynamic CRT) — needs vcruntime140.dll in PEB
- API set resolution: api-ms-win-crt-* -> ucrtbase.dll
- ResolveForward: follows kernel32 forwards to ntdll via PEB lookup
