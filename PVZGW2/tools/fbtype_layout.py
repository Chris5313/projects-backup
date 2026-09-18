#!/usr/bin/env python3
"""Reverse GW2's real ClassInfo layout.

Known anchor: ClientPlayerManagerEntity ClassInfo @ 0x1429A95F0.
CI+0x00 = 0x1422DDA58 'PVZClient'   (module/namespace name?)
CI+0x08 = 0x1429A95D8               (prev entry, 0x18 below)
CI+0x28 = 0x1422E8408 'GameplayClientPlayerExtent'  (??)
CI+0x48 = 0x1429A9CB8               (next entry? +0x6C8 delta... or member list)

Dump wider, follow +0x08 chain both ways, and check 0x1429A9CB8.
Also compare with ClientHumanPlayerEntity and DataContainer.
"""
import struct, re
BASE = 0x140000000
data = open(r"C:/Users/Public/gw2_dump_image.bin","rb").read()
N = len(data)
def fo(va): return va-BASE
def rd64(va):
    o=fo(va)
    if 0<=o<=N-8: return struct.unpack_from("<Q",data,o)[0]
def cstr(va,n=80):
    o=fo(va)
    if o is None or o<0x1000 or o>=N: return None
    end=data.find(b"\x00",o,o+n)
    if end<=o: return None
    s=data[o:end].decode(errors="replace")
    return s if re.fullmatch(r"[\x20-\x7e]{2,79}",s) else None

def dump(va, n=0xC0, label=""):
    print(f"-- {label} @ {hex(va)} --")
    for d in range(0, n, 8):
        q = rd64(va+d)
        s = cstr(q) if q else None
        mark = ""
        if s is None and q and 0x140000000 <= q < BASE+N:
            s2 = cstr(rd64(q) or 0)
            if s2: mark = f" ->{s2!r}"
        print(f"  +{d:#04x}: {hex(q or 0)} {s!r}{mark}")

dump(0x1429A95F0, 0x100, "ClientPlayerManagerEntity CI")
# the qword before CI? check CI-0x18..CI
dump(0x1429A95D8, 0x20, "CI-0x18 (prev)")
dump(0x1429A9CB8, 0x40, "CI+0xC8 ref")
EOF_MARKER = None
