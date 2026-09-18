#!/usr/bin/env python3
"""Examine the block around heap VA 0x68D10 (proj 1.0711/1.9042 live values).

Print the 0x200 bytes before and after as floats + qwords; locate object start
(the proj writer wrote proj@+0x3E0 of the view; if this proj is at view+0x3E0,
view start = 0x68D10 - 0x3E0 = 0x68930. Check fresh@+0x144 = 0x68A74.)"""
import struct, bisect

segs = []
man = open(r"C:/Users/Public/gw2_dump_heap_manifest.txt")
for ln in man:
    ln = ln.strip()
    if not ln or ln.startswith("#"): continue
    fo, va, sz = ln.split(",")
    segs.append((int(va,16), int(sz,16), int(fo,16)))
starts = [s[0] for s in sorted(segs)]
heap = open(r"C:/Users/Public/gw2_dump_heap.bin","rb")
img = open(r"C:/Users/Public/gw2_dump_image.bin","rb")

def read(va, n):
    import bisect as b
    i = b.bisect_right(starts, va) - 1
    if i < 0: return None
    # need va->(fileoff) mapping: sorted segs by va
    ssegs = sorted(segs)
    va0, size, off = ssegs[i]
    d = va - va0
    if d < 0 or d+n > size: return None
    heap.seek(off+d); return heap.read(n)

def floats(va, cnt):
    b = read(va, cnt*4)
    return struct.unpack(f"<{cnt}f", b) if b else None

def u8(va):
    b = read(va, 1)
    return b[0] if b else None

PROJ = 0x68D10
view = PROJ - 0x3E0
print(f"proj @ {hex(PROJ)}, hypothesized view start @ {hex(view)}")
print(f"fresh @ view+0x144 = {u8(view + 0x144)}")
print(f"fresh @ view+0x320 = {u8(view + 0x320)} (alt)")
print()
# dump matrices at candidate offsets relative to view
for off in (0x0, 0x40, 0x80, 0xC0, 0x100, 0x140, 0x180, 0x1C0, 0x200,
            0x2A0, 0x2E0, 0x320, 0x360, 0x3A0, 0x3E0):
    f = floats(view + off, 16)
    if f:
        row = " ".join(f"{v:8.3f}" for v in f[:8])
        row2 = " ".join(f"{v:8.3f}" for v in f[8:])
        print(f"view+{off:#05x}: {row}")
        print(f"            {row2}")
