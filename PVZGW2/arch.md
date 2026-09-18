# PvZ GW2 Internal Cheat — Architecture

> Target: Plants vs. Zombies Garden Warfare 2 (PvZ GW2)
> Engine: Frostbite 3
> Anti-cheat: EA AntiCheat (EAAC) — kernel-level (user calls it "Javelin"; see §2)
> Stack: kdmapper + kernel driver (reused from Unturned) + internal payload (MinHook + ImGui)

---

## 1. Goal

Internal cheat for PvZ GW2 delivered through the **same driver-loading pipeline** already
working for Unturned:

```
Loader (kdmapper)  →  vulnerable-driver load  →  kmap.sys (kernel driver)
                                                      │
                              polls for game process, manual-maps payload
                                                      │
                                          payload.dll (internal cheat)
                                              │
                              MinHook (game function hooks) + ImGui (DX11 overlay)
```

Three artifacts, one delivery chain. The driver and loader are ~90% reused; the
payload is new (Frostbite, not Mono/Unity).

---

## 2. Target Analysis

### 2.1 Game
- **Engine**: Frostbite 3 (EA proprietary, native C++).
- **Rendering**: Direct3D 11 (GW2 predates Frostbite's DX12 path).
- **Process**: `PvZGW2.exe` — **VERIFY at runtime** (Task Manager / Process Explorer).
- **Distro**: EA app (formerly Origin). Install dir reported as
  `C:\Program Files\EA Games\Plants vs Zombies Garden Warfare 2` (path needs
  confirmation on the build machine).

### 2.2 Anti-cheat
- PvZ GW2 received **EA AntiCheat (EAAC)** in a March 2024 update — a **kernel-mode**
  driver. EAAC only runs while a protected game runs, and unloads on exit.
- **Correction**: "Javelin" is EA's *newer* kernel AC (Battlefield 6, 2025). PvZ GW2
  ships **EAAC**, not Javelin. Same threat class (kernel driver, integrity checks),
  so the bypass strategy is identical in shape, but assume EAAC's detection surface.
- Implication: the cheat is **manual-mapped by our own kernel driver** so nothing
  touches the process's loader path / PEB the way a normal `LoadLibrary` would.
  This is the same posture that already works against BattlEye in Unturned.

### 2.3 What this means for the payload
- Frostbite is **not Mono/Unity**. No `mono-*` handshake. The payload talks to the
  game through **Frostbite's native reflection / type system** (`fb::TypeInfo`,
  virtual tables) and standard Win32 hooks.
- Hooking strategy:
  - **DX11 `Present`** — for the ImGui overlay (same as every DX11 internal).
  - **Frostbite game methods** — aimbot / ESP / movement, via MinHook on VTable
    slots or known symbols. Requires reversing the game's classes (see §7).

---

## 3. Component Architecture

### 3.1 Loader — reuse kdmapper (`svchost32.exe`)
- Unchanged from Unturned. Loads `runtime.sys` via a vulnerable-driver primitive,
  cleans PiDDBCacheTable / MmUnloadedDrivers / g_KernelHashBucketList traces.
- No code changes; only the **driver binary** it maps differs.

### 3.2 Kernel driver — reuse `driver/kmap` (Unturned)
Source: `C:\Users\Shadow\Documents\IC2\IC2\Unturned\driver\kmap`

| File | Role | GW2 change |
|------|------|-----------|
| `main.cpp` | DriverEntry; read payload; poll for target; spawn polling thread | `TARGET_PROCESS` → `PvZGW2.exe`; payload path; wait-for-module predicate |
| `mapper.cpp` | `MapUserDll`: pagefile section → map → copy → reloc → imports → thread-hijack | none (generic PE mapper) |
| `process.cpp` | `FindProcessByName`, `GetUserModuleBase`, export resolution | none |
| `apc.cpp` | Thread hijacking (suspend + `KTRAP_FRAME::Rip` rewrite + alert) | none |
| `klog.hpp` / `beep.hpp` | file logging / beep signals | optional retheme |
| `pe.hpp`, `nt_structs.hpp`, `mapper.hpp`, `process.hpp` | PE + NT structs | none |

Changes required:
1. `#define TARGET_PROCESS "PvZGW2.exe"` (verify).
2. Payload read path: reuse the **disk** variant (`main(1).cpp` reads
   `C:\Users\Public\payload.dll`) — simpler than the named-section variant and
   already proven as the shipped `runtime.sys`.
3. **Wait predicate**: Unturned waits for `mono-2.0-bdwgc.dll`. For Frostbite,
   wait for the game's main image (`PvZGW2.exe`) to be fully mapped **and** a
   stable hook target (e.g. the D3D11 device / swapchain) to exist. First pass:
   inject after `PvZGW2.exe` module is present + short settle delay; refine after
   the payload can self-locate D3D11.

### 3.3 Payload — NEW internal cheat (`payload.dll`)
Native x64 DLL. **Do not use Mono/Unity imports** — Frostbite is plain C++.

