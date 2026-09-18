@echo off
call "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvarsall.bat" x64 >nul 2>&1

rem UCRT fix — vcvars picks SDK 10.0.28000.0 (bin-only, no headers installed).
rem Force the newest SDK that actually has headers: 10.0.26100.0.
set UCRTVersion=10.0.26100.0
set "INCLUDE=C:\Program Files (x86)\Windows Kits\10\Include\%UCRTVersion%\ucrt;C:\Program Files (x86)\Windows Kits\10\Include\%UCRTVersion%\um;C:\Program Files (x86)\Windows Kits\10\Include\%UCRTVersion%\shared;C:\Program Files (x86)\Windows Kits\10\Include\%UCRTVersion%\winrt;%INCLUDE%"
set "LIB=C:\Program Files (x86)\Windows Kits\10\Lib\%UCRTVersion%\ucrt\x64;C:\Program Files (x86)\Windows Kits\10\Lib\%UCRTVersion%\um\x64;%LIB%"

cd /d "C:\Dev\security\israeliclient2.0\19\thirdparty\imgui\examples\example_win32_directx11"

echo Compiling C++ files...
cl.exe /c ^
  /I"..\..\..\..\framework" /I"..\.." /I"..\..\backends" ^
  /I"C:\Dev\security\israeliclient2.0\19\..\vendor\minhook\include" ^
  /I"C:\Dev\security\israeliclient2.0\19\thirdparty\imgui" ^
  /I"C:\Dev\security\israeliclient2.0\19\thirdparty\dxsdk\Include" ^
  /I"C:\Dev\security\israeliclient2.0\19\thirdparty\freetype\include" ^
  /Zi /nologo /W4 /WX- /diagnostics:column /O2 /Oi /FS ^
  /D BUILD_DLL /D NDEBUG /D _WINDLL /D _MBCS ^
  /Gm- /EHsc /MT /GS- /Gy /fp:precise ^
  /Zc:wchar_t /Zc:forScope /Zc:inline /std:c++20 ^
  /Fo"C:\Dev\security\israeliclient2.0\19\thirdparty\imgui\examples\example_win32_directx11\Release\\" /Fd"C:\Dev\security\israeliclient2.0\19\thirdparty\imgui\examples\example_win32_directx11\Release\vc143.pdb" ^
  /external:W4 /Gd /TP /FC /utf-8 /EHa /Zc:threadSafeInit- ^
  "C:\Dev\security\israeliclient2.0\19\framework\data\imgui_freetype.cpp" "C:\Dev\security\israeliclient2.0\19\framework\data\texture_loader.cpp" "C:\Dev\security\israeliclient2.0\19\framework\functional\base_elements.cpp" "C:\Dev\security\israeliclient2.0\19\framework\functional\begin.cpp" "C:\Dev\security\israeliclient2.0\19\framework\functional\begin_child.cpp" "C:\Dev\security\israeliclient2.0\19\framework\functional\button.cpp" "C:\Dev\security\israeliclient2.0\19\framework\functional\checkbox.cpp" "C:\Dev\security\israeliclient2.0\19\framework\functional\color_picker.cpp" "C:\Dev\security\israeliclient2.0\19\framework\functional\config.cpp" "C:\Dev\security\israeliclient2.0\19\framework\functional\custom_draw.cpp" "C:\Dev\security\israeliclient2.0\19\framework\functional\drag_slider.cpp" "C:\Dev\security\israeliclient2.0\19\framework\functional\dropdown.cpp" "C:\Dev\security\israeliclient2.0\19\framework\functional\esp_preview.cpp" "C:\Dev\security\israeliclient2.0\19\framework\functional\keybind.cpp" "C:\Dev\security\israeliclient2.0\19\framework\functional\lua.cpp" "C:\Dev\security\israeliclient2.0\19\framework\functional\notifications.cpp" "C:\Dev\security\israeliclient2.0\19\framework\functional\selection.cpp" "C:\Dev\security\israeliclient2.0\19\framework\functional\textfield.cpp" "C:\Dev\security\israeliclient2.0\19\framework\functional\text_editor.cpp" "C:\Dev\security\israeliclient2.0\19\framework\gui.cc" "C:\Dev\security\israeliclient2.0\19\thirdparty\imgui\imgui.cpp" "C:\Dev\security\israeliclient2.0\19\thirdparty\imgui\imgui_draw.cpp" "C:\Dev\security\israeliclient2.0\19\thirdparty\imgui\imgui_tables.cpp" "C:\Dev\security\israeliclient2.0\19\thirdparty\imgui\imgui_widgets.cpp" "C:\Dev\security\israeliclient2.0\19\thirdparty\imgui\backends\imgui_impl_dx11.cpp" "C:\Dev\security\israeliclient2.0\19\thirdparty\imgui\backends\imgui_impl_win32.cpp" "C:\Dev\security\israeliclient2.0\19\thirdparty\imgui\examples\example_win32_directx11\main.cpp"
