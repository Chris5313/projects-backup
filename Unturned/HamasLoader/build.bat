@echo off
setlocal

REM Set up MSVC x64 environment
call "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvarsall.bat" x64

REM Add cmake and ninja to PATH (VS-bundled)
set "PATH=C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin;C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja;%PATH%"

cd /d "%~dp0"
if exist build rmdir /s /q build
mkdir build
cd build

cmake .. -G "Ninja" -DCMAKE_BUILD_TYPE=Release -DCMAKE_C_COMPILER=cl -DCMAKE_CXX_COMPILER=cl
if %errorlevel% neq 0 (
    echo CONFIGURE FAILED
    exit /b 1
)

cmake --build . --config Release
if %errorlevel% neq 0 (
    echo BUILD FAILED
    exit /b 1
)

REM VMProtect: produce the distributable. The raw linker output imports
REM VMProtectSDK64.dll (dllimport-only SDK, no static stub exists) and CANNOT
REM run without it - never ship build\HamasClient.exe to users.
set "VMPCON=C:\Users\Shadow\Documents\Projects\Vmprotect\IC2\VMProtect\VMProtect\VMProtect_Ultimate_v3.8.4_Build_1754\VMProtect_Con.exe"
if not exist "%VMPCON%" (
    echo WARNING: VMProtect_Con.exe not found - no protected build produced. DO NOT distribute HamasClient.exe
    exit /b 0
)
echo.
echo Running VMProtect...
"%VMPCON%" "HamasClient.exe" "HamasClient_protected.exe"
if %errorlevel% neq 0 (
    echo VMPROTECT FAILED - no protected build. DO NOT distribute HamasClient.exe
    exit /b 1
)
echo.
echo BUILD SUCCESS
echo DISTRIBUTE THIS FILE: %cd%\HamasClient_protected.exe
