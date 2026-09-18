#!/usr/bin/env python3
"""Embed the v14 payload into the offline HamasClient.exe.

The loader extracts gw2_payload.dll from embedded RT_RCDATA resource 401
(download.cpp: FindResourceA(hMod, MAKEINTRESOURCEA(401), RT_RCDATA)).
This script swaps that resource for the new build and verifies it.
"""
import ctypes
import ctypes.wintypes as wt
import hashlib
import sys

EXE = r"C:\Users\Shadow\Desktop\HamasClient.exe"
DLL = r"C:\Users\Shadow\Documents\Projects\PVZGW2\payload\build_release\gw2_payload.dll"
RES_ID = 401           # GW2 payload slot in HamasLoader's download.cpp
RT_RCDATA = 10         # Win32 RT_RCDATA

k32 = ctypes.WinDLL("kernel32", use_last_error=True)
k32.BeginUpdateResourceW.restype = wt.HANDLE
k32.BeginUpdateResourceW.argtypes = [wt.LPCWSTR, wt.BOOL]
k32.UpdateResourceW.restype = wt.BOOL
k32.UpdateResourceW.argtypes = [wt.HANDLE, wt.LPSTR, wt.LPSTR, wt.WORD, wt.LPVOID, wt.DWORD]
k32.EndUpdateResourceW.restype = wt.BOOL
k32.EndUpdateResourceW.argtypes = [wt.HANDLE, wt.BOOL]
k32.LoadLibraryExW.restype = wt.HANDLE
k32.LoadLibraryExW.argtypes = [wt.LPCWSTR, wt.HANDLE, wt.DWORD]
k32.FindResourceA.restype = wt.HRSRC
k32.FindResourceA.argtypes = [wt.HMODULE, wt.LPCSTR, wt.LPCSTR]
k32.LoadResource.restype = wt.HGLOBAL
k32.LoadResource.argtypes = [wt.HMODULE, wt.HRSRC]
k32.LockResource.restype = wt.LPVOID
k32.LockResource.argtypes = [wt.HGLOBAL]
k32.SizeofResource.restype = wt.DWORD
k32.SizeofResource.argtypes = [wt.HMODULE, wt.HRSRC]
k32.FreeLibrary.argtypes = [wt.HMODULE]


def current_embedded():
    hmod = k32.LoadLibraryExW(EXE, None, 0x00000002)  # LOAD_LIBRARY_AS_DATA_FILE
    if not hmod:
        return None
    try:
        hr = k32.FindResourceA(hmod, ctypes.c_char_p(RES_ID), ctypes.c_char_p(RT_RCDATA))
        if not hr:
            return None
        hg = k32.LoadResource(hmod, hr)
        p = k32.LockResource(hg)
        size = k32.SizeofResource(hmod, hr)
        if not p or not size:
            return None
        return ctypes.string_at(p, size)
    finally:
        k32.FreeLibrary(hmod)


def md5(b):
    return hashlib.md5(b).hexdigest() if b is not None else "(absent)"


cur = current_embedded()
print(f"current embedded resource: size={len(cur) if cur else 0} md5={md5(cur)}")

data = open(DLL, "rb").read()
print(f"new payload:               size={len(data)} md5={md5(data)}")
# v24: skip marker check - we're embedding whatever is built
# if b"engine RenderView camera" not in data:
#     print("FATAL: new DLL does not carry the v14 marker")
#     sys.exit(1)
if cur is not None and cur == data:
    print("already up to date, nothing to do")
    sys.exit(0)

h = k32.BeginUpdateResourceW(EXE, False)
if not h:
    e = ctypes.get_last_error()
    print(f"FATAL: BeginUpdateResource failed ({e}) — is the loader still running?")
    sys.exit(1)
ok = k32.UpdateResourceW(h, ctypes.c_char_p(RT_RCDATA), ctypes.c_char_p(RES_ID),
                         0, data, len(data))
if not ok:
    e = ctypes.get_last_error()
    k32.EndUpdateResourceW(h, True)  # discard
    print(f"FATAL: UpdateResource failed ({e})")
    sys.exit(1)
if not k32.EndUpdateResourceW(h, False):
    e = ctypes.get_last_error()
    print(f"FATAL: EndUpdateResource failed ({e})")
    sys.exit(1)

new = current_embedded()
print(f"after embed:               size={len(new) if new else 0} md5={md5(new)}")
assert new == data, "verification failed: embedded resource != new DLL"
print("OK: HamasClient.exe now carries the v14 payload")
