# Changelog

## [2.0.0] — 2024-08-27

### Added
- **PVZGW2 Modules**
  - Full PvZ Garden Warfare 2 injection pipeline
  - GW2 modules added to game detection system in inject.cpp
  - Dedicated kernel driver and payload for Frostbite engine
  - DLL payload auto-detection by file size in download module (prevents wrong DLL injection)
- **UI Framework** (ported from israeliclient 2.0)
  - Custom dark-themed ImGui overlay with 5-tab layout
  - Accent color system (Hamas green #00A651)
  - Background blur via viewport sampling
  - Animated slide-up detail panels
  - Game cards with status indicators
- **Multi-Game Support**  
  - Game-specific driver/payload routing
  - Per-game config (memory section, log paths, driver names)
  - UNTURNED/ROBLOX game cards added (placeholders)
- **Security**
  - Thread-safe game monitor for process detection
  - HMAC key rotation before/after deployment
  - SecureBoot check + admin elevation prompt
  - Self-deletion via batch script on close
- **Discord Integration**
  - Webhook event logging (login attempts, injection start/success/failure)
  - Session token authentication via HMAC
  - Async sender thread with queue
- **Sound Effects**
  - WAV-based UI sounds (tab switch, click, success beep)

### Fixed
- **Shader blob crash**: Precompiled shader blobs embedded in `imgui_shaders.h` 
  with validated DXBC headers. No D3DCompiler dependency.  
- **BSOD (per-frame I/O)**: Removed all per-frame `CreateFile`/`WriteFile`/`CloseHandle` 
  calls from render loop. Logging restricted to init-only.
- **ImGui 1.90 vs 1.92 API mismatch**: Switched vendored ImGui to 1.90.7 to match the 
  framework's internal API usage
- **PNG-as-font crash**: `tab_icons.h` image data was being passed to `AddFontFromMemoryTTF()` 
  → corrupt font atlas → AV. Changed to use `inter_medium` TTF for icon slots.
- **Driver cleanup**: Fixed resource release ordering in `Overlay_Shutdown()` (ImGui 
  backends before D3D device release)
- **Thread safety**: `GetStatus()` shared buffer race condition with `SetStatus()`
- **Steam VDF parsing**: Narrower path extraction to avoid false matches
- **Font loading**: Eliminated redundant multi-font instances, reduced memory pressure
- **Sound WAV parse**: Added guard for truncated RIFF headers to prevent buffer over-reads
- **D3D11 pipeline**: Added HRESULT checks on swapchain creation and backbuffer acquisition
- **Project cleanup**: Removed ~47 duplicate file copies (70 MB), removed test scaffolding
- **Null guard**: Added null-checks for GUI textures and font objects before rendering
- **CMake fixes**: Unified CRT linking (`/MT` static only), removed duplicate linker flags

### Changed
- **Rebranded**: Window title → "Hamas Client", accent → green, sidebar → "HAMAS"/"CLIENT"
- **PVZGW2**: Extracted to separate project with own payload (see `../PVZGW2/`)
- **Configuration**: Per-game configs now in `semi.conf`/`legit.conf` instead of hardcoded
- **Webhook**: Moved endpoint URLs from hardcoded constants to centralized proxy function
- **GUI layout**: Tab icons use single-letter placeholdrs (A/V/M/C/S) with inter-medum font
- **Driver path**: Changed to build-specific `gw2_kmap.sys` (was shared `kdmapper.sys`)
- **Logging**: Payload log auto-cleared on fresh inject (avids stale multi-session data)
- **Build**: Standadized on Ninja + CMake across all modules (removed manual cl invocations)
- **Resources**: Hospitalized all assets in `.rc` file with proper resource IDs

### Removed
- **Standalone overlay**: Exteral `Unturned.png`/`hamaslogo.png` → now resources only
- **Test files**: `svchost.exe`, `payload_minimal.cpp`, build diagnostics
- **Unusd**: `vmware.hpp`, `VMProtectSDK.h/lib`, `antidbg.lib` from israeliclient
- **Duplicates**: 47 fiels including `(1)` copies, test builds, old embed assets

## [1.0.0] — Initial (israeliclient 2.0 fork)
- Unturned-focused injection loader
- VMProtect-protected binary
- Standalone Win32 loader GUI
- KeyAuth-based licensing
- kdmapper driver mapping