#!/usr/bin/env python3
"""Map the memory around heap VA 0x68D10 directly (VA range -> file offset)."""
import struct, bisect

segs = []
man = open(r"C:/Users/Public/gw2_dump_heap_manifest.txt")
for ln in man:
    ln = ln.strip()
    if not ln or ln.startswith("#"): continue
    fo, va, sz = ln.split(",")
    segs.append((int(va,16), int(sz,16), int(fo,16)))  # manifest: file_offset,va,size
    # NOTE: columns are file_offset,va,size — so va is column 1, fileoff col 0
    # fix: real mapping below
segs.sort()
starts = [s[0] for s in segs]
heap = open(r"C:/Users/Public/gw2_dump_heap.bin","rb")

def read(va, n):
    i = bisect.bisect_right(starts, va) - 1
    if i < 0: return None
    va0, size, off = segs[i]
    d = va - va0
    if d < 0 or d+n > size: return None
    heap.seek(off+d); return heap.read(n)

def floats(va, cnt):
    b = read(va, cnt*4)
    return struct.unpack(f"<{cnt}f", b) if b else None

PROJ = 0x68D10
# which segment holds it?
i = bisect.bisect_right(starts, PROJ) - 1
print("segment:", [hex(x) for x in segs[i]])
# dump a wide window BEFORE and AFTER the proj
for va in range(PROJ - 0x400, PROJ + 0x200, 0x40):
    f = floats(va, 16)
    if f is None:
        print(f"{va:#07x}: <gap>")
        continue
    r1 = " ".join(f"{v:8.3f}" for v in f[:8])
    r2 = " ".join(f"{v:8.3f}" for v in f[8:])
    print(f"{va:#07x}: {r1}")
    print(f"          {r2}")
