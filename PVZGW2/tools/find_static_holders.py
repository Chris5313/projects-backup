#!/usr/bin/env python3
"""Find static .data roots: image addresses whose qword points into the heap
where our holder/view objects live. These are the GameRenderer-style globals.

Focus zone: 0x142000000-0x143000000 (gw2 .data globals we've seen).
For each static holding a heap ptr: report static VA, target, and whether the
target contains a view ptr (holder) or IS a view (proj sig at +0x360)."""
import struct
import numpy as np

BASE = 0x140000000
img = open(r"C:/Users/Public/gw2_dump_image.bin", "rb").read()
N = len(img)

heap = open(r"C:/Users/Public/gw2_dump_heap.bin", "rb")

segs = []
for ln in open(r"C:/Users/Public/gw2_dump_heap_manifest.txt"):
    ln = ln.strip()
    if not ln or ln.startswith("#"):
        continue
    fo, va, sz = ln.split(",")
    segs.append((int(va, 16), int(sz, 16), int(fo, 16)))
segs.sort()
seg_vas = [s[0] for s in segs]
from bisect import bisect_right

def heap_fo(va):
    i = bisect_right(seg_vas, va) - 1
    if i < 0:
        return None
    sva, ssz, sfo = segs[i]
    if sva <= va < sva + ssz:
        return sfo + (va - sva)
    return None

def is_view(va):
    fo = heap_fo(va)
    if fo is None:
        return False
    heap.seek(fo + 0x360)
    pj = heap.read(64)
    if len(pj) < 64:
        return False
    f = struct.unpack("<16f", pj)
    return (abs(f[0] - 1.0711) < 0.03 and abs(f[5] - 1.9042) < 0.03 and
            abs(f[11] + 1) < 0.02 and abs(f[14] - 0.1) < 0.02)

def looks_holder(va):
    """Scan target's first 0x1000 bytes for a view pointer (any of our 103 views
    is too slow; instead check for the proj sig of a view at target+k for k in
    0..0x200 step 4 is heavy. Simplified: check if target+0..0x400 contains a
    qword that lands in a heap seg AND that qword's target has proj sig... too
    deep. Just report statics; classify later.)"""
    return True

# pass: scan image .data zone for heap pointers
ZONES = [(0x142000000, 0x143000000), (0x143000000, BASE + N)]
seen = set()
results = []
for lo, hi in ZONES:
    lo = max(lo, BASE); hi = min(hi, BASE + N)
    if lo >= hi:
        continue
    buf = img[lo - BASE:hi - BASE]
    arr = np.frombuffer(buf[:len(buf)//8*8], dtype="<u8")
    # heap pointers: below 0x142000000 and above 0x10000 (heap VAs seen: 0x1B-0x7B...)
    mask = (arr > 0x100000) & (arr < 0x140000000)
    idx = np.nonzero(mask)[0]
    print(f"zone {lo:#x}-{hi:#x}: {len(idx)} heap-pointer statics")
    cnt = 0
    for i in idx:
        site = lo + int(i)*8
        val = int(arr[i])
        r = (site, val)
        if val in seen:
            results.append(r); continue
        if is_view(val):
            seen.add(val)
            results.append(r)
            cnt += 1
            if cnt >= 5:
                break
    print(f"  ...{cnt} point directly at view objects in first sample")

# full classification of all statics
print("\nfull classification pass...")
view_statics = []
holder_statics = []
for i in range(0, 0):  # placeholder, real loop below
    pass

all_sites = []
for lo, hi in ZONES:
    lo = max(lo, BASE); hi = min(hi, BASE + N)
    if lo >= hi:
        continue
    buf = img[lo - BASE:hi - BASE]
    arr = np.frombuffer(buf[:len(buf)//8*8], dtype="<u8")
    mask = (arr > 0x100000) & (arr < 0x140000000)
    for i in np.nonzero(mask)[0]:
        all_sites.append((lo + int(i)*8, int(arr[i])))
print(f"total heap-pointer statics: {len(all_sites)}")

for site, val in all_sites:
    if is_view(val):
        view_statics.append((site, val))

print(f"\nSTATICS POINTING DIRECTLY AT VIEW OBJECTS: {len(view_statics)}")
for site, val in view_statics:
    print(f"  .data [{site:#x}] -> view {val:#x}")
