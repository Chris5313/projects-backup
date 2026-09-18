@echo off
setlocal

cd /d "%~dp0"

set PATH=C:\cmake-3.28.3-windows-x86_64\bin;C:\ninja;%PATH%

call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" x64 >nul 2>&1

if exist build rmdir /s /q build
mkdir build
cd build

cmake .. -G "Ninja" -DCMAKE_BUILD_TYPE=Release
if errorlevel 1 ( echo CMAKE CONFIGURE FAILED & exit /b 1 )

cmake --build .
if errorlevel 1 ( echo BUILD FAILED & exit /b 1 )

copy /Y gw2_payload.dll ..\..\build\gw2_payload.dll >nul
echo.
echo === PAYLOAD BUILD SUCCESS ===
dir gw2_payload.dll
