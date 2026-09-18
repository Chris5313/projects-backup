#!/usr/bin/env python3
"""Motion analysis: find the real position offset for entity/proxy structs.

Uses gw2_heapdump_2/3/4.bin (12:55, 13:01, 13:06 — same session) + heapmaps.
The 6 entity VAs from gw2_backpointers.txt are followed in each dump:
  1. verify CharObj vtable, hexdump the struct
  2. follow +0x50 (CO_PROXY) and report what's actually there
  3. scan struct + proxy for float triplets that CHANGE across dumps
     (finite, plausible world coords, not bit-frozen) -> the real pos offset
"""
import mmap
import re
import struct
import sys

PUB = r"C:/Users/Public"
DUMPS = {2: (PUB + r"/gw2_heapdump_2.bin", PUB + r"/gw2_heapmap_2.txt"),
         3: (PUB + r"/gw2_heapdump_3.bin", PUB + r"/gw2_heapmap_3.txt"),
         4: (PUB + r"/gw2_heapdump_4.bin", PUB + r"/gw2_heapmap_4.txt")}
ENTITIES = [0x8682F020, 0x86834D20, 0x88511D10, 0x88E77D70, 0x88EF49E0, 0x83C36E40]
IMG = 0x140000000
VT_CHAROBJ = 0x228B380


def load_map(path):
    regions = []
    with open(path) as f:
        for ln in f:
            m = re.match(r"DUMP\s+(0x[0-9A-Fa-f]+)\s+(0x[0-9A-Fa-f]+)\s+(0x[0-9A-Fa-f]+)", ln)
            if m:
                regions.append((int(m.group(1), 16), int(m.group(2), 16), int(m.group(3), 16)))
    regions.sort()
    return regions


class Blob:
    def __init__(self, path, regions):
        self.f = open(path, "rb")
        self.mm = mmap.mmap(self.f.fileno(), 0, access=mmap.ACCESS_READ)
        self.regions = regions

    def resolve(self, va, n):
        for rva, size, off in self.regions:
            if rva <= va and va + n <= rva + size:
                return off + (va - rva)
        return None

    def read(self, va, n):
        o = self.resolve(va, n)
        return None if o is None else bytes(self.mm[o:o + n])

    def qword(self, va):
        d = self.read(va, 8)
        return None if d is None else struct.unpack("<Q", d)[0]


def hexdump(d, base=0):
    if d is None:
        return "  <unmapped>"
    out = []
    for o in range(0, len(d), 16):
        c = d[o:o + 16]
        out.append(f"  +0x{o + base:03X}: " + " ".join(f"{b:02X}" for b in c))
    return "\n".join(out)


def finite(v):
    return v == v and abs(v) < 1e9


def plausible(p):
    x, y, z = p
    if not all(map(finite, p)):
        return False
    return abs(x) < 5000 and -100 < y < 1000 and abs(z) < 5000


def motion_scan(blobs, va, span, label):
    """Print offsets whose float triplet is plausible AND changes across dumps."""
    snaps = []
    for b in blobs:
        d = b.read(va, span)
        snaps.append(d)
    print(f"\n  -- motion scan ({label} @ 0x{va:X}, {span}B) --")
    found = 0
    for off in range(0, span - 12, 4):
        vals = []
        ok = True
        for d in snaps:
            if d is None or len(d) < off + 12:
                ok = False
                break
            vals.append(struct.unpack("<3f", d[off:off + 12]))
        if not ok or not all(plausible(v) for v in vals):
            continue
        changed = any(vals[i] != vals[i + 1] for i in range(len(vals) - 1))
        if changed:
            found += 1
            seq = " -> ".join(f"({v[0]:.1f},{v[1]:.1f},{v[2]:.1f})" for v in vals)
            print(f"    +0x{off:03X}: {seq}")
    if not found:
        print("    (no changing plausible triplet)")


def main():
    blobs = []
    for idx in (2, 3, 4):
        path, mapp = DUMPS[idx]
        blobs.append(Blob(path, load_map(mapp)))
        print(f"dump {idx} loaded")

    for eva in ENTITIES:
        print(f"\n===== ENTITY 0x{eva:X} =====")
        # 1. verify charobj vtable in each dump
        for i, b in enumerate(blobs):
            vt = b.qword(eva)
            rva = (vt - IMG) if vt else -1
            print(f"  d{idx_v(i)}: vt=0x{vt:X} rva=0x{rva:X} {'OK' if rva == VT_CHAROBJ else '??'}")
        # 2. hexdump charObj head from dump4
        print("  charObj[0x00..0x80] (d4):")
        print(hexdump(blobs[2].read(eva, 0x80), 0))
        # 3. follow +0x50 in each dump
        proxies = []
        for i, b in enumerate(blobs):
            pv = b.qword(eva + 0x50)
            proxies.append(pv)
            print(f"  d{idx_v(i)}: +0x50 -> 0x{pv:X}" if pv else f"  d{idx_v(i)}: +0x50 -> <none>")
        if len(set(proxies)) == 1 and proxies[0]:
            print("  proxy struct (d4, 0x100B):")
            print(hexdump(blobs[2].read(proxies[0], 0x100), 0))
            motion_scan(blobs, proxies[0], 0x100, "proxy")
        else:
            print(f"  proxy VA differs across dumps: {[hex(p) if p else None for p in proxies]}")
        # 4. also scan the charObj itself (position may be embedded)
        motion_scan(blobs, eva, 0x200, "charObj")

    for b in blobs:
        b.mm.close()


def idx_v(i):
    return i + 2


if __name__ == "__main__":
    main()