if errorlevel 1 (echo CPP_FAILED & exit /b 1)

echo Compiling C files...
cl.exe /c ^
  /I"..\..\..\..\framework" /I"..\.." /I"..\..\backends" ^
  /I"C:\Dev\security\israeliclient2.0\19\..\vendor\minhook\include" ^
  /I"C:\Dev\security\israeliclient2.0\19\thirdparty\imgui" ^
  /I"C:\Dev\security\israeliclient2.0\19\thirdparty\dxsdk\Include" ^
  /I"C:\Dev\security\israeliclient2.0\19\thirdparty\freetype\include" ^
  /Zi /nologo /W4 /WX- /diagnostics:column /O2 /Oi /FS ^
  /D BUILD_DLL /D NDEBUG /D _WINDLL /D _MBCS ^
  /Gm- /EHsc /MT /GS- /Gy /fp:precise ^
  /Zc:wchar_t /Zc:forScope /Zc:inline ^
  /Fo"C:\Dev\security\israeliclient2.0\19\thirdparty\imgui\examples\example_win32_directx11\Release\\" /Fd"C:\Dev\security\israeliclient2.0\19\thirdparty\imgui\examples\example_win32_directx11\Release\vc143.pdb" ^
  /external:W4 /Gd /TC /FC /utf-8 ^
  "C:\Dev\security\israeliclient2.0\vendor\minhook\src\hook.c" "C:\Dev\security\israeliclient2.0\vendor\minhook\src\buffer.c" "C:\Dev\security\israeliclient2.0\vendor\minhook\src\trampoline.c" "C:\Dev\security\israeliclient2.0\vendor\minhook\src\hde\hde64.c"
if errorlevel 1 (echo C_FAILED & exit /b 1)

echo Linking...
link.exe ^
  /OUT:"C:\Dev\security\israeliclient2.0\19\thirdparty\imgui\examples\example_win32_directx11\Release\example_win32_directx11.dll" ^
  /NOLOGO ^
  /LIBPATH:"C:\Dev\security\israeliclient2.0\19\thirdparty\dxsdk\Lib\x64" ^
  /LIBPATH:"C:\Dev\security\israeliclient2.0\19\thirdparty\freetype\win64" ^
  d3d11.lib dxgi.lib user32.lib libucrt.lib libvcruntime.lib ^
  kernel32.lib gdi32.lib winspool.lib comdlg32.lib advapi32.lib ^
  shell32.lib ole32.lib oleaut32.lib uuid.lib odbc32.lib odbccp32.lib ^
  freetype.lib ^
  /DLL ^
  /MANIFEST /MANIFESTUAC:"level='asInvoker' uiAccess='false'" /manifest:embed ^
  /DEBUG /PDB:"C:\Dev\security\israeliclient2.0\19\thirdparty\imgui\examples\example_win32_directx11\Release\example_win32_directx11.pdb" ^
  /SUBSYSTEM:CONSOLE /OPT:NOREF /OPT:ICF /ENTRY:RawDllMain ^
  /TLBID:1 /DYNAMICBASE /NXCOMPAT /MACHINE:X64 ^
  "C:\Dev\security\israeliclient2.0\19\thirdparty\imgui\examples\example_win32_directx11\Release\*.obj"
if errorlevel 1 (echo LINK_FAILED & exit /b 1)

echo BUILD_OK
