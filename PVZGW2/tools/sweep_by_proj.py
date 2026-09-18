#!/usr/bin/env python3
"""Enumerate every live view object by its proj block @ view+0x360.
Signature: [1.0711, 0, 0, 0,  0, 1.9042, 0, 0,  0, 0, 0, -1,  0, 0, 0.1, 0].
Then: fresh byte @ view+0x144, eye @ view+0x320+0x30 (cw[12..14])."""
import struct

segs = []
for ln in open(r"C:/Users/Public/gw2_dump_heap_manifest.txt"):
    ln = ln.strip()
    if not ln or ln.startswith("#"): continue
    fo, va, sz = ln.split(",")
    segs.append((int(va,16), int(sz,16), int(fo,16)))
by_fo = sorted(segs, key=lambda s: s[2])
heap = open(r"C:/Users/Public/gw2_dump_heap.bin","rb")

# exact 64-byte proj block
import struct as st
PROJ_SIG = st.pack("<16f", 1.0711, 0, 0, 0,  0, 1.9042, 0, 0,  0, 0, 0, -1,  0, 0, 0.1, 0)
# tolerance: rebuild sig from the actual dump values
def close_block(buf, i):
    f = st.unpack_from("<16f", buf, i)
    return (abs(f[0]-1.0711) < 0.01 and abs(f[5]-1.9042) < 0.01 and
            abs(f[11]+1) < 0.01 and abs(f[14]-0.1) < 0.01 and
            abs(f[1]) < 1e-5 and abs(f[4]) < 1e-5)

views = []
for va, size, off in by_fo:
    heap.seek(off)
    CH = 1 << 22
    base = 0
    while base < size:
        n = min(CH, size - base)
        buf = heap.read(n)
        if not buf: break
        # vectorized-ish scan: check f[0] first only
        mv = memoryview(buf).cast('f')  # not aligned-safe for odd offsets; use struct
        for i in range(0, len(buf) - 64, 4):
            f0 = st.unpack_from("<f", buf, i)[0]
            if abs(f0 - 1.0711) < 0.01:
                if close_block(buf, i):
                    views.append(va + base + i - 0x360)
        base += n
print(f"views found: {len(views)}", flush=True)
for v in sorted(set(views)):
    heap_v = v
    # read fresh + eye
    def rd(at, n):
        # local reader via segs
        import bisect
        ssegs = sorted(segs); starts=[s[0] for s in ssegs]
        i = bisect.bisect_right(starts, at) - 1
        if i < 0: return None
        va0, size, off = ssegs[i]
        d = at - va0
        if d < 0 or d+n > size: return None
        heap.seek(off+d); return heap.read(n)
    fb = rd(v + 0x144, 1)
    cw = rd(v + 0x320, 64)
    eye = None
    if cw:
        cwf = st.unpack("<16f", cw)
        eye = (cwf[12], cwf[13], cwf[14])
    print(f"view@{hex(v)} fresh={fb[0] if fb else None} eye={eye}")
