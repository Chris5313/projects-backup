@echo off
setlocal

REM GW2Dumper build — single-file MSVC console exe (no cmake needed).
REM Output: build\GW2Dumper.exe

cd /d "%~dp0"

call "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvarsall.bat" x64 >nul 2>&1
if errorlevel 1 ( echo VCVARS FAILED & exit /b 1 )

if not exist build mkdir build
pushd build

cl /nologo /O2 /MT /W3 ..\gw2_dumper.cpp /FeGW2Dumper.exe /link psapi.lib
if errorlevel 1 ( popd & echo BUILD FAILED & exit /b 1 )

popd

echo.
echo === GW2Dumper BUILD SUCCESS ===
dir build\GW2Dumper.exe
