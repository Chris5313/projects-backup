@echo off
setlocal enabledelayedexpansion

set MSVC=C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\14.44.35207
set WDK=C:\Program Files (x86)\Windows Kits\10
set WDKVER=10.0.26100.0
set SRCDIR=%~dp0
set OUTDIR=%SRCDIR%build_out

set CLEXE="%MSVC%\bin\HostX64\x64\cl.exe"
set LINKEXE="%MSVC%\bin\HostX64\x64\link.exe"

if not exist "%OUTDIR%" mkdir "%OUTDIR%"
cd /d "%SRCDIR%"

set INCLUDES=/I"%MSVC%\include" /I"%WDK%\Include\%WDKVER%\km" /I"%WDK%\Include\%WDKVER%\shared" /I"%WDK%\Include\%WDKVER%\ucrt"

set DEFS=/D_WIN64 /D_AMD64_ /DAMD64 /DNTDDI_VERSION=0x0A000009 /DWINVER=0x0A00 /D_WIN32_WINNT=0x0A00 /DNDEBUG /DWIN32=100 /DNTSTRSAFE_LIB

set CFLAGS=/nologo /W3 /WX- /GS- /GR- /EHa- /kernel /O2 /Zp8 /FI ntifs.h /std:c++17 /c /Fo"%OUTDIR%\\"

echo === Compiling sources ===
%CLEXE% %CFLAGS% %INCLUDES% %DEFS% main.cpp mapper.cpp process.cpp apc.cpp
if errorlevel 1 (
    echo COMPILE FAILED
    exit /b 1
)

set WDKLIBS="%WDK%\Lib\%WDKVER%\km\x64\ntoskrnl.lib" "%WDK%\Lib\%WDKVER%\km\x64\hal.lib" "%WDK%\Lib\%WDKVER%\km\x64\wdm.lib" "%WDK%\Lib\%WDKVER%\km\x64\BufferOverflowFastFailK.lib" "%WDK%\Lib\%WDKVER%\km\x64\ntstrsafe.lib"
set MSVCLIBS="%WDK%\Lib\%WDKVER%\km\x64\libcntpr.lib"

set OBJS="%OUTDIR%\main.obj" "%OUTDIR%\mapper.obj" "%OUTDIR%\process.obj" "%OUTDIR%\apc.obj"

echo === Linking ===
%LINKEXE% /nologo /nodefaultlib /SUBSYSTEM:NATIVE /DRIVER:WDM /ENTRY:CustomDriverEntry /MERGE:_TEXT=.text /BASE:0x10000 /SAFESEH:NO /PDB:"%OUTDIR%\kmap.pdb" /OUT:"%OUTDIR%\kmap.sys" %WDKLIBS% %MSVCLIBS% %OBJS%
if errorlevel 1 (
    echo LINK FAILED
    exit /b 1
)

echo.
echo === BUILD SUCCESS ===
dir "%OUTDIR%\kmap.sys"
copy /Y "%OUTDIR%\kmap.sys" "%SRCDIR%..\..\package\kmap.sys"
echo Copied to package.
