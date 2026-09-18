#!/usr/bin/env python3
"""Cross-check the v55 CAMMGR log against the real view location.
v55 log: CAMMGR: 0x31FB4300 +70=0x7A639F0
Real live view (proj-sig sweep): 0x3C7F9B0
Question: is 0x7A639F0 the same object? What's at 0x7A639F0+0x360/0x3E0?"""
import struct, bisect

segs = []
for ln in open(r"C:/Users/Public/gw2_dump_heap_manifest.txt"):
    ln = ln.strip()
    if not ln or ln.startswith("#"): continue
    fo, va, sz = ln.split(",")
    segs.append((int(va,16), int(sz,16), int(fo,16)))
segs.sort(); starts=[s[0] for s in segs]
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

for name, va in (("v55 camMgr+0x70 obj", 0x7A639F0), ("real view (sig)", 0x3C7F9B0)):
    print(f"== {name} @ {hex(va)} ==")
    for off in (0x0, 0x140, 0x144, 0x320, 0x360, 0x3E0):
        f = floats(va + off, 8)
        print(f"  +{off:#05x}: " + (" ".join(f"{v:9.3f}" for v in f) if f else "<unreadable>"))
    b = read(va + 0x144, 1)
    print(f"  fresh byte @+0x144: {b[0] if b else None}")
    print()

# Also: does any heap segment cover 0x7A639F0 at all? (v55 session may differ)
i = bisect.bisect_right(starts, 0x7A639F0) - 1
if i >= 0:
    va0, size, off = segs[i]
    print(f"0x7A639F0 nearest seg: va={hex(va0)} size={hex(size)} covered={va0 <= 0x7A639F0 < va0+size}")
