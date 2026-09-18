#!/usr/bin/env python3
"""Find the real position offset inside hknpCharacterProxy offline.

Reads the proxy VAs that gw2_viewproj logged (VP_TGT lines), maps them into
gw2_heapdump_4.bin via gw2_heapmap_4.txt, hexdumps the first 0x100 bytes of
each, and scores every 4-byte-aligned offset by how many proxies show a
plausible world-position triplet there (|x|,|z| < 5000, 0 < y < 500, all
finite). The offset that works across ALL proxies wins.
"""
import math
import re
import struct
import sys

PUB = r"C:/Users/Public"
LOG = PUB + r"/gw2_payload.log"
HEAP = {4: (PUB + r"/gw2_heapdump_4.bin", PUB + r"/gw2_heapmap_4.txt"),
        5: (PUB + r"/gw2_heapdump_5.bin", PUB + r"/gw2_heapmap_5.txt"),
        6: (PUB + r"/gw2_heapdump_6.bin", PUB + r"/gw2_heapmap_6.txt")}
WIN = 0x100            # bytes to examine per proxy
BAD = ("nan", "inf")


def load_map(path):
    regions = []
    with open(path) as f:
        for ln in f:
            m = re.match(r"DUMP\s+(0x[0-9A-Fa-f]+)\s+(0x[0-9A-Fa-f]+)\s+(0x[0-9A-Fa-f]+)", ln)
            if m:
                va, size, off = int(m.group(1), 16), int(m.group(2), 16), int(m.group(3), 16)
                regions.append((va, size, off))
    regions.sort()
    return regions


class Blob:
    def __init__(self, data, regions):
        self.data = data
        self.regions = regions

    def resolve(self, va, n):
        for rva, size, off in self.regions:
            if rva <= va and va + n <= rva + size:
                return off + (va - rva)
        return None

    def read(self, va, n):
        o = self.resolve(va, n)
        if o is None:
            return None
        return self.data[o:o + n]


def proxies_from_log():
    seen = []
    with open(LOG, errors="replace") as f:
        for ln in f:
            m = re.search(r"VP_TGT: added proxy=(0x[0-9A-Fa-f]+)", ln)
            if m:
                va = int(m.group(1), 16)
                if va not in seen:
                    seen.append(va)
    return seen


def plausible_triplet(p):
    x, y, z = p
    for v in (x, y, z):
        if v != v or math.isinf(v):          # NaN / inf
            return False
    return abs(x) < 5000 and 0 < y < 500 and abs(z) < 5000


def hexdump(blob, va):
    d = blob.read(va, WIN)
    if d is None:
        return None
    lines = []
    for o in range(0, len(d), 16):
        chunk = d[o:o + 16]
        hx = " ".join(f"{b:02X}" for b in chunk)
        asc = "".join(chr(b) if 32 <= b < 127 else "." for b in chunk)
        lines.append(f"  +0x{o:02X}: {hx:<47}  {asc}")
    return "\n".join(lines)


def main():
    proxies = proxies_from_log()
    print(f"proxies from log: {[hex(p) for p in proxies]}")
    if not proxies:
        sys.exit("no VP_TGT lines in log")

    for idx in (4, 5, 6):
        path, mapp = HEAP[idx]
        try:
            data = open(path, "rb").read(0x7FFFFFFF)
        except OSError:
            print(f"--- dump {idx}: unreadable, skip")
            continue
        regions = load_map(mapp)
        blob = Blob(data, regions)
        resolvable = [p for p in proxies if blob.resolve(p, WIN) is not None]
        print(f"--- dump {idx}: {len(resolvable)}/{len(proxies)} proxies resolvable")
        if len(resolvable) < 2:
            continue

        for p in resolvable:
            print(f"\n== proxy 0x{p:X} ==")
            print(hexdump(blob, p) or "  <unmapped>")

        # consensus scan
        print("\n== consensus (plausible triplets per offset) ==")
        hits = {}
        for off in range(0, WIN - 12, 4):
            n_ok = 0
            for p in resolvable:
                d = blob.read(p + off, 12)
                if d is None or len(d) < 12:
                    continue
                if plausible_triplet(struct.unpack("<3f", d)):
                    n_ok += 1
            if n_ok:
                hits[off] = n_ok
        best = sorted(hits.items(), key=lambda kv: -kv[1])[:12]
        for off, n_ok in best:
            sample = struct.unpack("<3f", blob.read(resolvable[0] + off, 12))
            print(f"  +0x{off:02X}: {n_ok}/{len(resolvable)} proxies  e.g. ({sample[0]:.2f}, {sample[1]:.2f}, {sample[2]:.2f})")


if __name__ == "__main__":
    main()
