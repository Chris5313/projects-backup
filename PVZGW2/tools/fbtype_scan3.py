#!/usr/bin/env python3
"""GW2 Frostbite class index - collect every {namePtr, flags, ClassInfo*} record
in the TypeMeta region regardless of packing. A record is a CLASS if its +0x10
qword points into the ClassInfo zone (0x142000000-0x143000000).
Output: full class index + structure dump of key classes.
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
index = {}   # name -> (recVA, classInfoVA, flags)
va = region_lo
while va < region_hi - 8:
    nm = rd64(va)
    s = cstr(nm) if nm else None
    if s and re.fullmatch(r"[A-Za-z][A-Za-z0-9_]{2,60}", s):
        cls = rd64(va + 0x10)
        if cls and 0x142000000 <= cls < 0x143000000:
            flags = struct.unpack_from("<I", data, fo(va)+8)[0]
            if s not in index:
                index[s] = (va, cls, flags)
    va += 8
print(f"classes found: {len(index)}")

def dump_ci(name, size=0x60):
    if name not in index:
        print(f"\n{name}: NOT FOUND")
        return
    rec, cls, fl = index[name]
    print(f"\n== {name} rec={hex(rec)} flags={hex(fl)} ClassInfo={hex(cls)} ==")
    for d in range(0, size, 8):
        q = rd64(cls+d)
        cs = cstr(q) if q else None
        print(f"  CI+{d:#04x}: {hex(q or 0)} {cs!r}")

for nm in ("RenderView", "ClientPlayerManagerEntity", "DataContainer", "TypeInfo"):
    dump_ci(nm)

with open(r"C:/Users/Public/gw2_fb_class_index.txt", "w") as f:
    for s, (rec, cls, fl) in sorted(index.items()):
        f.write(f"{s}\t{hex(rec)}\t{hex(cls)}\t{hex(fl)}\n")
print("\nfull index -> C:/Users/Public/gw2_fb_class_index.txt")
