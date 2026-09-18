#!/usr/bin/env python3
"""Determine GW2's metadata string encoding.

Class names: plain ASCII (we indexed 8461 of them).
Member names: NOT found for GameView ('cameraController' absent).
Test: are member names stored as 32-bit NameId hashes? Frostbite's fb::NameId
= FNV-ish of the string. If so we can't get member names, but we don't need
names — we need OFFSETS. Are there offset tables anywhere? The GameView field
records' small u32s (0xbde/0xb16/0x1525 = ~3038/2838/5429) - if these were
byte offsets they'd be absurd. They look like hash ids too.

DECISIVE TEST: find a field offset we KNOW. CharObj max health @+0x8C.
ClientCharacterHealthComponent should have a health field. Search its field
records for small u32s in 0..0x200 range.
"""
import struct, re
BASE = 0x140000000
data = open(r"C:/Users/Public/gw2_dump_image.bin","rb").read()
N = len(data)
def fo(va): return va-BASE
def rd64(va):
    o=fo(va)
    if 0<=o<=N-8: return struct.unpack_from("<Q",data,o)[0]
def u32(va):
    o=fo(va)
    return struct.unpack_from("<I",data,o)[0] if 0<=o<=N-4 else 0
def cstr(va,n=80):
    o=fo(va)
    if o is None or o<0x1000 or o>=N: return None
    end=data.find(b"\x00",o,o+n)
    if end<=o: return None
    s=data[o:end].decode(errors="replace")
    return s if re.fullmatch(r"[\x20-\x7e]{2,79}",s) else None

# ClientCharacterHealthComponent block + its fields array
# find its TypeMeta first:
import bisect
# from index file
idx = {}
for line in open(r"C:/Users/Public/gw2_fb_class_index.txt"):
    s, rec, cls, fl = line.rstrip("\n").split("\t")
    idx[s] = (int(rec,16), int(cls,16), int(fl,16))
for nm in ("ClientCharacterHealthComponent","ClientPlayerManagerEntity","SpatialEntity"):
    rec, cls, fl = idx[nm]
    farr = rd64(rec + 0x20)
    print(f"== {nm}: fields @ {hex(farr)} countguess={hex((fl>>8)&0xFFF)} ==")
    for d in range(0, 0x60, 8):
        q = rd64(farr+d)
        s = cstr(q) if q else None
        print(f"  +{d:#04x}: {hex(q or 0)} {s!r}")
    # dump u32 view
    print("  u32s:", [hex(u32(farr+d)) for d in range(0, 0x40, 4)])
    print()
