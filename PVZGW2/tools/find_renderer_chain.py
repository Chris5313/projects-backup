#!/usr/bin/env python3
"""Find GW2's GameRenderer-equivalent static by reverse pointer-walking.

Spankerfield/BF4: GameRenderer static -> +0x60 -> RenderView -> VP @0x460.
GW2's runtime chains died, so find the REAL chain from data:

  1. Locate live view objects in the process dump (proj sig @ view+0x360).
  2. Find every qword pointing to a view  (holders).
  3. Find every qword pointing to a holder (level 2, 3...).
  4. Any hit whose ADDRESS lives in the image's .data range (0x142000000+)
     is a STATIC ROOT  ==  GameRenderer-equivalent.

Output: candidate roots with (static -> off -> holder -> off -> view) chains.
"""
import struct
import numpy as np
from bisect import bisect_right

BASE = 0x140000000
IMG_END = BASE + 0x4000000          # 64MB image ballpark (image bin is 60MB)

segs = []                            # (va, size, fo) sorted by va
for ln in open(r"C:/Users/Public/gw2_dump_heap_manifest.txt"):
    ln = ln.strip()
    if not ln or ln.startswith("#"):
        continue
    fo, va, sz = ln.split(",")
    segs.append((int(va, 16), int(sz, 16), int(fo, 16)))
segs.sort()
seg_vas = [s[0] for s in segs]
print(f"segments: {len(segs)}")

def va_to_fo(va):
    i = bisect_right(seg_vas, va) - 1
    if i < 0:
        return None
    sva, ssz, sfo = segs[i]
    if sva <= va < sva + ssz:
        return sfo + (va - sva)
    return None

def in_image(va):
    return BASE <= va < IMG_END

heap = open(r"C:/Users/Public/gw2_dump_heap.bin", "rb")
TOTAL = sum(s[1] for s in segs)
print(f"total mapped: {TOTAL/1e9:.2f} GB")

# ---- pass 1: find view objects -------------------------------------------
# reuse the proven signature: f32 @ view+0x360 == 1.0711, full block check
CHUNK = 1 << 26
views = set()
view_list = []
for sva, ssz, sfo in segs:
    if in_image(sva) and sva + ssz <= IMG_END:
        continue                    # views live in the heap, not the image
    off = 0
    while off < ssz:
        n = min(CHUNK, ssz - off)
        heap.seek(sfo + off)
        buf = heap.read(n)
        if not buf:
            break
        arr = np.frombuffer(buf[:len(buf)//4*4], dtype="<f4")
        cand = np.nonzero(np.abs(arr - 1.0711) < 0.03)[0]
        for idx in cand:
            i4 = int(idx)*4
            if i4 + 0x400 > len(buf):
                continue
            f5 = struct.unpack_from("<f", buf, i4 + 0x360 - 0x360 + 20)[0]  # +0x374
            if not (1.9042 - 0.03 < f5 < 1.9042 + 0.03):
                continue
            pj = struct.unpack_from("<16f", buf, i4)
            if not (abs(pj[11] + 1) < 0.02 and abs(pj[14] - 0.1) < 0.02):
                continue
            view = sva + off + i4 - 0x360
            if view > 0x100000 and view not in views:
                views.add(view)
                view_list.append(view)
        off += n
print(f"views found: {len(view_list)}")
for v in view_list[:30]:
    print(f"  view @ {v:#x}")

if not view_list:
    raise SystemExit("no views — cannot walk")

# ---- reverse pointer passes ----------------------------------------------
def find_pointers_to(targets, label):
    """One pass over all mapped memory: qword values in `targets` set.
    Returns list of (site_va, value)."""
    tset = set(targets)
    hits = []
    for sva, ssz, sfo in segs:
        off = 0
        while off < ssz:
            n = min(CHUNK, ssz - off)
            heap.seek(sfo + off)
            buf = heap.read(n - (n % 8))
            if not buf:
                break
            arr = np.frombuffer(buf, dtype="<u8")
            # candidate mask against the target set (small -> loop values)
            for t in tset:
                idx = np.nonzero(arr == t)[0]
                for i in idx:
                    hits.append((sva + off + int(i)*8, t))
            off += n
    print(f"{label}: {len(hits)} sites")
    return hits

lv1 = find_pointers_to(view_list, "ptrs->view")
# dedupe + keep heap/image info
lv1_sites = sorted(set(h[0] for h in lv1))
print(f"distinct holders: {len(lv1_sites)}")
for s in lv1_sites[:20]:
    tag = "IMG" if in_image(s) else "heap"
    print(f"  {tag} {s:#x} -> view")

# level 2: who points to the holders
lv2 = find_pointers_to(lv1_sites, "ptrs->holder")
lv2_sites = sorted(set(h[0] for h in lv2))
img_roots = [s for s in lv2_sites if in_image(s)]
print(f"level2 sites: {len(lv2_sites)}, IMAGE RESIDENT: {len(img_roots)}")
for s in img_roots[:20]:
    print(f"  STATIC ROOT @ {s:#x}")

# level 3 if needed
if not img_roots:
    lv3 = find_pointers_to(lv2_sites, "ptrs->lv2")
    lv3_sites = sorted(set(h[0] for h in lv3))
    img_roots = [s for s in lv3_sites if in_image(s)]
    print(f"level3 IMAGE RESIDENT: {len(img_roots)}")
    for s in img_roots[:20]:
        print(f"  STATIC ROOT @ {s:#x}")

# ---- reconstruct chains ----------------------------------------------------
print("\n== candidate chains (static -> holder -> view) ==")
# map: site -> value it holds
val_at = dict(lv1)
val2 = dict(lv2)
for root in img_roots[:20]:
    v1 = val2.get(root)
    if v1 is None:
        continue
    off1 = None
    # find which lv1 site lives at v1... holder base unknown; lv1 site IS the
    # slot containing the view ptr. holder base = v1 (a pointer to the holder
    # object base would equal v1 only if slot is at +0). Report raw chain.
    # find lv1 site whose address is in [v1, v1+0x2000)
    cands = [s for s in lv1_sites if v1 <= s < v1 + 0x2000]
    for s in cands[:4]:
        vw = val_at.get(s)
        if vw in views:
            print(f"  static {root:#x} -> holder {v1:#x} (slot +{s-v1:#x}) -> view {vw:#x}")
