#!/usr/bin/env python3
"""Enumerate RT_RCDATA resources in the desktop HamasClient.exe (name, size, md5 head)."""
import ctypes as c
import hashlib

k32 = c.WinDLL("kernel32", use_last_error=True)
k32.LoadLibraryExW.restype = c.c_void_p
k32.LoadLibraryExW.argtypes = [c.c_wchar_p, c.c_void_p, c.c_uint32]
k32.EnumResourceNamesW.argtypes = [c.c_void_p, c.c_void_p, c.c_void_p, c.c_long]
k32.FindResourceW.restype = c.c_void_p
k32.FindResourceW.argtypes = [c.c_void_p, c.c_wchar_p, c.c_wchar_p]
k32.LoadResource.restype = c.c_void_p
k32.LoadResource.argtypes = [c.c_void_p, c.c_void_p]
k32.LockResource.restype = c.c_void_p
k32.LockResource.argtypes = [c.c_void_p]
k32.SizeofResource.restype = c.c_uint32
k32.SizeofResource.argtypes = [c.c_void_p, c.c_void_p]
k32.FreeLibrary.argtypes = [c.c_void_p]

import sys
EXE = sys.argv[1] if len(sys.argv) > 1 else r"C:\Users\Shadow\Desktop\HamasClient.exe"
RT_RCDATA = 10
names = []

ENUMPROC = c.WINFUNCTYPE(c.c_long, c.c_void_p, c.c_void_p, c.c_void_p, c.c_long, c.c_long)

def cb(hmod, typ, name, lang, ud):
    def as_name(p):
        v = c.cast(p, c.c_void_p).value or 0
        if v > 0xFFFF:
            return c.cast(p, c.c_wchar_p).value
        return "#%d" % v
    names.append(as_name(name))
    return True

h = k32.LoadLibraryExW(EXE, None, 0x2)  # LOAD_LIBRARY_AS_DATA_FILE
if not h:
    raise SystemExit("LoadLibraryEx failed: %d" % c.get_last_error())
try:
    proc = ENUMPROC(cb)
    if not k32.EnumResourceNamesW(h, c.c_void_p(RT_RCDATA), proc, 0):
        print("EnumResourceNamesW failed or none: %d" % c.get_last_error())
    for n in names:
        hr = k32.FindResourceW(h, n, c.c_wchar_p(RT_RCDATA))
        if not hr:
            continue
        hg = k32.LoadResource(h, hr)
        p = k32.LockResource(hg)
        size = k32.SizeofResource(h, hr)
        data = c.string_at(p, size) if p and size else b""
        md5 = hashlib.md5(data).hexdigest()[:12] if data else "(empty)"
        marker = "GW2v14" if b"engine RenderView camera" in data else ""
        print(f"{n:>6}  size={size:>9,}  md5={md5}  {marker}")
finally:
    k32.FreeLibrary(h)