```
DllMain
 └─ spawn worker thread (return fast; DllMain must not block or call loader APIs)
     ├─ wait for game to reach main menu / first Present
     ├─ init MinHook (MH_Initialize)
     ├─ init ImGui (DX11 backend) + hook IDXGISwapChain::Present (VTable)
     ├─ resolve Frostbite types (fb::TypeInfo) for the features we hook
     ├─ install game hooks (aimbot / ESP / etc.)
     └─ render loop: ImGui frames inside Present hook
```

Libraries (statically linked / vendored, all embedded-friendly):
- **MinHook** — user-mode inline hooks (MH_CreateHook + MH_EnableHook).
- **ImGui** + `imgui_impl_dx11` / `imgui_impl_win32` — overlay UI.
- **D3D11** — swapchain/present capture.

---

## 4. Data / Feature Layer (Frostbite)

Initial feature set (each gated behind a `#define` so we ship a minimal MVP first):

| Feature | Hook target | Notes |
|---------|-------------|-------|
| Overlay menu | `IDXGISwapChain::Present` (VTable index 8) | ImGui; toggled with Insert |
| ESP | Frostbite `ClientPlayer`/`ClientSoldierEntity` iteration + world→screen | needs class/offset resolution |
| Aimbot | `ClientSoldierEntity` aim methods | highest reversing effort |
| Misc | `fb::Input` for key handling, speed/jump patches | after MVP |

**MVP scope (first shippable)**: overlay menu + one provable hook (Present) to
confirm the full pipeline (driver → map → DllMain → hook → overlay renders). Game
feature hooks land incrementally after that.

---

## 5. Build & Toolchain Requirements

| Component | Toolchain | Status |
|-----------|-----------|--------|
| Loader | prebuilt `svchost32.exe` (kdmapper) | ✅ reused |
| Driver | WDK + MSVC `cl/link` via `compile.bat` | ⚠️ **WDK not installed** (no `km/` headers in Windows Kits). Must install WDK to rebuild `runtime.sys`. |
| Payload | MSVC (BuildTools present) + MinHook + ImGui | ✅ toolchain present; MinHook must be vendored |
| MinHook | vendor source (`MinHook.h`, `HDE` + hook core) | ⚠️ not vendored yet — add to `PVZGW2/vendor/` |

**Blocking prerequisite**: WDK kernel headers/libs are missing (`10.0.26100.0\km`
absent). Until WDK is installed, the driver **cannot be rebuilt** — the existing
`runtime.sys` works only for Unturned (`TARGET_PROCESS` + payload expectations).
This must be resolved before a GW2 build.

---

## 6. Directory Layout (planned)

```
PVZGW2/
├── arch.md                 ← this document
├── driver/                 ← copy of Unturned driver, adapted
│   ├── main.cpp            (TARGET_PROCESS=PvZGW2.exe, disk-read payload)
│   ├── mapper.cpp process.cpp apc.cpp
│   ├── *.hpp (pe, nt_structs, klog, beep, mapper, process)
│   └── compile.bat         (needs WDK)
├── payload/                ← NEW internal cheat
│   ├── main.cpp            (DllMain + worker + Present hook)
│   ├── hooks.cpp/h         (MinHook wrappers)
│   ├── overlay.cpp/h       (ImGui init/render)
│   └── CMakeLists.txt / build.bat
├── loader/                 ← kdmapper binary (svchost32.exe)
├── vendor/
│   ├── minhook/            (MinHook source)
│   └── imgui/              (ImGui + DX11/Win32 backends)
└── build/                  (output: payload.dll, runtime.sys, loader exe)
```

---

## 7. Open Questions / Risks

1. **Exact exe name** — assume `PvZGW2.exe`; confirm before first inject.
2. **WDK absent** — blocks driver rebuild. Install
   [WDK](https://learn.microsoft.com/windows-hardware/drivers/download-the-wdk).
3. **Frostbite offsets** — class layouts / world→screen / aim hooks need reversing
   (Cheat Engine + ReClass / IDA on `PvZGW2.exe`). This is the long pole, not the
   injection pipeline.
4. **EAAC detection** — kernel manual-mapping avoids the classic loader path, but
   EAAC may still scan for VTable swaps / inline hooks (MinHook patches). MVP first,
   then measure; consider VEH-based or pointer-swap hooking if MinHook sig is flagged.
5. **D3D11 device resolution** — Present VTable hook needs a live swapchain; hook
   `D3D11CreateDeviceAndSwapChain` early, or walk `IDXGIFactory` → swapchain.

---

## 8. Milestones

1. **M0 — toolchain**: install WDK; confirm driver rebuilds from copied source.
2. **M1 — pipeline port**: driver polls `PvZGW2.exe`, maps a trivial payload
   (DllMain writes marker byte) — proves end-to-end injection.
3. **M2 — overlay**: payload inits ImGui/DX11 and renders a menu on Present hook.
4. **M3 — features**: Frostbite type resolution → ESP → aimbot (incremental).
5. **M4 — hardening**: EAAC detection pass; hook technique review; cleanup.

**Decision needed before coding starts**: confirm WDK install (or accept reusing a
prebuilt driver if one exists for GW2), and confirm the exact game exe name.
