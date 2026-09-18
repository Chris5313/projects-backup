#!/usr/bin/env python3
"""BFS from a class record through all pointers; report strings at each depth."""
import struct, re
from collections import deque
BASE = 0x140000000
data = open(r"C:/Users/Public/gw2_dump_image.bin","rb").read()
N = len(data)
def fo(va): return va-BASE
def rd64(va):
    o=fo(va)
    if 0<=o<=N-8: return struct.unpack_from("<Q",data,o)[0]
def cstr(va,n=80):
    o=fo(va)
    if o is None or o<0x1000 or o>=N: return None
    end=data.find(b"\x00",o,o+n)
    if end<=o: return None
    s=data[o:end].decode(errors="replace")
    return s if re.fullmatch(r"[\x20-\x7e]{2,79}",s) else None

idx = {}
for line in open(r"C:/Users/Public/gw2_fb_class_index.txt"):
    s, rec, cls, fl = line.rstrip("\n").split("\t")
    idx[s] = int(rec,16)
rec = idx["ClientCharacterHealthComponent"]

seen = set()
q = deque([(rec, 0)])
depth_strings = {}
MAXD = 5
while q:
    va, d = q.popleft()
    if va in seen or d > MAXD or va < 0x140000000 or va >= BASE+N: continue
    seen.add(va)
    s = cstr(rd64(va) or 0)
    if s:
        depth_strings.setdefault(d, []).append((hex(va), s))
    for off in range(0, 0x48, 8):
        nx = rd64(va+off)
        if nx and 0x142000000 <= nx < BASE+N:
            q.append((nx, d+1))

for d in sorted(depth_strings):
    lst = depth_strings[d]
    print(f"depth {d}: {len(lst)} strings; sample:")
    for va, s in lst[:12]:
        print("   ", va, repr(s))
