#!/usr/bin/env python3
"""Walk the full Frostbite ClassInfo chain across image + heap dumps.

The image-only walk stopped at 62 nodes because ClassInfo objects mostly live
in the heap (operator-new'd). Stitch gw2_dump_image.bin (file off == RVA from
0x140000000) with gw2_dump_heap.bin via gw2_dump_heap_manifest.txt
(file_offset,va,size) into one sparse reader, then follow ->next from each
image candidate and score the chains.
"""
import re, struct, bisect, sys

BASE = 0x140000000

# ---- sparse address space ---------------------------------------------------
segs = []   # (va_lo, va_hi, file, off)
def add_seg(va, size, file, off):
    segs.append((va, va + size, file, off))

img = r"C:/Users/Public/gw2_dump_image.bin"
add_seg(BASE, 60817408, img, 0)

man = open(r"C:/Users/Public/gw2_dump_heap_manifest.txt")
heap = open(r"C:/Users/Public/gw2_dump_heap.bin", "rb")
for ln in man:
    ln = ln.strip()
    if not ln or ln.startswith("#"): continue
    fo, va, sz = ln.split(",")
    add_seg(int(va, 16), int(sz, 16), "HEAP", int(fo, 16))
segs.sort()
starts = [s[0] for s in segs]
print(f"segments: {len(segs)}")

_fh = {}
def fh(path):
    if path not in _fh:
        _fh[path] = open(path, "rb")
    return _fh[path]

def read(va, n):
    i = bisect.bisect_right(starts, va) - 1
    if i < 0: return None
    lo, hi, file, off = segs[i]
    d = va - lo
    if d < 0 or d + n > hi - lo: return None
    if file == "HEAP":
        heap.seek(off + d)
        return heap.read(n)
    f = fh(file)
    f.seek(off + d)
    return f.read(n)

def rd64(va):
    b = read(va, 8)
    return struct.unpack("<Q", b)[0] if b and len(b) == 8 else None

def cstr(va, maxlen=64):
    b = read(va, maxlen)
    if not b: return None
    z = b.find(b"\x00")
    if z <= 0: return None
    s = b[:z].decode(errors="replace")
    return s if re.fullmatch(r"[\x20-\x7e]{2,63}", s) else None

# ---- find chain heads in image ----------------------------------------------
data = open(img, "rb").read()
N = len(data)
strs = {}
for m in re.finditer(rb"[\x20-\x7e]{5,64}\x00", data[0x1C00000:]):
    strs[0x1C00000 + m.start()] = m.group()[:-1].decode()

heads = []
for off in range(0x100000, N - 8, 8):
    v = struct.unpack_from("<Q", data, off)[0]
    fo = v - BASE
    if fo in strs:
        heads.append(off)
print(f"image name-ptr windows: {len(heads)}")

def walk(head_va, limit=200000):
    seen = set()
    good = 0
    names = []
    interesting = {}
    va = head_va
    while len(seen) < limit:
        if va in seen or va is None: break
        seen.add(va)
        nv = rd64(va)
        nfo = (nv - BASE) if nv else None
        s = None
        if nfo in strs:
            s = strs[nfo]
        else:
            s = cstr(nv) if nv else None
        if s:
            good += 1
            if len(names) < 15: names.append(s)
            if s in ("RenderView", "ScreenComponent", "CameraManager", "ClientPlayerManager",
                     "RenderCamera", "CameraEntity", "TypeInfo", "DataContainer", "GameView",
                     "RenderViewData", "AimAssistTargetManager"):
                interesting[s] = va
        nxt = rd64(va + 8)
        if nxt is None or (nxt & 7): break
        va = nxt
    return good, len(seen), names, interesting

best = (0, 0, [], {})
bh = None
for off in heads:
    g, n, nm, it = walk(BASE + off)
    if n > best[1]:
        best = (g, n, nm, it); bh = BASE + off
g, n, nm, it = best
print(f"BEST head=0x{bh:X} visited={n} named={g}")
print("sample names:", nm)
print("interesting:", {k: hex(v) for k, v in it.items()})
