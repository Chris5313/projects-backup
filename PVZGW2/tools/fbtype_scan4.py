#!/usr/bin/env python3
"""Final GW2 class index: TypeMeta records -> ClassInfo metas in 0x1429A-0x142A zone."""
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
index = {}
va = region_lo
while va < region_hi - 8:
    nm = rd64(va)
    s = cstr(nm) if nm else None
    if s and re.fullmatch(r"[A-Za-z][A-Za-z0-9_]{2,60}", s):
        cls = rd64(va + 0x10)
        if cls and 0x1429A0000 <= cls < 0x142A00000:
            flags = struct.unpack_from("<I", data, fo(va)+8)[0]
            if s not in index:
                index[s] = (va, cls, flags)
    va += 8
print(f"classes: {len(index)}")

def dump_meta(name, n=0x60):
    if name not in index:
        print(f"\n{name}: NOT FOUND"); return
    rec, cls, fl = index[name]
    print(f"\n== {name} TypeMeta={hex(rec)} flags={hex(fl)} meta={hex(cls)} ==")
    for d in range(0, n, 8):
        q = rd64(cls+d)
        s2 = cstr(q) if q else None
        mark = ""
        if s2 is None and q and 0x140000000 <= q < BASE+N:
            s3 = cstr(rd64(q) or 0)
            if s3: mark = f" ->{s3!r}"
        print(f"  +{d:#04x}: {hex(q or 0)} {s2!r}{mark}")

for nm in ("ClientPlayerManagerEntity","ClientHumanPlayerEntity","ClientControllableEntity",
           "ClientCharacterHealthComponent","ClientPlayerGateEntity","SpatialEntity"):
    dump_meta(nm)

with open(r"C:/Users/Public/gw2_fb_class_index.txt","w") as f:
    for s,(rec,cls,fl) in sorted(index.items()):
        f.write(f"{s}\t{hex(rec)}\t{hex(cls)}\t{hex(fl)}\n")
print("\nindex rewritten: 8461-class zone check")
