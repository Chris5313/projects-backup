#!/usr/bin/env python3
"""Enumerate GW2's 16-byte-strided reflection entry array.

Verified facts:
  - 0x1429B8EC8 is entry {name='PathfindingShared', next=0x1429ABAF8}
  - names are packed inline, 16-byte aligned, next chains downward
So entries form an array with stride 0x10: [namePtr, nextPtr].
Find the array start (min name ptr) and enumerate the whole thing.
"""
import struct, re

BASE = 0x140000000
data = open(r"C:/Users/Public/gw2_dump_image.bin", "rb").read()
N = len(data)
def fo(va): return va - BASE
def rd64(va):
    o = fo(va)
    if 0 <= o <= N - 8: return struct.unpack_from("<Q", data, o)[0]
    return None
def cstr(va, n=80):
    o = fo(va)
    if o is None or o < 0x1000 or o >= N: return None
    end = data.find(b"\x00", o, o + n)
    if end <= o: return None
    s = data[o:end].decode(errors="replace")
    return s if re.fullmatch(r"[\x20-\x7e]{2,79}", s) else None

HEAD = 0x1429B8EC8
# walk down to the smallest-address entry
va = HEAD
seen = set()
while True:
    nxt = rd64(va + 8)
    if not nxt or nxt in seen: break
    seen.add(va)
    va = nxt
lowest_entry = va
print("lowest entry:", hex(lowest_entry))

# candidates for array start: scan downward from lowest entry while
# entry pattern (namePtr valid, next == prev-0x10) keeps holding
start = lowest_entry
while True:
    prev = start - 0x10
    n0 = rd64(prev)
    if n0 and cstr(n0):
        n1 = rd64(prev + 8)
        if n1 == start:
            start = prev
            continue
    break
print("array start candidate:", hex(start))

# count entries forward
entries = []
va = start
while True:
    n0 = rd64(va)
    nxt = rd64(va + 8)
    s = cstr(n0) if n0 else None
    if not s:
        break
    entries.append((va, s, nxt))
    if nxt == va or nxt is None or not (0x140000000 <= nxt < BASE + N): break
    va = nxt
print(f"entries: {len(entries)}")
print("first 10:", [e[1] for e in entries[:10]])
print("last 5:", [e[1] for e in entries[-5:]])

# search for interesting names
want = ["RenderView", "ScreenComponent", "RenderCamera", "CameraEntity", "CameraManager",
        "ClientPlayerManager", "TypeInfo", "DataContainer", "GameView", "Mesh",
        "ClientSoldierEntity", "ClientPlayerEntry", "HealthComponent", "SpatialEntity",
        "ClientControllableEntity", "ComponentEntity", "GameComponent"]
found = {s: va for va, s, _ in entries if s in want}
print("interesting:", {k: hex(v) for k, v in found.items()})

# dump nested structs of a few
for name in ("RenderView", "ScreenComponent", "TypeInfo", "DataContainer"):
    if name in found:
        va = found[name]
        n0 = rd64(va)
        print(f"\n== {name} @ {hex(va)} (name@{hex(n0)}) ==")
        for d in range(0x10, 0x90, 8):
            q = rd64(va + d)
            cs = cstr(q) if q else None
            print(f"  +{d:#04x}: {hex(q or 0)}  {cs!r}")
