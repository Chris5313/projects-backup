#!/usr/bin/env python3
"""Backward-trace the chain to view 0x3C7F9B0.
Holder: heap 0x8653BA40 = obj+0x1C8 slot. Dump around the holder, find its
container start, then search for pointers to the container."""
import struct, bisect

segs = []
for ln in open(r"C:/Users/Public/gw2_dump_heap_manifest.txt"):
    ln = ln.strip()
    if not ln or ln.startswith("#"): continue
    fo, va, sz = ln.split(",")
    segs.append((int(va,16), int(sz,16), int(fo,16)))
segs.sort(); starts=[s[0] for s in segs]
heap = open(r"C:/Users/Public/gw2_dump_heap.bin","rb")
img = open(r"C:/Users/Public/gw2_dump_image.bin","rb")

def read(va, n):
    i = bisect.bisect_right(starts, va) - 1
    if i < 0: return None
    va0, size, off = segs[i]
    d = va - va0
    if d < 0 or d+n > size: return None
    heap.seek(off+d); return heap.read(n)

def rd64h(va):
    b = read(va, 8)
    return struct.unpack("<Q", b)[0] if b else None

HOLD = 0x8653BA40
# dump holder context
print(f"== context around holder {hex(HOLD)} ==")
for d in range(-0x40, 0x48, 8):
    v = rd64h(HOLD + d)
    print(f"  {hex(HOLD+d)}: {hex(v) if v is not None else '?'}")

# The holder is inside some object. Find its container: scan backward for a
# vtable pointer (image-range qword).
container = None
for back in range(0, 0x1000, 8):
    v = rd64h(HOLD - back)
    if v and 0x140000000 <= v < 0x143000000:
        container = HOLD - back
        print(f"\ncontainer candidate @ {hex(container)} (vtable {hex(v)}, -{back:#x} from holder)")
        break

# search image + heap for pointers to container
if container:
    sig = struct.pack("<Q", container)
    print(f"\n== who points at container {hex(container)}? ==")
    start = 0
    cnt = 0
    while True:
        i = img.find(sig, start)
        if i < 0: break
        print(f"  img VA {hex(BASE+i) if (BASE:=0x140000000) else ''}")
        cnt += 1; start = i + 1
        if cnt > 8: break
    hits = 0
    for va, size, off in sorted(segs, key=lambda s: s[2]):
        heap.seek(off)
        CH = 1 << 22
        base = 0
        while base < size:
            buf = heap.read(min(CH, size - base))
            if not buf: break
            j = 0
            while True:
                i = buf.find(sig, j)
                if i < 0: break
                if (base + i) % 8 == 0:
                    print(f"  heap VA {hex(va+base+i)}")
                    hits += 1
                    if hits > 8: break
                j = i + 1
            base += CH
        if hits > 8: break
