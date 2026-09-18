#!/usr/bin/env python3
"""Who points at the real live view (0x3C7F9B0)?

Scan image + heap dumps for the pointer value 0x3C7F9B0 and report each
holding address. Then check what those holders are reachable from.
"""
import struct, bisect

VIEW = 0x3C7F9B0
sig = struct.pack("<Q", VIEW)

# ---- image scan ----
img = open(r"C:/Users/Public/gw2_dump_image.bin","rb").read()
BASE = 0x140000000
print("== image hits ==")
start = 0
n = 0
while True:
    i = img.find(sig, start)
    if i < 0: break
    print(f"  img VA {hex(BASE+i)}")
    n += 1; start = i+1
    if n > 10: break
if n == 0: print("  (none)")

# ---- heap scan ----
segs = []
for ln in open(r"C:/Users/Public/gw2_dump_heap_manifest.txt"):
    ln = ln.strip()
    if not ln or ln.startswith("#"): continue
    fo, va, sz = ln.split(",")
    segs.append((int(va,16), int(sz,16), int(fo,16)))
heap = open(r"C:/Users/Public/gw2_dump_heap.bin","rb")
print("== heap hits ==")
hits = 0
for va, size, off in sorted(segs, key=lambda s: s[2]):
    heap.seek(off)
    CH = 1 << 22
    base = 0
    while base < size:
        buf = heap.read(min(CH, size - base))
        if not buf: break
        j = 0
        while True:
            i = buf.find(sig, j)
            if i < 0 or i % 8: break
            print(f"  heap VA {hex(va+base+i)}")
            hits += 1
            j = i + 1
            if hits > 15: break
        base += CH
    if hits > 15: break
if hits == 0: print("  (none)")
