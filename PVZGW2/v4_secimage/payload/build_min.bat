@echo off
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" x64 >nul 2>&1
cd /d "C:\Users\Shadow\Documents\IC2\IC2\PVZGW2\payload"
cl /nologo /O1 /LD /GS- payload_minimal.cpp /link /SUBSYSTEM:WINDOWS /OUT:build\gw2_payload_min.dll
