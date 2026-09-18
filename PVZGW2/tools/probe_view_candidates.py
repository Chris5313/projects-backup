#!/usr/bin/env python3
"""Probe view-object candidates in the heap dumps.

From v55 log: CAMMGR: 0x31FB4300 +70=0x7A639F0  (runtime VA of the view obj)
Check in the heap dump: bytes at 0x7A639F0 + 0x144 (fresh), +0x360/0x3E0
(proj floats), +0x0 (cam-world?), and scan +0..0x800 for proj-like rows.
"""
import struct, bisect, re

segs = []
def add_seg(va, size, file, off): segs.append((va, va+size, file, off))
add_seg(0x140000000, 60817408, r"C:/Users/Public/gw2_dump_image.bin", 0)
man = open(r"C:/Users/Public/gw2_dump_heap_manifest.txt")
for ln in man:
    ln = ln.strip()
    if not ln or ln.startswith("#"): continue
    fo, va, sz = ln.split(",")
    add_seg(int(va,16), int(sz,16), "HEAP", int(fo,16))
segs.sort(); starts=[s[0] for s in segs]
heap = open(r"C:/Users/Public/gw2_dump_heap.bin","rb")
imgf = open(r"C:/Users/Public/gw2_dump_image.bin","rb")

def read(va, n):
    i = bisect.bisect_right(starts, va) - 1
    if i < 0: return None
    lo, hi, f, off = segs[i]
    d = va - lo
    if d < 0 or d+n > hi-lo: return None
    if f == "HEAP": heap.seek(off+d); return heap.read(n)
    imgf.seek(off+d); return imgf.read(n)

def floats(va, cnt):
    b = read(va, cnt*4)
    if not b: return None
    return struct.unpack(f"<{cnt}f", b)

def qword(va):
    b = read(va, 8)
    return struct.unpack("<Q", b)[0] if b else None

def u8(va):
    b = read(va, 1)
    return b[0] if b else None

CANDS = {
    "view@camMgr+0x70 (v55 log)": 0x7A639F0,
    "active-static singleton": 0x142D05410,  # static itself; its VALUE is runtime ptr
}
# also: the value of the static would be in the image dump at that address
sv = qword(0x142D05410)
print("qword_142D05410 value in image dump:", hex(sv) if sv else None)
if sv and sv > 0x10000:
    CANDS["active-static VALUE"] = sv

for name, va in CANDS.items():
    print(f"\n== {name} @ {hex(va)} ==")
    fresh = u8(va + 0x144)
    print(f"  fresh@+0x144 = {fresh}")
    for off in (0x0, 0x320, 0x360, 0x3E0):
        f = floats(va + off, 8)
        if f:
            print(f"  +{off:#05x}: " + " ".join(f"{v:9.3f}" for v in f))
        else:
            print(f"  +{off:#05x}: <unreadable>")
    # scan for proj-like 16-float blocks
    found = 0
    off = 0
    while off <= 0x800 and found < 4:
        f = floats(va + off, 16)
        if f and abs(f[0]) > 0.5 and abs(f[5]) > 0.5 and abs(f[0]) < 10 and abs(f[5]) < 10:
            print(f"  PROJ-like @+{off:#05x}: [0]={f[0]:.3f} [5]={f[5]:.3f} [10]={f[10]:.3f} [14]={f[14]:.3f}")
            found += 1
        off += 0x10
