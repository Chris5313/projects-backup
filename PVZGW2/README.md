# GW2 Internal

Internal ImGui overlay for Plants vs. Zombies: Garden Warfare 2.

Kernel driver manual-maps a payload DLL into `GW2.Main_Win64_Retail.exe`, the
payload hooks `IDXGISwapChain::Present` and renders a 5-tab menu (Combat /
Visuals / Misc / Config / Settings) with FontAwesome 6 icons, toggled with **F1**.

## Structure

| Path | What it is |
|---|---|
| `payload/` | The DLL that gets mapped. DXGI Present hook + ImGui (DX11) overlay |
| `payload/framework/` | Widget framework (tabs, checkboxes, sliders, color pickers, keybinds) |
| `payload/preview/` | Offscreen renderer — screenshots the menu without launching the game |
| `driver/` | Kernel-mode mapper: reads the DLL, maps it RW, flips PTEs to RX, calls DllMain via thread hijack |
| `vendor/` | Dear ImGui + MinHook |
| `arch.md` | Architecture notes |

## Build

**Payload** (VS2022 Build Tools + CMake + Ninja):
```
cd payload
build.bat
```
Produces `payload/build/gw2_payload.dll`.

**Menu preview** (renders the menu to a BMP, no game needed):
```
cd payload\preview
build.bat
build\menu_preview.exe menu.bmp 240        # default tab
build\menu_preview.exe menu.bmp 240 14 4   # jump to tab index 4 (Settings)
```

**Driver** (WDK, admin prompt):
```
cd driver
compile.bat
```
Produces `driver/build_out/gw2_kmap.sys`.

## Notes

- The payload is manually mapped (not `SEC_IMAGE`), so `DllMain` registers its
  own exception table via `RtlAddFunctionTable` — without it every
  `__try/__except` in the image is dead code.
- Present is captured by patching the swapchain class vtable (slot 8) from a
  dummy D3D11 device before the game's renderer init.
- The ImGui Win32 backend is initialized with the HWND taken from the live
  game swapchain's `GetDesc()` — never from the dummy window.
- Icon font: FontAwesome 6 Free Solid embedded in `framework/tab_icons.h`
  (source TTF: `framework/fa-solid-900.ttf`). Missing glyphs render as the
  last glyph in the font, so always use real FA codepoints (U+F000–U+F8FF).
