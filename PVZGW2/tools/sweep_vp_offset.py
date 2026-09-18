#!/usr/bin/env python3
"""Find GW2's precomputed ViewProjection offset inside the live view object.

BF4's RenderView stores m_ViewProjection precomputed at +0x420 (spankerfield
reads it directly). GW2's view object is the same engine family (proj @0x360,
dup @0x3E0 — matches BF4's 0x320/0x3E0 rhythm). If GW2 also precomputes VP,
some 4x4 block in the view object must solve eye == that view's own camera eye
(cam-world translation @ view+0x350).

Method: anchor views via the exact proj signature @ view+0x360, sweep every
4-byte offset in [view-0x40, view+0x900], test both matrix conventions with
Cramer solve against the view's eye. Histogram offsets across all views.
"""
import struct
import numpy as np
from collections import Counter

BASE = 0x140000000

segs = []
for ln in open(r"C:/Users/Public/gw2_dump_heap_manifest.txt"):
    ln = ln.strip()
    if not ln or ln.startswith("#"):
        continue
    fo, va, sz = ln.split(",")
    segs.append((int(va, 16), int(sz, 16), int(fo, 16)))
by_fo = sorted(segs, key=lambda s: s[2])

heap = open(r"C:/Users/Public/gw2_dump_heap.bin", "rb")


def close_proj(f):
    return (abs(f[0] - 1.0711) < 0.02 and abs(f[5] - 1.9042) < 0.02 and
            abs(f[11] + 1) < 0.02 and abs(f[14] - 0.1) < 0.02 and
            abs(f[1]) < 1e-5 and abs(f[4]) < 1e-5)


def det3(m):
    return (m[0] * (m[4] * m[8] - m[5] * m[7])
            - m[1] * (m[3] * m[8] - m[5] * m[6])
            + m[2] * (m[3] * m[7] - m[4] * m[6]))


def solve_eye(A, b):
    """Cramer solve 3x3 A*o=b; returns None or eye tuple."""
    D = det3(A)
    if abs(D) < 1e-10:
        return None
    x = [b[0], A[1], A[2], b[1], A[4], A[5], b[2], A[7], A[8]]
    y = [A[0], b[0], A[2], A[3], b[1], A[5], A[6], b[2], A[8]]
    z = [A[0], A[1], b[0], A[3], A[4], b[1], A[6], A[7], b[2]]
    dx, dy, dz = det3(x) / D, det3(y) / D, det3(z) / D
    if not all(-1e7 < v < 1e7 for v in (dx, dy, dz)):
        return None
    return (dx, dy, dz)


def vp_solves(block, eye):
    """Test 16-float block as VP (both conventions) against eye.
    Convention A (col-vector): rows r0=(m0,m4,m8),r1=(m1,m5,m9),r3=(m3,m7,m11)
    Convention B (row-vector): block transposed = convention A on transposed."""
    f = block
    # quick sanity: all finite, bounded
    for v in f:
        if not (-1e6 < v < 1e6):
            return False
    # A: affine rows from columns
    A = (f[0], f[4], f[8], f[1], f[5], f[9], f[3], f[7], f[11])
    b = (-f[12], -f[13], 1.0 - f[15])
    if solve_eye(A, b):
        e = solve_eye(A, b)
        if e and sum((e[k] - eye[k]) ** 2 for k in range(3)) < 4.0:
            return True
    # B: transposed block
    t = (f[0], f[1], f[2], f[3],
         f[4], f[5], f[6], f[7],
         f[8], f[9], f[10], f[11],
         f[12], f[13], f[14], f[15])
    A2 = (t[0], t[4], t[8], t[1], t[5], t[9], t[3], t[7], t[11])
    b2 = (-t[12], -t[13], 1.0 - t[15])
    e2 = solve_eye(A2, b2)
    return bool(e2 and sum((e2[k] - eye[k]) ** 2 for k in range(3)) < 4.0)


CHUNK = 1 << 23
hits = Counter()          # offset -> count
conv_count = Counter()    # offset -> {"A","B"}
n_views = 0
examples = {}             # offset -> (viewva, block values)

for va0, size, off0 in by_fo:
    base = 0
    while base < size:
        n = min(CHUNK, size - base)
        heap.seek(off0 + base)
        buf = heap.read(n)
        if not buf or len(buf) < 64:
            break
        arr = np.nan_to_num(np.frombuffer(buf[:len(buf) // 4 * 4], dtype="<f4"), nan=0.0, posinf=0.0, neginf=0.0)
        if len(arr) < 16:
            break
        # candidate anchors: 1.0711
        cand = np.nonzero(np.abs(arr - 1.0711) < 0.02)[0]
        for idx in cand:
            i4 = int(idx) * 4
            if i4 + 64 > len(buf):
                continue
            f = struct.unpack_from("<16f", buf, i4)
            if not close_proj(f):
                continue
            viewva = (va0 + base + i4) - 0x360
            if viewva <= 0x10000:
                continue
            # eye @ view+0x350 (cam-world @0x320, translation +0x30)
            ro_eye = viewva + 0x350 - va0 - base   # chunk-relative
            if ro_eye < 0 or ro_eye + 12 > len(buf):
                continue
            eye = struct.unpack_from("<3f", buf, ro_eye)
            if not all(-1e5 < v < 1e5 and v != 0 for v in eye):
                continue
            n_views += 1
            # sweep the view region for VP blocks
            lo = max(viewva - 0x40, va0)
            hi = min(viewva + 0x900, va0 + size)
            for vo in range(lo, hi - 64, 4):
                ro = vo - va0 - base
                if ro < 0 or ro + 64 > len(buf):
                    continue
                blk = struct.unpack_from("<16f", buf, ro)
                # skip the proj blocks themselves
                if vo in (viewva + 0x360, viewva + 0x3E0):
                    continue
                # convention check
                if vp_solves(blk, eye):
                    rel = vo - viewva
                    hits[rel] += 1
                    if rel not in examples:
                        examples[rel] = (viewva, blk, eye)
        base += n

print(f"views anchored: {n_views}")
print(f"\ntop VP-offset candidates (offset within view object):")
for rel, cnt in hits.most_common(12):
    convs = conv_count.get(rel, "?")
    print(f"  view+0x{rel:03X}: {cnt} views")
print("\nexamples:")
for rel, cnt in hits.most_common(5):
    viewva, blk, eye = examples[rel]
    print(f"\n  view+0x{rel:03X} (view @ {viewva:#x}, eye=({eye[0]:.1f},{eye[1]:.1f},{eye[2]:.1f}))")
    for r in range(4):
        print(f"    row{r}: {blk[r*4]:12.5f} {blk[r*4+1]:12.5f} {blk[r*4+2]:12.5f} {blk[r*4+3]:12.5f}")
