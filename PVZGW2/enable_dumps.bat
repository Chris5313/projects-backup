@echo off
net session >nul 2>&1
if %errorLevel% NEQ 0 (
    echo ERROR: Run this as Administrator! Right-click ^> Run as administrator
    pause
    exit /b 1
)

echo === Enabling Kernel Crash Dumps ===

REM Create Minidump folder if missing
if not exist "C:\Windows\Minidump" mkdir "C:\Windows\Minidump"

REM Set to Small Memory Dump (256KB, always fits in pagefile)
reg add "HKLM\SYSTEM\CurrentControlSet\Control\CrashControl" /v CrashDumpEnabled /t REG_DWORD /d 3 /f
reg add "HKLM\SYSTEM\CurrentControlSet\Control\CrashControl" /v AutoReboot /t REG_DWORD /d 0 /f
reg add "HKLM\SYSTEM\CurrentControlSet\Control\CrashControl" /v MinidumpDir /t REG_EXPAND_SZ /d "%%SystemRoot%%\Minidump" /f
reg add "HKLM\SYSTEM\CurrentControlSet\Control\CrashControl" /v AlwaysKeepMemoryDump /t REG_DWORD /d 1 /f
reg add "HKLM\SYSTEM\CurrentControlSet\Control\CrashControl" /v Overwrite /t REG_DWORD /d 0 /f

echo.
echo === Done ===
echo - Crash dumps enabled (Automatic Memory Dump)
echo - Auto-reboot DISABLED (screen will stay on BSOD so you can read the stop code)
echo - Minidump folder created at C:\Windows\Minidump
echo.
echo NEXT TIME IT BSODS: Write down the stop code shown on screen (e.g. KMODE_EXCEPTION_NOT_HANDLED)
echo and any hex value after it (e.g. 0x0000001E). Then reboot and send me:
echo   1. The stop code text
echo   2. C:\Users\Public\kmap_status.txt
echo   3. C:\Windows\Minidump\*.dmp (if any)
echo.
pause
