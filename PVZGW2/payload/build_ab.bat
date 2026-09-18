@echo off
setlocal
cd /d "%~dp0"
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" x64 >nul 2>&1
if not exist ab_build mkdir ab_build
cl /nologo /LD /GS- /O1 /Fo:ab_build\ payload_ab.cpp /link /NODEFAULTLIB /ENTRY:DllMain /OUT:payload_ab.dll
if errorlevel 1 ( echo BUILD FAILED & exit /b 1 )
dir payload_ab.dll
echo.
echo === AB PAYLOAD BUILD SUCCESS ===
echo Test: copy /Y payload_ab.dll C:\Users\Public\gw2_payload.dll
