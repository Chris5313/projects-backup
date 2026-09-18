#!/usr/bin/env python3
"""Find view objects by scanning for the full cam-world matrix bytes
(eye at cw[12..14] = 301.5, 68.8, -157.2) then back off 0x320."""
import struct, bisect

segs = []
for ln in open(r"C:/Users/Public/gw2_dump_heap_manifest.txt"):
    ln = ln.strip()
    if not ln or ln.startswith("#"): continue
    fo, va, sz = ln.split(",")
    segs.append((int(va,16), int(sz,16), int(fo,16)))
by_fo = sorted(segs, key=lambda s: s[2])
heap = open(r"C:/Users/Public/gw2_dump_heap.bin","rb")
img = open(r"C:/Users/Public/gw2_dump_image.bin","rb")
img_segs = [(0x140000000, 60817408, 0)]

def read_gen(reader, segs_list, va, n):
    ssegs = sorted(segs_list)
    starts = [s[0] for s in ssegs]
    i = bisect.bisect_right(starts, va) - 1
    if i < 0: return None
    va0, size, off = ssegs[i]
    d = va - va0
    if d < 0 or d+n > size: return None
    reader.seek(off+d); return reader.read(n)

# signature: float32 triple near (301.5, 68.8, -157.2), any alignment
def near(v, t, tol): return abs(v - t) < tol

def scan(reader, segs_list, label, limit=24):
    hits = []
    for va, size, off in sorted(segs_list, key=lambda s: s[2]):
        reader.seek(off)
        CH = 1 << 22
        base = 0
        while base < size:
            n = min(CH, size - base)
            buf = reader.read(n)
            if not buf: break
            for i in range(0, len(buf) - 12, 4):
                x, y, z = struct.unpack_from("<3f", buf, i)
                if near(x, 301.5, 6) and near(y, 68.8, 6) and near(z, -157.2, 8):
                    real = va + base + i
                    hits.append(real)
                    if len(hits) >= limit: return hits
            base += n
    return hits

print("scan heap for eye triple (301.5,68.8,-157.2):")
hits = scan(heap, segs, "heap")
for h in hits:
    view = h - 0x330  # eye is at cw[12] = view+0x320+0x30 (if layout holds)
    view2 = h - 0x320  # eye directly at +0x320
    fresh1 = read_gen(heap, segs, view + 0x144, 1)
    fresh2 = read_gen(heap, segs, view2 + 0x144, 1)
    proj1 = read_gen(heap, segs, view + 0x360, 8)
    proj2 = read_gen(heap, segs, view2 + 0x360, 8)
    p1 = struct.unpack("<2f", proj1) if proj1 else None
    p2 = struct.unpack("<2f", proj2) if proj2 else None
    print(f"  eye@{hex(h)}: viewA={hex(view)} fresh={fresh1} proj={p1} | viewB={hex(view2)} fresh={fresh2} proj={p2}")
