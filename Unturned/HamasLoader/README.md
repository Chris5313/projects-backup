# HamasClient — Multi-Game Cheat Loader

Kernel-mode game cheating framework with custom ImGui UI for PvZ: Garden   
Warfare 2 (primary target) with Unturned and Roblox support planned.

## ⚠️ Disclaimer

This project is for **educational purposes only**. Using this software in violation of game ToS, EULAs, or applicable laws is not endorsed. The authors assume no liability for misuse, bans, or legal consequences.

## 🏗️ Architecture

```
HamasClient.exe (user-mode loader)
├── D3D11 / ImGui GUI (src/main.cpp, src/gui.cpp)
├── Discord webhook logging (src/discord_log.cpp)
├── Game detection via Steam registry/VDF (src/game.cpp)
└── Driver injection pipeline (src/inject.cpp)
    ├── kdmapper (userspace driver mapper)
    └── gw2_kmap.sys (kernel driver → manual-maps gw2_payload.dll)

gw2_payload.dll (injected payload for PvZ GW2)
├── Present hook → D3D11 overlay (payload/main.cpp)
├── ImGui framework ported from israeliclient 2.0 (payload/framework/)
└── Custom widget system with tabbed UI (payload/framework/gui_gw2.cc)
```

## 📁 Project Structure

```
HamasClient/
├── src/                    # Loader source code
│   ├── main.cpp           # WinMain, D3D11 setup, main loop
│   ├── gui.cpp/h          # ImGui GUI: login, dashboard, tabs
│   ├── inject.cpp/h       # Driver injection pipeline
│   ├── game.cpp/h         # Steam game detection & launch
│   ├── download.cpp/h     # Embedded resource extraction
│   ├── discord_log.cpp/h  # Webhook event logging
│   ├── style.cpp/h        # ImGui theming
│   ├── sound.cpp/h        # WAV/music playback
│   └── security.cpp/h     # Anti-VM / security
├── embed/                  # Embedded resources (packed into EXE)
│   ├── gw2_payload.dll    # Injected DLL for PvZ GW2
│   ├── gw2_kmap.sys       # Kernel driver
│   ├── kdmapper.exe       # Driver mapper
│   └── *.png/*.wav        # UI assets
├── assets/                 # Loose runtime assets
│   ├── sounds/            # UI sound effects (.wav)
│   └── *.png/*.ttf        # Flags, fonts, textures
├── vendor/imgui/           # ImGui 1.90.7 (with precompiled shaders)
└── build.bat              # MSVC + Ninja build script
```

## 🔧 Building

### Prerequisites
- Visial Studio 2022 Build Tools (MSVC 19.x)
- CMake 3.20+
- Ninja build system
- Windows 10/11 SDK

### Build
```bat
cd HamasClient
build.bat
```

Output: `build/HamasClient.exe` (~110 MB with embedded resources)

## � Features

### PvZ GW2
- DX11 overlay with Present hook
- Aimbot (FOV, smothing, visibility check)
- Triggerbot (delayed fire)
- ESP (box, name, health bar, distance)
- Chams (visible/invisible color)
- Misc (no spread, no recil, rapid fire)

### UI Framework
- Custom dark-themed ImGui (ported from israeliclient 2.0)
- 5-tab layout: Combat, Visuals, Misc, Config, Settings
- Accent color system (Hamas green #00A651)
- Per-fame dark overlay (no file I/O per frame)

### Loader
- Hardware ID binding
- Discord webhook logging (login, inject, game events)
- Steam library auto-detection
- One-click inject (deploy payload → load driver → inject → monitor)

## 🔍 Key Technical Details

### Injection Pipeline
1. Extract gw2_payload.dll + gw2_kmap.sys from embedded resoures
2. Deploy DLL to `%ProgramData%\Microsoft\DeviceSync\`
3. Load kernel driver via kdmapper (vulnerable driver exploit)
4. Driver polls for target process → manual-maps payload via thread hijaking
5. Payload hooks DX11 Present → renders overlay

### Manual-mapping (Drier)
- Pagefile-backed setion (avoids MEM_PRIVATE VAD flaging)
- Thread hijaking via KTRAP_FRAME::Rip modification
- Imports resolved manually from PEB module list
- PE header wiped after ma (0x1000 bytes zeroed)
- Shellcode writes markr byte → driver verifies DllMain executed

### Sader Blobs
- Precompiled VS_4_0 + PS_4_0 from standard Imgui HLSL source
- Embedded in `imgui_shaders.h` — no D3DCompiler dependency
- 876 bytes VS + 660 bytes PS

## 📝 License

MIT — AhmedMmd (2024). See [LICENSE](./LICENSE) for full text.

## 🔗 Related
- [ImGui](https://github.com/ocornut/imgui) — v1.90.7
- [MinHook](https://github.com/TsudaKageyu/minhook) — API hoking
- [kdmapper](https://github.com/TheCruZ/kdmapper) — driver mapping