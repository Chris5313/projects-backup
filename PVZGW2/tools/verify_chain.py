#!/usr/bin/env python3
"""Verify the fresh-dump chain against raw bytes:
  CAM_STATIC 0x142CEE730 -> camMgr 0x320C4300
  camMgr+0x68 -> views array 0x513B3DF0 -> RenderView[0] (0x520 bytes)
Extract the FULL view bytes, compute VP = P * W2V from the verified proj,
then locate the exact precomputed-VP offset inside the view.
"""
import struct, sys
import numpy as np

PUB = r"C:/Users/Public"
img = open(PUB + "/gw2_dump_image.bin", "rb").read()          # image @ 0x140000000
IMG_BASE = 0x140000000

# ---- manifest: segment list [(va, file_offset, size)] ----
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
        segs.append((vals[1], vals[0], vals[2]))   # (va, file_off, size) — manifest is file_offset,va,size
print(f"manifest segments: {len(segs)}")

def rd_va(va, n):
    """read n bytes at virtual address va from heap dump (or image)."""
    for sva, soff, ssize in segs:
        if sva <= va and va + n <= sva + ssize:
            f = open(PUB + "/gw2_dump_heap.bin", "rb")
            f.seek(soff + (va - sva))
            d = f.read(n)
            f.close()
            return d
    return None

def qword(va):
    d = rd_va(va, 8)
    return struct.unpack("<Q", d)[0] if d else None

def floats(va, n16):
    d = rd_va(va, n16 * 4)
    return list(struct.unpack("<" + "f" * n16, d)) if d else None

# ---- 1. the chain ----
static_img_off = 0x142CEE730 - IMG_BASE
static_val_img = struct.unpack("<Q", img[static_img_off:static_img_off + 8])[0]
print(f"CAM_STATIC value in IMAGE .data : {static_val_img:X}")

camMgr = qword(0x320C4300)  # sanity: readable?
print(f"camMgr @0x320C4300 readable, first qword = {camMgr:X}" if camMgr is not None else "camMgr NOT in dump!")

for off in (0x60, 0x68, 0x70, 0x78):
    v = qword(0x320C4300 + off)
    print(f"camMgr+0x{off:02X} = {v:X}" if v is not None else f"camMgr+0x{off:02X} unreadable")

viewsArr = qword(0x320C4300 + 0x68)
print(f"\nviews array base = {viewsArr:X}")

# ---- 2. full RenderView[0] ----
V = rd_va(viewsArr, 0x520)
if V is None:
    print("!! view bytes not covered by dump"); sys.exit(1)
f = np.frombuffer(V, dtype="<f4")
print(f"view bytes extracted: 0x520")

def show(off16, label):
    m = f[off16:off16 + 16].reshape(4, 4)
    print(f"  {label} @+0x{off16*4:03X}:")
    for r in m:
        print("   ", " ".join(f"{x:10.4f}" for x in r))

# known: eye (272.3, 64.1, -128.1) — find every place it appears
eye = np.array([272.3028, 64.1490, -128.0671], dtype=np.float32)
for i in range(0, 0x520 // 4 - 3):
    if abs(f[i] - eye[0]) < 0.5 and abs(f[i+1] - eye[1]) < 0.5 and abs(f[i+2] - eye[2]) < 0.5:
        print(f"  eye match at view+0x{i*4:03X} (elem {i})")

# ---- 3. build expected VP from verified proj @ +0x360 ----
P = f[0x360//4 : 0x360//4 + 16].reshape(4, 4).astype(np.float64)  # col-major array = math col-major
print("\nproj @+0x360 (as stored, 4x4):")
print(P)

first = V[:64]
import struct as _s
print("view[0..64] as floats:", [round(x,4) for x in _s.unpack('<16f', first)])

# camera basis rows from the +0x000 world matrix; W2V (col-vector convention):
# rows = basis rows, translation col = -dot(basis_row, eye)
R = f[0:12].reshape(3, 4)[:, :3].astype(np.float64)   # basis rows
W2V = np.zeros((4, 4))
W2V[:3, :3] = R
W2V[:3, 3] = -R @ eye.astype(np.float64)
W2V[3, 3] = 1.0
VP = P @ W2V      # col-major col-vector: clip = VP * (x,y,z,1)
print("\nexpected VP = P @ W2V (col-vector):")
print(VP)

# search view bytes for the 16 VP floats in several layouts
targets = {
    "colmajor-as-stored": VP.T.reshape(-1),          # store col-major -> memory order = VP columns
    "rowmajor": VP.reshape(-1),
    "colmajor-transposed": VP.reshape(-1).T.reshape(-1) if False else VP.T.flatten(),
}
vp16 = VP.flatten()  # row-major flatten of math matrix
cands = {}
for name, arr in (("VP-rows", VP), ("VP-cols", VP.T)):
    a = arr.astype(np.float32).flatten()
    for off in range(0, 0x520 - 64, 4):
        blk = f[off//4 : off//4 + 16]
        if np.all(np.abs(blk - a) < 2e-2):
            print(f"\n*** EXACT VP MATCH ({name}) at view+0x{off:03X} ***")
            cands[off] = name
if not cands:
    print("\nno exact 16-float match — searching row-by-row:")
    a = VP.astype(np.float32)
    for r in range(4):
        row = a[r] if True else None
        for off in range(0, 0x520 - 16, 4):
            blk = f[off//4 : off//4 + 4]
            if np.all(np.abs(blk - a[r]) < 2e-2):
                print(f"  VP row {r} found at view+0x{off:03X}: {blk}")
        col = a[:, r]
        for off in range(0, 0x520 - 16, 4):
            blk = f[off//4 : off//4 + 4]
            if np.all(np.abs(blk - col) < 2e-2):
                print(f"  VP col {r} found at view+0x{off:03X}: {blk}")

# ---- 4. w-row annihilation test for every 64-byte block (which offsets are VPs?) ----
print("\nw-row annihilation scan (w@eye ~ 0, scales sane):")
for off in range(0, 0x520 - 64, 4):
    b = f[off//4 : off//4 + 16].astype(np.float64)
    wrow = b[3::4]  # b[3],b[7],b[11],b[15]
    w = eye.astype(np.float64) @ wrow[:3] + wrow[3]
    s0, s1 = abs(b[0]), abs(b[5])
    if abs(w) < 1.0 and 0.05 < s0 < 8 and 0.05 < s1 < 8:
        print(f"  +0x{off:03X}: w@eye={w:+.4f} b00={b[0]:.4f} b11={b[5]:.4f}")
