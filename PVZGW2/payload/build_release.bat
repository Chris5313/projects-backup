@echo off
setlocal

REM RELEASE build — shipping: F2 = light entity refresh (no disk dumps),
REM F3 disabled, per-lock logging compiled out. Output: build_release\gw2_payload.dll
REM Also copies to the deploy dir (..\..\build\gw2_payload.dll).

cd /d "%~dp0"

set PATH=C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin;C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja;%PATH%

call "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvarsall.bat" x64 >nul 2>&1

if exist build_release rmdir /s /q build_release
mkdir build_release
cd build_release

cmake .. -G "Ninja" -DCMAKE_BUILD_TYPE=Release -DGW2_RELEASE=ON
if errorlevel 1 ( echo CMAKE CONFIGURE FAILED & exit /b 1 )

cmake --build .
if errorlevel 1 ( echo BUILD FAILED & exit /b 1 )

copy /Y gw2_payload.dll ..\..\build\gw2_payload.dll >nul
if errorlevel 1 ( echo DEPLOY COPY FAILED & exit /b 1 )

echo.
echo === RELEASE BUILD SUCCESS ===
dir gw2_payload.dll
