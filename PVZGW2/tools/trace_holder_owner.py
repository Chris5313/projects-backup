#!/usr/bin/env python3
"""Who references container 0x8653BA30? Scan heap + image; print referrer ctx."""
import struct

CONT = 0x8653BA30
sig = struct.pack("<Q", CONT)

segs = []
for ln in open(r"C:/Users/Public/gw2_dump_heap_manifest.txt"):
    ln = ln.strip()
    if not ln or ln.startswith("#"): continue
    fo, va, sz = ln.split(",")
    segs.append((int(va,16), int(sz,16), int(fo,16)))
segs.sort(); starts=[s[0] for s in segs]
heap = open(r"C:/Users/Public/gw2_dump_heap.bin","rb")
img = open(r"C:/Users/Public/gw2_dump_image.bin","rb").read()
BASE = 0x140000000

print("== image refs ==")
start = 0; n = 0
while n < 6:
    i = img.find(sig, start)
    if i < 0: break
    print(f"  img VA {hex(BASE+i)}")
    n += 1; start = i+1
if n == 0: print("  (none)")

print("== heap refs ==")
hits = 0
for va, size, off in sorted(segs, key=lambda s: s[2]):
    heap.seek(off)
    CH = 1 << 22
    base = 0
    stop = False
    while base < size and not stop:
        buf = heap.read(min(CH, size - base))
        if not buf: break
        j = 0
        while True:
            i = buf.find(sig, j)
            if i < 0: break
            if (base + i) % 8 == 0:
                ref_va = va + base + i
                print(f"  heap VA {hex(ref_va)}")
                hits += 1
                if hits > 10: stop = True; break
            j = i + 1
        base += CH
