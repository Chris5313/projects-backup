#!/usr/bin/env python3
"""Find ALL live view objects: scan heap for the cam-world basis row signature
at view+0x320 (-0.988, 0.032, -0.153, 0), then for each hit:
  fresh = u8(hit + 0x144), proj = floats(hit + 0x360, 8)
Report VAs of every live view. Then scan image too.
"""
import struct, bisect

segs = []
for ln in open(r"C:/Users/Public/gw2_dump_heap_manifest.txt"):
    ln = ln.strip()
    if not ln or ln.startswith("#"): continue
    fo, va, sz = ln.split(",")
    segs.append((int(va,16), int(sz,16), int(fo,16)))
by_fo = sorted(segs, key=lambda s: s[2])
heap = open(r"C:/Users/Public/gw2_dump_heap.bin","rb")

SIG = struct.pack("<4f", -0.988, 0.032, -0.153, 0.0)

def read_at(va, n):
    i = bisect.bisect_right(sorted(s[0] for s in segs), va) - 1
    if i < 0: return None
    ssegs = sorted(segs)
    va0, size, off = ssegs[i]
    d = va - va0
    if d < 0 or d+n > size: return None
    heap.seek(off+d); return heap.read(n)

views = []
for va, size, off in by_fo:
    heap.seek(off)
    CH = 1 << 22
    base = 0
    while base < size:
        n = min(CH, size - base)
        buf = heap.read(n)
        if not buf: break
        start = 0
        while True:
            j = buf.find(SIG, start)
            if j < 0 or j + 4 > len(buf): break
            if (base + j) % 4 == 0:  # float aligned
                view_va = va + base + j - 0x320
                views.append(view_va)
            start = j + 1
        base += n
print(f"view-object candidates (camWorld@+0x320 signature): {len(views)}")
for v in views[:20]:
    fresh = read_at(v + 0x144, 1)
    proj = read_at(v + 0x360, 32)
    p = struct.unpack("<8f", proj) if proj else None
    cw = read_at(v + 0x320, 64)
    cwf = struct.unpack("<16f", cw) if cw else None
    print(f"view@{hex(v)} fresh={fresh[0] if fresh else None} "
          f"proj0={p[0]:.3f} proj5={p[5]:.3f} " if p else f"view@{hex(v)} fresh=?",
          f"eye=({cwf[12]:.1f},{cwf[13]:.1f},{cwf[14]:.1f})" if cwf else "")
