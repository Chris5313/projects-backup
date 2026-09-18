@echo off
setlocal EnableDelayedExpansion
title GW2 v13 build + deploy

set SRC=%~dp0payload\build_release\gw2_payload.dll
set EMBED=C:\Users\Shadow\Documents\Projects\Unturned\HamasLoader\embed\gw2_payload.dll
set VPS_PY=C:\Users\Shadow\Documents\Projects\.cred\vps.py
set REMOTE=/opt/hamas-log-proxy/files/gw2_payload.dll

echo === BUILD ===
call "%~dp0payload\build_release.bat"
if errorlevel 1 (
    echo [!] BUILD FAILED
    pause
    exit /b 1
)
if not exist "%SRC%" (
    echo [!] No DLL after build?
    pause
    exit /b 1
)

echo === VERIFY v13 MARKER ===
python -c "d=open(r'%SRC%','rb').read(); ok=b'render-thread Unmap passthrough' in d and b'zoom slot adopted' in d; print('v13 marker:', ok); exit(0 if ok else 1)"
if errorlevel 1 (
    echo [!] DLL missing v13 marker - wrong or stale build
    pause
    exit /b 1
)

for /f %%M in ('powershell -NoProfile -c "(Get-FileHash -Algorithm MD5 '%SRC%').Hash.ToLower()"') do set LOCAL_MD5=%%M
echo local  md5 %LOCAL_MD5%

echo === EMBED COPY ===
copy /y "%SRC%" "%EMBED%" >nul
for /f %%M in ('powershell -NoProfile -c "(Get-FileHash -Algorithm MD5 '%EMBED%').Hash.ToLower()"') do set EMBED_MD5=%%M
echo embed  md5 %EMBED_MD5%
if not "%LOCAL_MD5%"=="%EMBED_MD5%" (
    echo [!] EMBED MISMATCH
    pause
    exit /b 1
)

echo === VPS UPLOAD ===
python "%VPS_PY%" --put "%SRC%" "%REMOTE%"
if errorlevel 1 (
    echo [!] VPS UPLOAD FAILED
    pause
    exit /b 1
)
python "%VPS_PY%" "md5sum %REMOTE%"
echo.
echo Expected: %LOCAL_MD5%
echo.
echo DONE. v13 is on the VPS and in the loader embed.
pause
