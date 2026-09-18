#!/usr/bin/env python3
"""Dump the REAL view object around VA 0x3C7FD90 (live proj, P[14]=-1).

If the engine layout (proj@view+0x3E0, fresh@view+0x144) holds for this fork,
view start = 0x3C7FD90 - 0x3E0 = 0x3C7F9B0.
"""
import struct, bisect

segs = []
for ln in open(r"C:/Users/Public/gw2_dump_heap_manifest.txt"):
    ln = ln.strip()
    if not ln or ln.startswith("#"): continue
    fo, va, sz = ln.split(",")
    segs.append((int(va,16), int(sz,16), int(fo,16)))
segs.sort()
starts = [s[0] for s in segs]
heap = open(r"C:/Users/Public/gw2_dump_heap.bin","rb")

def read(va, n):
    i = bisect.bisect_right(starts, va) - 1
    if i < 0: return None
    va0, size, off = segs[i]
    d = va - va0
    if d < 0 or d+n > size: return None
    heap.seek(off+d); return heap.read(n)

def floats(va, cnt):
    b = read(va, cnt*4)
    return struct.unpack(f"<{cnt}f", b) if b else None

def u8(va):
    b = read(va, 1)
    return b[0] if b else None

PROJ = 0x3C7FD90
view = PROJ - 0x3E0
print(f"proj@{hex(PROJ)}  hypothesized view@{hex(view)}")
print(f"fresh@view+0x144 = {u8(view+0x144)}")
print()
for off in (0x0, 0x40, 0x80, 0xC0, 0x100, 0x140, 0x180, 0x1C0, 0x200, 0x240,
            0x280, 0x2C0, 0x300, 0x320, 0x340, 0x360, 0x380, 0x3A0, 0x3C0, 0x3E0):
    f = floats(view + off, 16)
    if f is None:
        print(f"view+{off:#05x}: <gap>")
        continue
    r1 = " ".join(f"{v:8.3f}" for v in f[:8])
    r2 = " ".join(f"{v:8.3f}" for v in f[8:])
    print(f"view+{off:#05x}: {r1}")
    print(f"            {r2}")
