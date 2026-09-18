#!/usr/bin/env python3
"""Parse GW2's class blocks in the TypeMeta region.

Model to verify (from ClientPlayerManagerEntity @0x143213778):
  block = 0x38 bytes: {namePtr, flags(u32), moduleMeta, 0, fieldsPtr, 0, 0}
  fieldsPtr -> member entry array {namePtr, ...}

Scan the region for this block pattern, build class->fields, dump samples.
"""
import struct, re
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

region_lo, region_hi = 0x143000000, BASE+N
# verify stride: check the 0x38 block for ClientPlayerManagerEntity (0x143213778)
va = 0x143213778
print("== block @ ClientPlayerManagerEntity ==")
for d in range(0, 0x38, 8):
    q = rd64(va+d)
    s = cstr(q) if q else None
    print(f"  +{d:#04x}: {hex(q or 0)} {s!r}")
print()
# compare with ClientHumanPlayerEntity @0x1432116a8 and GameView @0x1432296c8
for name, v in (("ClientHumanPlayerEntity",0x1432116a8),("GameView",0x1432296c8)):
    print(f"== {name} block ==")
    for d in range(0, 0x38, 8):
        q = rd64(v+d)
        s = cstr(q) if q else None
        print(f"  +{d:#04x}: {hex(q or 0)} {s!r}")
    print()
