@echo off
call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" x64 >nul 2>&1

set UCRTVersion=10.0.26100.0
set "INCLUDE=C:\Program Files (x86)\Windows Kits\10\Include\%UCRTVersion%\ucrt;C:\Program Files (x86)\Windows Kits\10\Include\%UCRTVersion%\um;C:\Program Files (x86)\Windows Kits\10\Include\%UCRTVersion%\shared;C:\Program Files (x86)\Windows Kits\10\Include\%UCRTVersion%\winrt;%INCLUDE%"
set "LIB=C:\Program Files (x86)\Windows Kits\10\Lib\%UCRTVersion%\ucrt\x64;C:\Program Files (x86)\Windows Kits\10\Lib\%UCRTVersion%\um\x64;%LIB%"

if not exist "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\imgui\examples\example_win32_directx11\Release" mkdir "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\imgui\examples\example_win32_directx11\Release"

echo [1/3] Compiling C++ files...
cl.exe /c ^
  /I"C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\framework" /I"C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\imgui" /I"C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\imgui\backends" ^
  /I"C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\vendor\minhook\include" ^
  /I"C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\dxsdk\Include" ^
  /I"C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\freetype\include" ^
  /Zi /nologo /W4 /WX- /diagnostics:column /O2 /Oi /FS ^
  /D BUILD_DLL /D NDEBUG /D _WINDLL /D _MBCS ^
  /Gm- /EHsc /MT /GS- /Gy /fp:precise ^
  /Zc:wchar_t /Zc:forScope /Zc:inline /std:c++20 ^
  /Fo"C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\imgui\examples\example_win32_directx11\Release\\" /Fd"C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\imgui\examples\example_win32_directx11\Release\vc143.pdb" ^
  /external:W4 /Gd /TP /FC /utf-8 /EHa /Zc:threadSafeInit- ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\framework\data\imgui_freetype.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\framework\data\texture_loader.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\framework\functional\base_elements.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\framework\functional\begin.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\framework\functional\begin_child.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\framework\functional\button.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\framework\functional\checkbox.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\framework\functional\color_picker.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\framework\functional\config.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\framework\functional\custom_draw.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\framework\functional\drag_slider.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\framework\functional\dropdown.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\framework\functional\esp_preview.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\framework\functional\keybind.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\framework\functional\lua.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\framework\functional\notifications.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\framework\functional\selection.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\framework\functional\text_editor.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\framework\functional\textfield.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\imgui\imgui.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\imgui\imgui_draw.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\imgui\imgui_tables.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\imgui\imgui_widgets.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\imgui\backends\imgui_impl_dx11.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\imgui\backends\imgui_impl_win32.cpp" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\imgui\examples\example_win32_directx11\main.cpp"
if errorlevel 1 (echo CPP_FAILED & exit /b 1)

echo [2/3] Compiling C files (MinHook)...
cl.exe /c ^
  /I"C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\vendor\minhook\include" ^
  /I"C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\imgui" ^
  /Zi /nologo /W4 /WX- /O2 /Oi /FS ^
  /D BUILD_DLL /D NDEBUG /D _WINDLL /D _MBCS ^
  /Gm- /MT /GS- /Gy /fp:precise ^
  /Zc:wchar_t /Zc:forScope /Zc:inline ^
  /Fo"C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\imgui\examples\example_win32_directx11\Release\\" /Fd"C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\imgui\examples\example_win32_directx11\Release\vc143.pdb" ^
  /external:W4 /Gd /TC /FC /utf-8 ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\vendor\minhook\src\hook.c" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\vendor\minhook\src\buffer.c" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\vendor\minhook\src\trampoline.c" ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\vendor\minhook\src\hde\hde64.c"
if errorlevel 1 (echo C_FAILED & exit /b 1)

echo [3/3] Linking...
link.exe ^
  /OUT:"C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\imgui\examples\example_win32_directx11\Release\israeliclient2.dll" ^
  /NOLOGO ^
  /LIBPATH:"C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\dxsdk\Lib\x64" ^
  /LIBPATH:"C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\freetype\win64" ^
  d3d11.lib dxgi.lib user32.lib libucrt.lib libvcruntime.lib ^
  kernel32.lib gdi32.lib winspool.lib comdlg32.lib advapi32.lib ^
  shell32.lib ole32.lib oleaut32.lib uuid.lib odbc32.lib odbccp32.lib ^
  freetype.lib winmm.lib ^
  /DLL ^
  /MANIFEST:NO ^
  /DEBUG:NONE ^
  /SUBSYSTEM:CONSOLE /OPT:NOREF /OPT:ICF /ENTRY:RawDllMain ^
  /DYNAMICBASE /NXCOMPAT /MACHINE:X64 ^
  "C:\Users\Chirs\Documents\IC2\Main\israeliclient 2.0\19\thirdparty\imgui\examples\example_win32_directx11\Release\*.obj"
if errorlevel 1 (echo LINK_FAILED & exit /b 1)

echo BUILD_OK
