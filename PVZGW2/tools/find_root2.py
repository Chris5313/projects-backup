#!/usr/bin/env python3
"""Reverse-chain hunt v2 on the FRESH dump (2026-09-18 09:46).

View finder (FOV-independent):
  - Anchor on projection structure: pj[11]=-1, pj[14]=0.1 (RH near/far —
    same in every FOV), then read f_x/f_y dynamically.
  - view = proj_va - 0x360; eye = view+0x350 (floats 0..2 — same as runtime
    EyeFromView which reads view+0x320+12).
  - Self-validate: VP @ view+0x460 w-row must annihilate eye (w = e.b3+e.b7+
    e.b11+b15 ~ 0) — the 25-view-proven test.

Then reverse chains:
  pass A: qwords pointing into view±0x40  -> level-1 holders
  pass B: qwords pointing into holder±0x20 -> level-2 holders
  level-2 holders inside IMAGE = spankerfield-style static roots.
"""
import struct
import numpy as np
from collections import defaultdict

PUB = r"C:/Users/Public/"
BASE = 0x140000000

img = open(PUB + "gw2_dump_image.bin", "rb").read()
print(f"image {len(img)/1e6:.0f}MB")

segs = []
for ln in open(PUB + "gw2_dump_heap_manifest.txt"):
    ln = ln.strip()
    if not ln or ln.startswith("#"):
        continue
    fo, va, sz = ln.split(",")
    segs.append((int(va, 16), int(sz, 16), int(fo, 16)))
segs.sort()
print(f"segments {len(segs)}")

heap = open(PUB + "gw2_dump_heap.bin", "rb")

