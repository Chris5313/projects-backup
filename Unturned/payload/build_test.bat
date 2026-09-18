@echo off
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" x64
cd /d "%~dp0"
cl /O2 /GS- /std:c++17 /MT /c /DIC_TEST_MODE /DNDEBUG payload.cpp
if errorlevel 1 (
    echo COMPILE FAILED
    exit /b 1
)
link /DLL /NODEFAULTLIB /ENTRY:DllMain /OUT:payload_new.dll kernel32.lib payload.obj /MANIFEST:NO
if errorlevel 1 (
    echo LINK FAILED
    exit /b 1
)
echo BUILD OK
copy /Y payload_new.dll "..\Test\test_files\payload.dll"
echo Copied to test_files
