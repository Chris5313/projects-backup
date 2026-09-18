@echo off
title Build payload.dll
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" ^
    "%~dp0payload.vcxproj" ^
    /p:Configuration=Release /p:Platform=x64 ^
    /t:Build /v:minimal
if errorlevel 1 (
    echo [!] Build failed
    pause
    exit /b 1
)
echo [+] Built: Release\payload.dll
pause
