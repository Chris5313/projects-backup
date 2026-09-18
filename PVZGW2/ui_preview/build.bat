@echo off
setlocal

:: Setup VS2022 environment
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvars64.bat" >nul 2>&1

:: Build with ninja
if exist "build" rd /s /q build
mkdir build
cd build

cmake -G Ninja -DCMAKE_BUILD_TYPE=Release ..
if errorlevel 1 (
    echo CMake failed!
    pause
    exit /b 1
)

ninja
if errorlevel 1 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Build successful! Run: build\ui_preview.exe
echo.
