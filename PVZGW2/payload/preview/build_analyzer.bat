@echo off
setlocal
cd /d "%~dp0"
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" x64 >nul 2>&1
cl /nologo /O2 /EHsc bmp_analyze.cpp /Fe:bmp_analyze.exe
if errorlevel 1 ( echo COMPILE FAILED & exit /b 1 )
echo === ANALYZER BUILT ===
