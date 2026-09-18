# Israeli Client 2.0

## CRITICAL: How to Build the Payload DLL

The DLL is injected by a kernel driver that does MANUAL PE MAPPING. The CRT is NOT initialized by the loader. The DLL uses `RawDllMain` as a direct entry point (MSVC pre-CRT hook). **You MUST follow these exact steps or the DLL will silently crash on injection.**

### Build Steps

1. **Edit the vcxproj** at `19/thirdparty/imgui/examples/example_win32_directx11/example_win32_directx11.vcxproj`

   Change ONLY these 3 things in the `Release|x64` configuration:

   ```xml
   <!-- Line ~47: Change Application to DynamicLibrary -->
   <ConfigurationType>DynamicLibrary</ConfigurationType>

   <!-- Line ~49: Change true to false (LTCG breaks /OPT:NOREF) -->
   <WholeProgramOptimization>false</WholeProgramOptimization>

   <!-- Line ~145: Add BUILD_DLL -->
   <PreprocessorDefinitions>BUILD_DLL;%(PreprocessorDefinitions)</PreprocessorDefinitions>
   ```

   And add this inside the `<Link>` block for Release|x64 (after `<SubSystem>Console</SubSystem>`):
   ```xml
   <EntryPointSymbol>RawDllMain</EntryPointSymbol>
   ```

2. **Build via MSBuild** (or open framework.sln in VS and build Release|x64):
   ```
   call "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvarsall.bat" x64
   msbuild "19\framework.sln" /p:Configuration=Release /p:Platform=x64 /t:Build /m:1 /v:quiet
   ```

3. **Deploy** the output DLL:
   ```
   copy "19\thirdparty\imgui\examples\example_win32_directx11\Release\example_win32_directx11.dll" "C:\Dev\security\debug_test\payload.dll"
   ```

4. **REVERT the vcxproj** before committing (the repo keeps it as Application):
   ```
   git checkout HEAD -- 19/thirdparty/imgui/examples/example_win32_directx11/example_win32_directx11.vcxproj
   ```

### WHY These Settings Matter

| Setting | Value | Why |
|---------|-------|-----|
| `ConfigurationType` | `DynamicLibrary` | Produces .dll not .exe |
| `WholeProgramOptimization` | `false` | LTCG conflicts with `/OPT:NOREF`; changes CRT init layout |
| `BUILD_DLL` define | Required | `main.cpp` uses `#ifdef BUILD_DLL` for DLL entry vs exe entry |
| `/ENTRY:RawDllMain` | **CRITICAL** | The kernel driver does manual PE mapping without CRT setup. `_DllMainCRTStartup` (default) will crash. `RawDllMain` bypasses CRT init entirely. The DLL has custom heap allocators (HeapAlloc) replacing malloc/new so CRT is not needed. |
| `/OPT:NOREF` | Already in vcxproj | Keeps ALL functions linked (driver needs them; don't strip unreferenced code) |
| `/MT` | Already in vcxproj | Static CRT link. No vcruntime140.dll dependency |
| `/GS-` | Already in vcxproj | No buffer security cookies (CRT not available) |
| `/Zc:threadSafeInit-` | Already in vcxproj | No CRT thread-safe static init (CRT not available) |

### Build Output
- Output: `19/thirdparty/imgui/examples/example_win32_directx11/Release/example_win32_directx11.dll`
- Expected size: ~3-5 MB
- Copy to: `C:\Dev\security\debug_test\payload.dll`

### Testing
1. Run `C:\Dev\security\debug_test\IsraeliClient.exe` as Administrator
2. It loads `payload.dll` via kernel driver into Unturned
3. Press F1 in-game to toggle the menu
4. Debug log: `C:\Users\Public\payload_debug.txt`
5. Inject log: `C:\Users\Public\ic_inject_debug.txt`

### If the DLL doesn't show UI after injection:
- Check `C:\Users\Public\payload_debug.txt` - if EMPTY, the entry point is wrong (missing `/ENTRY:RawDllMain`)
- If it shows "dll_thread START" + "HOOKS DONE" but no menu, the Present hook works but rendering crashes
- If the loader says "IPC: SUCCESS!" but nothing happens, the DLL loaded but the thread crashed

---

## Project Structure
```
israeliclient2.0/
  19/                             # UI framework (version 19 fork)
    framework/
      gui.cc                      # Main menu render - 7 tabs, all features
      functional/                 # Custom ImGui widgets
        config.cpp                # Config list selectable + trash delete
        checkbox.cpp, button.cpp, dropdown.cpp, etc.
      settings/
        functions.h               # Framework classes + ui_sound (BUILD_DLL only)
        variables.h               # All feature variables (c_variable)
        settings.h                # Colors + metrics
        config.h                  # Config save/load/autoload system (BUILD_DLL only)
      data/
        font.h, texture.h        # Embedded fonts + textures (large byte arrays)
      shader/
        blur.hpp, pshader.hpp     # Background blur shaders
    thirdparty/
      imgui/                      # Dear ImGui + backends + example project
        imconfig.h                # BUILD_DLL: disables IM_ASSERT (abort hangs in injected DLL)
        examples/example_win32_directx11/
          main.cpp                # DLL entry (BUILD_DLL) or standalone exe test
          example_win32_directx11.vcxproj  # VS project (committed as Application, patch to DLL for build)
          Release/                # Build output
      dxsdk/                      # DirectX SDK
      freetype/                   # FreeType library
    framework.sln                 # Visual Studio solution
  vendor/
    minhook/                      # MinHook for vtable hooking
  assets/                         # Runtime assets (brand icon, sounds)
```

## Injection Chain
```
IsraeliClient.exe (loader)
  -> Creates shared memory section with payload.dll bytes
  -> Writes section name + size to registry
  -> Launches kdmapper.exe with kmap.sys
     -> Kernel driver reads registry, opens section
     -> Manual PE maps payload.dll into Unturned.exe
     -> Calls RawDllMain (entry point)
        -> Writes marker byte 0x42 at base+0x100
        -> Spawns dll_thread
           -> Creates dummy D3D11 device for vtable
           -> Hooks Present + ResizeBuffers via MinHook
           -> ImGui init on first hooked Present
           -> gui->render() draws the menu
```

## Key Files for Code Changes
- **Add features**: `19/framework/gui.cc` (menu tabs) + `19/framework/settings/variables.h` (state)
- **Widget styling**: `19/framework/settings/settings.h` (colors/metrics)
- **Config system**: `19/framework/functional/config.cpp` + `19/framework/settings/config.h`
- **Sounds**: `19/framework/settings/functions.h` (ui_sound namespace, BUILD_DLL only)

## Menu Tabs (hex selector in gui.cc)
0=Combat, 1=Visuals, 2=Players, 3=Misc, 4=Automation, 5=Config, 6=Settings

## Widget API
```cpp
widget->checkbox("Label", &bool_var);
widget->checkbox_with_key("Label", &bool, &key, &holding, &value, &show_binds);
widget->checkbox_with_color("Label", &bool, color[4], has_alpha);
widget->slider_int("Label", &int_var, min, max, step, "%d");
widget->slider_float("Label", &float_var, min, max, step, "%.1f");
widget->dropdown("Label", &selection, string_vector, count);
widget->button("Label", ImVec2(w, h));
widget->separator();
gui->begin_child("name"); /* widgets */ gui->end_child();
gui->begin_group(); /* left col */ gui->end_group(); gui->sameline(); gui->begin_group(); /* right col */ gui->end_group();
SCALE(value)  // DPI-aware scaling
```
