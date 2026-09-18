#!/usr/bin/env python3
"""Sweep heap for the REAL view object: look for
  - proj rows: (1.0712, 0, 0) / (0, 1.9048, 0) anywhere,
  - cam-world basis: unit-length rows with translation near (225.7, 62.1, -162.1).
Report absolute VAs (heap manifest -> VA)."""
import struct, bisect, re

segs = []
man = open(r"C:/Users/Public/gw2_dump_heap_manifest.txt")
for ln in man:
    ln = ln.strip()
    if not ln or ln.startswith("#"): continue
    fo, va, sz = ln.split(",")
    segs.append((int(va,16), int(sz,16), int(fo,16)))  # (va, size, fileoff)
# sort by file offset for sequential scanning
by_fo = sorted(segs, key=lambda s: s[2])
starts = [s[0] for s in sorted(segs)]
heap = open(r"C:/Users/Public/gw2_dump_heap.bin","rb")

def heap_fo_to_va(fo):
    import bisect as b
    offs = [e[2] for e in by_fo]
    i = b.bisect_right(offs, fo) - 1
    if i < 0: return None
    va, size, o = by_fo[i]
    if fo - o >= size: return None
    return va + (fo - o)

def scan_pattern(desc, check, step=4):
    hits = 0
    for off, va, size in by_fo:
        heap.seek(off)
        # read in chunks
        CH = 1 << 20
        base = 0
        while base < size:
            n = min(CH, size - base)
            buf = heap.read(n)
            if not buf: break
            for i in range(0, len(buf) - 64, step):
                f = struct.unpack_from("<16f", buf, i)
                if check(f):
                    real_va = va + base + i
                    print(f"  {desc}: VA {hex(real_va)}  [0]={f[0]:.4f} [5]={f[5]:.4f}")
                    hits += 1
                    if hits > 15: return
            base += n
    print(f"  ({desc}: {hits} hits)")

print("== proj-like rows (|f0|~1.07, |f5|~1.90) ==")
scan_pattern("proj", lambda f: abs(abs(f[0])-1.0712)<0.05 and abs(abs(f[5])-1.9048)<0.08)
print("== cam-world translation (225.7, 62.1, -162.1) ==")
scan_pattern("campos", lambda f: abs(f[12]-225.7)<25 and abs(f[13]-62.1)<25 and abs(f[14]+162.1)<25)
