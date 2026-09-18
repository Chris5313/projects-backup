@echo off
setlocal enabledelayedexpansion

REM ---- Toolchain paths (edit if your machine differs) ----
set MSVC=C:\Program Files\Microsoft Visual Studio\18\Community\VC\Tools\MSVC\14.51.36231
set SDK=C:\Program Files (x86)\Windows Kits\10
set WDK=%~dp0..\tools\wdk
set WDKVER=10.0.26100.0

set SRCDIR=%~dp0
set OUTDIR=%SRCDIR%build_out

set CLEXE="%MSVC%\bin\HostX64\x64\cl.exe"
set LINKEXE="%MSVC%\bin\HostX64\x64\link.exe"

if not exist "%OUTDIR%" mkdir "%OUTDIR%"
cd /d "%SRCDIR%"

REM km headers from vendored WDK; shared/ucrt from SDK; CRT from MSVC
set INCLUDES=/I"%MSVC%\include" /I"%WDK%\Include\%WDKVER%\km" /I"%SDK%\Include\%WDKVER%\shared" /I"%SDK%\Include\%WDKVER%\ucrt"
set CFLAGS=/nologo /W3 /WX- /GS- /GR- /EHa- /kernel /O2 /Zp8 /FI ntifs.h /std:c++17 /c /Fo"%OUTDIR%\\"
set DEFS=/D_WIN64 /D_AMD64_ /DAMD64 /DNTDDI_VERSION=0x0A000009 /DWINVER=0x0A00 /D_WIN32_WINNT=0x0A00 /DNDEBUG /DWIN32=100 /DNTSTRSAFE_LIB

echo === Compiling driver sources ===
%CLEXE% %CFLAGS% %INCLUDES% %DEFS% main.cpp mapper.cpp process.cpp apc.cpp
if errorlevel 1 (
    echo COMPILE FAILED
    exit /b 1
)

set WDKLIBS="%WDK%\Lib\%WDKVER%\km\x64\ntoskrnl.lib" "%WDK%\Lib\%WDKVER%\km\x64\hal.lib" "%WDK%\Lib\%WDKVER%\km\x64\wdm.lib" "%WDK%\Lib\%WDKVER%\km\x64\BufferOverflowFastFailK.lib" "%WDK%\Lib\%WDKVER%\km\x64\ntstrsafe.lib"
set MSVCLIBS="%WDK%\Lib\%WDKVER%\km\x64\libcntpr.lib"

set OBJS="%OUTDIR%\main.obj" "%OUTDIR%\mapper.obj" "%OUTDIR%\process.obj" "%OUTDIR%\apc.obj"

echo === Linking driver ===
%LINKEXE% /nologo /nodefaultlib /SUBSYSTEM:NATIVE /DRIVER:WDM /ENTRY:CustomDriverEntry /MERGE:_TEXT=.text /BASE:0x10000 /SAFESEH:NO /PDB:"%OUTDIR%\gw2_kmap.pdb" /OUT:"%OUTDIR%\gw2_kmap.sys" %WDKLIBS% %MSVCLIBS% %OBJS%
if errorlevel 1 (
    echo LINK FAILED
    exit /b 1
)

echo.
echo === BUILD SUCCESS ===
dir "%OUTDIR%\gw2_kmap.sys"
echo Driver output: %OUTDIR%\gw2_kmap.sys
