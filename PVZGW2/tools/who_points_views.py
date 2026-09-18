#!/usr/bin/env python3
"""For the 63 view objects, find pointers to each in the heap.
The engine's CANONICAL view (what qword_142D05410 should hold) is likely the
one referenced by a container with the submit-function pattern. Count refs."""
import struct, bisect

segs = []
for ln in open(r"C:/Users/Public/gw2_dump_heap_manifest.txt"):
    ln = ln.strip()
    if not ln or ln.startswith("#"): continue
    fo, va, sz = ln.split(",")
    segs.append((int(va,16), int(sz,16), int(fo,16)))
by_fo = sorted(segs, key=lambda s: s[2])
starts = [s[0] for s in segs]
heap = open(r"C:/Users/Public/gw2_dump_heap.bin","rb")

VIEWS = [0x3c7f9b0,0x3b5a0390,0x3b6b3bc0,0x3b6b40e0,0x3b6b4600,0x3b6b76a0,
         0x3b6b7cc0,0x3b6b81e0,0x3b6b8700,0x3b76ef40,0x3b76f030,0x3b76f360,
         0x3b76f450,0x3b76f780,0x3b76f870,0x3b76fba0,0x3b76fc90,0x3b7700e0,
         0x3b7701d0,0x3b770500,0x3b7705f0,0x3b770920,0x3b770a10,0x3b770d40,
         0x3b770e30,0x3b771250,0x3b771580,0x3b771670,0x3b77b180,0x3b77b270]
VSET = set(VIEWS)

refs = {v: [] for v in VIEWS}
for va, size, off in by_fo:
    heap.seek(off)
    CH = 1 << 23
    base = 0
    while base < size:
        buf = heap.read(min(CH, size - base))
        if not buf: break
        for i in range(0, len(buf) - 8, 8):
            v = struct.unpack_from("<Q", buf, i)[0]
            if v in VSET:
                refs[v].append(va + base + i)
        base += CH

print("view -> refcount (first 8 referrer VAs):")
for v in VIEWS:
    r = refs[v]
    print(f"  {hex(v)}: {len(r)} refs", [hex(x) for x in r[:4]])
