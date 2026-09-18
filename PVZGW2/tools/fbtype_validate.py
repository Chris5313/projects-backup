#!/usr/bin/env python3
"""Validate the 0x38-block layout against runtime-known offsets.

We KNOW from runtime/IDA: CharObj (custom class) has:
  +0x18 marker, +0x48 TypeInfo, +0x50 proxy, +0x8C MaxHealth float.

If GW2's ClassInfo carries offsets, ClientCharacterHealthComponent's record
should contain 0x8C somewhere (health offset) OR its nested pointers lead to
a table with small u32s. Compare raw records across classes - if words differ
between classes, they encode class-specific data.
"""
import struct, re
BASE = 0x140000000
data = open(r"C:/Users/Public/gw2_dump_image.bin","rb").read()
N = len(data)
def fo(va): return va-BASE
def rd64(va):
    o=fo(va)
    if 0<=o<=N-8: return struct.unpack_from("<Q",data,o)[0]

idx = {}
for line in open(r"C:/Users/Public/gw2_fb_class_index.txt"):
    s, rec, cls, fl = line.rstrip("\n").split("\t")
    idx[s] = int(rec,16)

# raw hex of 5 different class records
for nm in ("ClientCharacterHealthComponent","GameView","SpatialEntity",
           "ClientPlayerManagerEntity","LinearTransform"):
    if nm not in idx: continue
    rec = idx[nm]
    o = fo(rec)
    print(f"{nm:36s} {data[o:o+0x38].hex(' ')}")
print()
# compare: same 0x38-block word-by-word diff count across the five
recs = [idx[n] for n in ("ClientCharacterHealthComponent","GameView","SpatialEntity",
                         "ClientPlayerManagerEntity","LinearTransform") if n in idx]
import itertools
for a, b in itertools.combinations(range(len(recs)), 2):
    oa, ob = fo(recs[a]), fo(recs[b])
    same = sum(1 for d in range(0, 0x38, 4)
               if struct.unpack_from("<I",data,oa+d)[0] == struct.unpack_from("<I",data,ob+d)[0])
    print(f"blocks {a} vs {b}: {same}/14 words identical")
