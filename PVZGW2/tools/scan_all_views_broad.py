#!/usr/bin/env python3
"""Find ALL real view objects (proj@+0x360 == cam proj signature) and pointers
to them. Fast scan: precheck on f[0]≈1.0711 via struct scan with step 4."""
import struct

segs = []
for ln in open(r"C:/Users/Public/gw2_dump_heap_manifest.txt"):
    ln = ln.strip()
    if not ln or ln.startswith("#"): continue
    fo, va, sz = ln.split(",")
    segs.append((int(va,16), int(sz,16), int(fo,16)))
by_fo = sorted(segs, key=lambda s: s[2])
heap = open(r"C:/Users/Public/gw2_dump_heap.bin","rb")

views = []
pack = struct.Struct("<f")
for va, size, off in by_fo:
    heap.seek(off)
    CH = 1 << 23
    base = 0
    while base < size:
        buf = heap.read(min(CH, size - base))
        if not buf: break
        n = len(buf)
        # precheck: find bytes of float 1.0711 (0x3F89...) quickly
        # 1.0711f = 0x3F89 1D70 approx; scan for any float in [1.05,1.09]
        i = 0
        L = n - 64
        while i < L:
            # quick reject: check 4 bytes as uint, exponent range
            u = struct.unpack_from("<I", buf, i)[0]
            # float 1.0711 -> 0x3F891D70; range check exponent bits
            if 0x3F800000 <= u <= 0x3FA00000:
                f0 = struct.unpack_from("<f", buf, i)[0]
                if 1.05 < f0 < 1.09:
                    f = struct.unpack_from("<16f", buf, i)
                    if (abs(f[5]-1.9042) < 0.02 and abs(f[11]+1) < 0.02
                            and abs(f[14]-0.1) < 0.02 and f[1] == 0 and f[4] == 0):
                        views.append(va + base + i - 0x360)
                        if len(views) > 60: break
            i += 4
        base += CH
    if len(views) > 60: break

views = sorted(set(views))
print(f"views: {len(views)}")
for v in views[:30]:
    print(f"  {hex(v)}")
