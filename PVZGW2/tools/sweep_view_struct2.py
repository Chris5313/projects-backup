#!/usr/bin/env python3
"""Correct sweep: real VA per hit (segment va + offset within segment)."""
import struct

segs = []  # (va, size, fileoff)
for ln in open(r"C:/Users/Public/gw2_dump_heap_manifest.txt"):
    ln = ln.strip()
    if not ln or ln.startswith("#"): continue
    fo, va, sz = ln.split(",")
    segs.append((int(va,16), int(sz,16), int(fo,16)))
by_fo = sorted(segs, key=lambda s: s[2])
heap = open(r"C:/Users/Public/gw2_dump_heap.bin","rb")

def scan(desc, check, limit=12):
    hits = 0
    for va, size, off in by_fo:
        heap.seek(off)
        CH = 1 << 22
        base = 0
        while base < size:
            n = min(CH, size - base)
            buf = heap.read(n)
            if not buf: break
            for i in range(0, len(buf) - 64, 4):
                f = struct.unpack_from("<16f", buf, i)
                if check(f):
                    print(f"  {desc}: VA {hex(va + base + i)}  [0]={f[0]:.4f} [5]={f[5]:.4f} [12]={f[12]:.1f} [13]={f[13]:.1f} [14]={f[14]:.1f}")
                    hits += 1
                    if hits >= limit: return
            base += n
    print(f"  ({desc}: {hits} hits)")

print("== proj rows (1.0711 / 1.9042) ==")
scan("proj", lambda f: abs(abs(f[0])-1.0711)<0.01 and abs(abs(f[5])-1.9042)<0.02)
print("== cam-world translation (~225.7, 62.1, -162.1) ==")
scan("campos", lambda f: abs(f[12]-225.7)<20 and abs(f[13]-62.1)<20 and abs(f[14]+162.1)<20)
