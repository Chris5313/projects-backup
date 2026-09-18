#!/usr/bin/env python3
"""Dump the REAL proxy structs (charObj+0x50) and state structs (charObj+0x58)
from heapdump 4, scanning for plausible position triplets."""
import mmap
import re
import struct

PUB = r"C:/Users/Public"
DUMP = PUB + r"/gw2_heapdump_4.bin"
MAPF = PUB + r"/gw2_heapmap_4.txt"
ENTITIES = [0x8682F020, 0x86834D20, 0x88511D10, 0x88E77D70, 0x88EF49E0, 0x83C36E40]
PROXIES = [0x98F54780, 0x98F548C0]
IMG = 0x140000000


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

    def read(self, va, n):
        for rva, size, off in self.regions:
            if rva <= va and va + n <= rva + size:
                o = off + (va - rva)
                return bytes(self.mm[o:o + n])
        return None

    def qword(self, va):
        d = self.read(va, 8)
        return None if d is None else struct.unpack("<Q", d)[0]


def hd(b, d, base=0):
    if d is None:
        return "  <unmapped>"
    out = []
    for o in range(0, len(d), 16):
        c = d[o:o + 16]
        fl = []
        for fo in range(0, min(len(c), 16), 4):
            if fo + 4 <= len(c):
                v = struct.unpack("<f", c[fo:fo + 4])[0]
                fl.append(f"{v:9.2f}" if abs(v) < 1e7 and v == v else "    ---  ")
        hx = " ".join(f"{x:02X}" for x in c)
        out.append(f"  +{o + base:03X}: {hx:<47} |" + " ".join(fl) + "|")
    return "\n".join(out)


def plausible(p):
    x, y, z = p
    if any(v != v or abs(v) > 1e9 for v in p):
        return False
    return abs(x) < 5000 and -100 < y < 1000 and abs(z) < 5000


b = Blob(DUMP, load_map(MAPF))

print("=== PROXY structs (charObj+0x50), dump4 ===")
for pv in PROXIES:
    d = b.read(pv, 0x120)
    print(f"\n-- proxy 0x{pv:X} --")
    print(hd(b, d))
    # scan whole struct for plausible triplets
    if d:
        hits = []
        for off in range(0, len(d) - 12, 4):
            t = struct.unpack("<3f", d[off:off + 12])
            if plausible(t):
                hits.append((off, t))
        print("  plausible triplets:", ", ".join(f"+0x{o:03X}({t[0]:.1f},{t[1]:.1f},{t[2]:.1f})" for o, t in hits[:20]) or "NONE")

print("\n=== STATE structs (charObj+0x58), dump4 ===")
for eva in ENTITIES[:3]:
    st = b.qword(eva + 0x58)
    print(f"\n-- entity 0x{eva:X} state=0x{st:X} if small-offset check --")
    if st and st > 0x10000:
        d = b.read(st, 0x120)
        print(hd(b, d))
        if d:
            hits = []
            for off in range(0, len(d) - 12, 4):
                t = struct.unpack("<3f", d[off:off + 12])
                if plausible(t):
                    hits.append((off, t))
            print("  plausible triplets:", ", ".join(f"+0x{o:03X}({t[0]:.1f},{t[1]:.1f},{t[2]:.1f})" for o, t in hits[:20]) or "NONE")

# Follow one proxy qword chain: maybe proxy+0x0 points at a body/motion state
print("\n=== proxy inner pointers (first 5 qwords of proxy 0x98F54780) ===")
for off in range(0, 0x28, 8):
    q = b.qword(0x98F54780 + off)
    if q and q > 0x10000:
        d = b.read(q, 0x80)
        print(f"\n-- proxy+0x{off:02X} -> 0x{q:X} --")
        print(hd(b, d))
