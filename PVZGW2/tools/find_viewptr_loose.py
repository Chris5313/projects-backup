#!/usr/bin/env python3
"""Loosen the pointer hunt for view 0x3C7F9B0:
 - pointers INTO the object range 0x3C7F9B0..0x3C7FDD0 (any offset inside)
 - 4-byte truncated values (heap addresses < 4 GB in x64 apps happen)
"""
import struct

VIEW_LO, VIEW_HI = 0x3C7F9B0, 0x3C7FDD0

img = open(r"C:/Users/Public/gw2_dump_image.bin","rb").read()
BASE = 0x140000000
print("== image: qwords pointing into view obj ==")
n = 0
for i in range(0x100000, len(img) - 8, 8):
    v = struct.unpack_from("<Q", img, i)[0]
    if VIEW_LO <= v < VIEW_HI:
        print(f"  img VA {hex(BASE+i)} -> {hex(v)} (obj+{v-VIEW_LO:#x})")
        n += 1
        if n > 12: break
if n == 0: print("  (none)")

segs = []
for ln in open(r"C:/Users/Public/gw2_dump_heap_manifest.txt"):
    ln = ln.strip()
    if not ln or ln.startswith("#"): continue
    fo, va, sz = ln.split(",")
    segs.append((int(va,16), int(sz,16), int(fo,16)))
heap = open(r"C:/Users/Public/gw2_dump_heap.bin","rb")
print("== heap: qwords pointing into view obj ==")
hits = 0
for va, size, off in sorted(segs, key=lambda s: s[2]):
    heap.seek(off)
    CH = 1 << 22
    base = 0
    while base < size:
        buf = heap.read(min(CH, size - base))
        if not buf: break
        for i in range(0, len(buf) - 8, 8):
            v = struct.unpack_from("<Q", buf, i)[0]
            if VIEW_LO <= v < VIEW_HI:
                print(f"  heap VA {hex(va+base+i)} -> {hex(v)} (obj+{v-VIEW_LO:#x})")
                hits += 1
                if hits > 12: break
        base += CH
    if hits > 12: break
if hits == 0: print("  (none)")
