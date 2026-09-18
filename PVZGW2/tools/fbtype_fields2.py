#!/usr/bin/env python3
"""Decode +0x20 array inside the 0x38 class block.

ClientPlayerManagerEntity: fields @0x14323B230 (flags 0x480035)
GameView:                  fields @0x143254E60 (flags 0x100035)
flags&0xFF = 0x35 both. 0x48/0x10 in upper bits - maybe field count?
0x480035 >> 8 = 0x4803, hmm. Try: (flags>>8)&0xFFF = 0x480 / 0x10 -> 0x48=72, 0x10=16.
Maybe that's the field count! CMPM = 72 fields vs GameView 16 fields. Plausible.

Probe fields array: dump first 0x80 bytes of each, find name strings + offsets.
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

for name, fva, flags in (("ClientPlayerManagerEntity",0x14323B230,0x480035),
                         ("GameView",0x143254E60,0x100035)):
    cnt = (flags>>8)&0xFFF
    print(f"== {name} fields @ {hex(fva)} guess-count={cnt} ==")
    for d in range(0, 0x80, 8):
        q = rd64(fva+d)
        s = cstr(q) if q else None
        print(f"  +{d:#04x}: {hex(q or 0)} {s!r}")
    print()