# ---------- 1. views (FOV-independent anchor) ----------
views = []           # (va, eye, fx, fy)
fx_hist = defaultdict(int)
for va, sz, fo in segs:
    nf = sz // 4
    if nf < (0x4A0 // 4):
        continue
    heap.seek(fo)
    f = np.frombuffer(heap.read(nf * 4), dtype=np.float32)
    # proj[11] = -1 (RH w-negative — invariant); near/far ([14] etc.) are
    # level-dependent so NOT anchored. f_x read dynamically at c+0.
    m11 = np.nonzero(np.abs(f[: nf - 15] + 1.0) < 0.02)[0]
    for c in m11:
        c = int(c)
        vf = c - (0x360 // 4)  # view base float offset (proj @ view+0x360)
        if vf < 0 or vf + (0x4A0 // 4) > nf:
            continue
        e = f[vf + (0x350 // 4): vf + (0x350 // 4) + 3]
        elen = float(np.sqrt(np.sum(e.astype(np.float64) ** 2)))
        if not (15 < elen < 100000):
            continue
        b = f[vf + (0x460 // 4): vf + (0x460 // 4) + 16]
        w = float(e[0]) * float(b[3]) + float(e[1]) * float(b[7]) + \
            float(e[2]) * float(b[11]) + float(b[15])
        if abs(w) > 5.0:
            continue
        # finite check on VP
        if not np.all(np.isfinite(b)) or np.max(np.abs(b)) > 1e6:
            continue
        fx, fy = float(f[c]), float(f[c + 5])
        # screen-projection gate: f_y/f_x must be a display aspect ratio.
        # Shadow/depth projections have arbitrary ratios (found: -0.34, 0, ...).
        if fx <= 0.05 or fy <= 0.05:
            continue
        ratio = fy / fx
        if not (1.0 < ratio < 4.0):
            continue
        fx_hist[round(fx, 3)] += 1
        views.append((va + vf * 4, (float(e[0]), float(e[1]), float(e[2])), fx, fy))

print(f"validated views: {len(views)}")
print(f"f_x histogram: {dict(sorted(fx_hist.items(), key=lambda kv: -kv[1])[:8])}")
for v in sorted(views, key=lambda x: -x[2])[:12]:
    print(f"  view {hex(v[0])} eye=({v[1][0]:.1f},{v[1][1]:.1f},{v[1][2]:.1f}) fx={v[2]:.4f} fy={v[3]:.4f}")

if not views:
    raise SystemExit("no validated views — anchor still wrong; stop")

# ---------- 2. pass A: pointers into view±0x40 ----------
win = 0x40
lo = np.array([np.uint64(v[0] - win) for v in views], dtype=np.uint64)
hi = np.array([np.uint64(v[0] + win) for v in views], dtype=np.uint64)
print(f"\npass A: {len(views)} view windows ...")

def ptrs_u64(buf):
    """All qwords in buf at 4-byte stride, as uint64 array (len 0 if short)."""
    n4 = len(buf) // 4
    if n4 < 2:
        return np.empty(0, dtype=np.uint64)
    lo = np.frombuffer(buf, dtype=np.uint32, count=n4)
    hi = np.frombuffer(buf[4:4 + n4 * 4], dtype=np.uint32, count=n4)
    return (lo.astype(np.uint64) | (hi.astype(np.uint64) << np.uint64(32)))

def scan_ptrs(lo, hi, tag):
    """Find qwords (image+heap) landing in any [lo_i, hi_i]."""
    hits = []
    def check(arr, base_addr, where):
        v = arr[arr < np.uint64(0x7FFFFFFFFFFF)]
        if len(v) == 0:
            return
        CH = 64
        for s in range(0, len(lo), CH):
            e = min(s + CH, len(lo))
            idx = np.nonzero((v[None, :] >= lo[s:e, None]) & (v[None, :] <= hi[s:e, None]))
            for row, col in zip(*idx):
                hits.append((int(v[col]), where, base_addr + int(col) * 4, s + row))
    # image (61MB — one pass)
    check(ptrs_u64(img), BASE, "IMAGE")
    # heap segments
    for va, sz, fo in segs:
        if sz < 16:
            continue
        heap.seek(fo)
        check(ptrs_u64(heap.read(sz)), va, "heap")
    print(f"  {tag}: {len(hits)} hits")
    return hits

hitsA = scan_ptrs(lo, hi, "pass A")
holder_sites = sorted(set(h[2] for h in hitsA))
img_A = [a for a in holder_sites if BASE <= a < BASE + len(img)]
print(f"distinct L1 holder sites: {len(holder_sites)} | in IMAGE: {len(img_A)}")
for a in img_A[:20]:
    print(f"  IMAGE L1 {hex(a)}")

# ---------- 3. pass B: pointers into L1 holders±0x20 ----------
win2 = 0x20
lo2 = np.array([np.uint64(a - win2) for a in holder_sites[:4000]], dtype=np.uint64)
hi2 = np.array([np.uint64(a + win2) for a in holder_sites[:4000]], dtype=np.uint64)
hitsB = scan_ptrs(lo2, hi2, "pass B")
l2_sites = sorted(set(h[2] for h in hitsB))
img_B = [a for a in l2_sites if BASE <= a < BASE + len(img)]
print(f"distinct L2 sites: {len(l2_sites)} | in IMAGE: {len(img_B)}")
for a in img_B[:40]:
    print(f"  IMAGE L2 {hex(a)}")

# ---------- 4. save ----------
with open(PUB + "gw2_static_candidates.txt", "w") as f:
    f.write("# views (validated)\n")
    for v in views:
        f.write(f"VIEW {hex(v[0])} eye=({v[1][0]:.2f},{v[1][1]:.2f},{v[1][2]:.2f}) fx={v[2]:.4f}\n")
    f.write("\n# L1 holder sites in IMAGE\n")
    for a in img_A:
        f.write(f"L1 {hex(a)}\n")
    f.write("\n# L2 sites in IMAGE (verify in IDA)\n")
    for a in img_B:
        f.write(f"L2 {hex(a)}\n")
print("\nsaved -> gw2_static_candidates.txt")
