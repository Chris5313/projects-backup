@echo off
setlocal
call "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvarsall.bat" x64 >nul 2>&1
set "CMAKE=C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"
set "NINJA=C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja\ninja.exe"
set "SRC=C:\Dev\security\israeliclient2.0"

rem Force UCRT + MSVC includes into INCLUDE if missing
if not defined UCRTVersion set UCRTVersion=10.0.26100.0
set "INCLUDE=C:\Program Files (x86)\Windows Kits\10\Include\%UCRTVersion%\ucrt;%INCLUDE%"

cd /d "%SRC%"
rd /s /q build 2>nul
mkdir build
cd build
"%CMAKE%" "%SRC%" -G "Ninja" -DCMAKE_BUILD_TYPE=Release -DCMAKE_C_COMPILER=cl -DCMAKE_CXX_COMPILER=cl -DCMAKE_MAKE_PROGRAM="%NINJA%"
if errorlevel 1 (echo CONFIG FAILED & exit /b 1)
"%NINJA%"
if errorlevel 1 (echo BUILD FAILED & exit /b 1)
echo BUILD OK
copy /Y israeliclient2.dll "%SRC%\test_package\payload.dll" >nul 2>&1
copy /Y israeliclient2.dll "%SRC%\test_package\israeliclient.dll" >nul 2>&1
echo Copied to test_package (payload.dll + israeliclient.dll)
rem ── Sync assets ──
if not exist "%SRC%\test_package\assets" mkdir "%SRC%\test_package\assets"
xcopy /Y /E /Q "%SRC%\assets\*" "%SRC%\test_package\assets\" >nul 2>&1
echo Assets synced to test_package
