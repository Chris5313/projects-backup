#!/usr/bin/env python3
"""Round 2: include the IMAGE (gw2_dump_image.bin) in the reverse pointer walk.
A .data static holding &holder == GameRenderer-equivalent root."""
import struct
import numpy as np
from bisect import bisect_right

BASE = 0x140000000

segs = []
for ln in open(r"C:/Users/Public/gw2_dump_heap_manifest.txt"):
    ln = ln.strip()
    if not ln or ln.startswith("#"):
        continue
    fo, va, sz = ln.split(",")
    segs.append((int(va, 16), int(sz, 16), int(fo, 16)))
segs.sort()
seg_vas = [s[0] for s in segs]

img = open(r"C:/Users/Public/gw2_dump_image.bin", "rb").read()
print(f"image: {len(img)/1e6:.1f} MB -> VA {BASE:#x}..{BASE+len(img):#x}")

def va_to_fo(va):
    # image first
    if BASE <= va < BASE + len(img):
        return None, img, va - BASE          # special: (None, buf, off)
    i = bisect_right(seg_vas, va) - 1
    if i < 0:
        return None, None, None
    sva, ssz, sfo = segs[i]
    if sva <= va < sva + ssz:
        return sfo + (va - sva), None, None
    return None, None, None

heap = open(r"C:/Users/Public/gw2_dump_heap.bin", "rb")

# ---- views (reuse round-1 result logic, image excluded as before) ---------
CHUNK = 1 << 26
views = set()
for sva, ssz, sfo in segs:
    if BASE <= sva < BASE + len(img):
        continue
    off = 0
    while off < ssz:
        n = min(CHUNK, ssz - off)
        heap.seek(sfo + off)
        buf = heap.read(n)
        if not buf:
            break
        arr = np.nan_to_num(np.frombuffer(buf[:len(buf)//4*4], dtype="<f4"),
                            nan=0.0, posinf=0.0, neginf=0.0)
        cand = np.nonzero(np.abs(arr - 1.0711) < 0.03)[0]
        for idx in cand:
            i4 = int(idx)*4
            if i4 + 0x400 > len(buf):
                continue
            f5 = struct.unpack_from("<f", buf, i4 + 0x14)[0]
            if not (1.9042 - 0.03 < f5 < 1.9042 + 0.03):
                continue
            pj = struct.unpack_from("<16f", buf, i4)
            if not (abs(pj[11] + 1) < 0.02 and abs(pj[14] - 0.1) < 0.02):
                continue
            views.add(sva + off + i4 - 0x360)
        off += n
print(f"views: {len(views)}")

# ---- holders: qwords == view, scanned in HEAP + IMAGE ----------------------
holder_hits = {}      # site -> view
def scan_qwords(buf, base_va, targets):
    arr = np.frombuffer(buf[:len(buf)//8*8], dtype="<u8")
    out = {}
    for t in targets:
        idx = np.nonzero(arr == t)[0]
        for i in idx:
            out[base_va + int(i)*8] = t
    return out

for v in views:
    pass

# heap pass
for sva, ssz, sfo in segs:
    if BASE <= sva < BASE + len(img):
        continue
    off = 0
    while off < ssz:
        n = min(CHUNK, ssz - off)
        heap.seek(sfo + off)
        buf = heap.read(n)
        if not buf:
            break
        for site, val in scan_qwords(buf, sva + off, views).items():
            holder_hits[site] = val
        off += n
# image pass
holder_hits.update(scan_qwords(img, BASE, views))
print(f"holders: {len(holder_hits)} (image-resident: {sum(1 for s in holder_hits if BASE <= s < BASE+len(img))})")

holders = set(holder_hits.keys())

# ---- level2: qwords == holder sites, in HEAP + IMAGE ------------------------
lv2 = {}
for sva, ssz, sfo in segs:
    if BASE <= sva < BASE + len(img):
        continue
    off = 0
    while off < ssz:
        n = min(CHUNK, ssz - off)
        heap.seek(sfo + off)
        buf = heap.read(n)
        if not buf:
            break
        for site, val in scan_qwords(buf, sva + off, holders).items():
            lv2[site] = val
        off += n
lv2.update(scan_qwords(img, BASE, holders))
img_lv2 = {s: v for s, v in lv2.items() if BASE <= s < BASE + len(img)}
print(f"lv2 sites: {len(lv2)}, IMAGE-RESIDENT ROOTS: {len(img_lv2)}")
for s, v in sorted(img_lv2.items()):
    print(f"  ROOT .data[{s:#x}] = {v:#x}  (holder slot, holds view {holder_hits[v]:#x})")

# ---- level3: who points at those roots (image .data → .data or heap) --------
roots = set(img_lv2.keys())
if roots:
    lv3 = {}
    for sva, ssz, sfo in segs:
        if BASE <= sva < BASE + len(img):
            continue
        off = 0
        while off < ssz:
            n = min(CHUNK, ssz - off)
            heap.seek(sfo + off)
            buf = heap.read(n)
            if not buf:
                break
            for site, val in scan_qwords(buf, sva + off, roots).items():
                lv3[site] = val
            off += n
    lv3.update(scan_qwords(img, BASE, roots))
    print(f"lv3 (pointers to roots): {len(lv3)}")
    for s, v in sorted(lv3.items())[:20]:
        loc = "IMG" if BASE <= s < BASE + len(img) else "heap"
        print(f"  {loc} {s:#x} -> root {v:#x}")
