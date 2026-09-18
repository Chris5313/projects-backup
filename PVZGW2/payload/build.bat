@echo off
setlocal

REM DEBUG build — full diagnostics: F2 = 5GB heap dump + scan files,
REM F3 = dump pair, per-lock logging. Output: build\gw2_payload_debug.dll
REM Shipping build: run build_release.bat

cd /d "%~dp0"

set PATH=C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin;C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja;%PATH%

call "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvarsall.bat" x64 >nul 2>&1

if exist build rmdir /s /q build
mkdir build
cd build

cmake .. -G "Ninja" -DCMAKE_BUILD_TYPE=Release
if errorlevel 1 ( echo CMAKE CONFIGURE FAILED & exit /b 1 )

cmake --build .
if errorlevel 1 ( echo BUILD FAILED & exit /b 1 )

echo.
echo === DEBUG BUILD SUCCESS ===
dir gw2_payload_debug.dll
