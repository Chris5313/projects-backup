#!/usr/bin/env python3
"""BF4-style entity array hunt (fast: 2 disk passes, rest numpy).

Frostbite (BF4/spankerfield) enumerates players from a fixed array:
  GameContext -> PlayerManager -> player pointers (stride 8)
GW2's equivalent = a contiguous array of qwords that all point at objects
whose vtable == CharObj vtable (0x14228B380, dump-verified).

Pass 1: sweep heap, record every 8-aligned slot whose qword is heap-range.
Pass 2: batched-by-segment reads check which targets carry the CharObj
        vtable -> sorted array of confirmed entity pointers.
Pass 3 (no disk): runs of CONSECUTIVE confirmed slots in each segment = arrays.
Pass 4: bounded owner hunt (qwords == array start) in heap + image.
"""
import struct
import numpy as np

PUB = r"C:/Users/Public"
VT = 0x14228B380
HEAP_LO, HEAP_HI = 0x1000000, 0xB0000000

segs = []
for line in open(PUB + "/gw2_dump_heap_manifest.txt"):
    line = line.strip()
    if not line or line.startswith("#"):
        continue
    parts = line.replace(",", " ").split()
    try:
        vals = [int(p, 16) for p in parts if p]
    except ValueError:
        continue
    if len(vals) >= 3:
        segs.append((vals[1], vals[0], vals[2]))  # (va, file_off, size)
segs.sort()
print(f"manifest segments: {len(segs)}, total {sum(s[2] for s in segs)/2**30:.2f} GB", flush=True)

# ---- pass 1 + 3: per segment, one read; candidate slots + run detection ----
# (run detection needs per-segment slot flags, so it happens here too)
runs = []
for sva, soff, ssize in segs:
    if ssize < 0x1000:
        continue
    with open(PUB + "/gw2_dump_heap.bin", "rb") as f:
        f.seek(soff)
        data = f.read(ssize)
    if len(data) < 16:
        del data
        continue
    n = len(data) // 8
    q = np.frombuffer(data, dtype="<u8", count=n)
    cand = (q >= HEAP_LO) & (q <= HEAP_HI)
    del data, q
    # pass 3 must wait for confirmation, but runs are within-segment — store
    # candidate positions for later flagging (memory-light: uint32 offsets)
    pos = np.nonzero(cand)[0].astype(np.uint64)
    del cand
    # pass 2 for this segment's candidates: batched vtable read
    if len(pos) == 0:
        continue
    targets = np.uint64(sva) + pos * np.uint64(8)
    # group targets by containing segment (they can cross segments)
    tseg = np.searchsorted(np.array([s[0] for s in segs], dtype=np.uint64), targets, side="right") - 1
    good_local = np.zeros(len(pos), dtype=bool)
    for tsi in np.unique(tseg):
        m = tseg == tsi
        tva, toff, tsz = segs[int(tsi)]
        tt = targets[m]
        lo, hi = int(tt.min()), int(tt.max()) + 8
        if lo < tva or hi > tva + tsz:
            # targets not fully inside this segment: fall back per-element
            for k, v in enumerate(tt):
                d = None
                if tva <= v and v + 8 <= tva + tsz:
                    with open(PUB + "/gw2_dump_heap.bin", "rb") as f:
                        f.seek(toff + int(v) - tva)
                        d = f.read(8)
                good_local[np.nonzero(m)[0][k]] = d is not None and len(d) == 8 and struct.unpack("<Q", d)[0] == VT
            continue
        with open(PUB + "/gw2_dump_heap.bin", "rb") as f:
            f.seek(toff + lo - tva)
            blob = f.read(hi - lo)
        if len(blob) != hi - lo:
            continue
        vtq = np.frombuffer(blob, dtype="<u8", count=(hi - lo) // 8)[(tt - np.uint64(lo)).astype(np.int64) // 8]
        good_local[np.nonzero(m)[0]] = vtq == np.uint64(VT)
        del blob
    # runs of consecutive good slots within this segment
    g = good_local
    if g.any():
        d = np.diff(g.astype(np.int8))
        starts = np.nonzero((g[1:] & (d == 1)))[0] + 1
        if g[0]:
            starts = np.concatenate(([0], starts))
        ends = np.nonzero((g[:-1] & (d == -1)))[0] + 1
        if g[-1]:
            ends = np.concatenate((ends, [len(g)]))
        for s, e in zip(starts, ends):
            if e - s >= 3:
                runs.append((int(sva + int(s) * 8), int(e - s)))
    print(f"  seg 0x{sva:X} done ({len(pos)} cands, {int(g.sum())} charobjs)", flush=True)

print(f"\nruns >=3: {len(runs)}", flush=True)
for va, cnt in sorted(runs, key=lambda r: -r[1])[:15]:
    print(f"  array @ 0x{va:X} count={cnt}", flush=True)

# ---- pass 4: bounded owner hunt ---------------------------------------------
def owner_hunt(array_va):
    pat = struct.pack("<Q", array_va)
    out = []
    for sva, soff, ssize in segs:
        if ssize < 0x1000:
            continue
        with open(PUB + "/gw2_dump_heap.bin", "rb") as f:
            f.seek(soff)
            data = f.read(ssize)
        i = data.find(pat)
        while i != -1:
            if i % 8 == 0:
                out.append((sva + i, "heap"))
            i = data.find(pat, i + 1)
        del data
    img = open(PUB + "/gw2_dump_image.bin", "rb").read()
    i = img.find(pat)
    while i != -1:
        if i % 8 == 0:
            out.append((0x140000000 + i, "image"))
        i = img.find(pat, i + 1)
    return out

top = sorted(runs, key=lambda r: -r[1])[:6]
for va, cnt in top:
    owners = owner_hunt(va)
    print(f"\narray 0x{va:X} (n={cnt}): {len(owners)} owner refs", flush=True)
    for ova, kind in owners[:4]:
        if kind == "heap":
            # neighborhood via segment lookup
            seg = None
            for sva, soff, ssize in segs:
                if sva <= ova - 0x40 and ova + 0x80 <= sva + ssize:
                    seg = (sva, soff, ssize)
                    break
            if seg:
                with open(PUB + "/gw2_dump_heap.bin", "rb") as f:
                    f.seek(seg[1] + (ova - 0x40 - seg[0]))
                    nb = f.read(0xC0)
                row = " ".join(
                    f"+0x{k:X}:{struct.unpack_from('<Q', nb, k)[0]:X}"
                    for k in range(0x30, 0x60, 8)
                )
                print(f"   heap holder 0x{ova:X}  {row}")
                continue
        print(f"   {kind} holder 0x{ova:X}")
