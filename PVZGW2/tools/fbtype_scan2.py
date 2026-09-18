#!/usr/bin/env python3
"""GW2 Frostbite type metadata - 0x18 stride confirmed (22k consecutive hits).

Record: {namePtr, u32 flags, u32 pad, ClassInfo*}  (stride 0x18)
Build full name->ClassInfo index; dump interesting classes; reverse layout.
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
while va < region_hi:
    nm = rd64(va)
    s = cstr(nm) if nm else None
    if s and re.fullmatch(r"[A-Za-z][A-Za-z0-9_]{2,60}", s):
        cls = rd64(va + 0x10)
        flags = struct.unpack_from("<I", data, fo(va)+8)[0]
        if s not in index:
            index[s] = (va, cls, flags)
    va += 0x18
print(f"named records: {len(index)}")

# dump a few interesting
for name in ("RenderView", "ClientPlayerManagerEntity", "DataContainer", "TypeInfo"):
    if name in index:
        rec, cls, fl = index[name]
        print(f"\n== {name} rec={hex(rec)} flags={hex(fl)} ClassInfo={hex(cls or 0)} ==")
        if cls:
            for d in range(0, 0x60, 8):
                q = rd64(cls+d)
                cs = cstr(q) if q else None
                print(f"  CI+{d:#04x}: {hex(q or 0)} {cs!r}")
    else:
        print(f"\n{name}: NOT FOUND")

# save full index
with open(r"C:/Users/Public/gw2_fb_class_index.txt", "w") as f:
    for s, (rec, cls, fl) in sorted(index.items()):
        f.write(f"{s}\t{hex(rec)}\t{hex(cls or 0)}\t{hex(fl)}\n")
print("\nfull index -> C:/Users/Public/gw2_fb_class_index.txt")
