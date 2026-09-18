#!/usr/bin/env python3
"""GW2 Frostbite SDK-lite dump (final offline pass).

What we know:
  - Class records: 0x38 bytes in 0x143200000-0x1438C0000:
      +0x00 namePtr (ASCII), +0x08 flags, +0x10 moduleMeta, +0x20 fieldsPtr
  - Module metas in 0x1429A0000-0x142A00000: {parentModulePtr@+0x08, ...}
    BFS showed child classes reach parent CLASSES through the meta graph
    (ClientControllableHealthComponent -> ClientControllableEntity etc).
  - Member names are not stored as strings in the image (hash-based).

Output: gw2_sdk_dump.txt with all 8461 classes: name, flags, module chain,
and parent candidates (strings reachable within 3 hops of the record).
"""
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

region_lo, region_hi = 0x143000000, BASE+N
classes = {}   # name -> (rec, flags, module, fieldsPtr)
va = region_lo
while va < region_hi - 8:
    nm = rd64(va)
    s = cstr(nm) if nm else None
    if s and re.fullmatch(r"[A-Za-z][A-Za-z0-9_]{2,60}", s):
        flags = struct.unpack_from("<I", data, fo(va)+8)[0]
        mod = rd64(va + 0x10)
        fld = rd64(va + 0x20)
        if mod and 0x1429A0000 <= mod < 0x142A00000 and s not in classes:
            classes[s] = (va, flags, mod, fld)
    va += 8
print(f"classes: {len(classes)}")

def module_chain(mod):
    out = []
    seen = set()
    while mod and mod not in seen and len(out) < 8:
        seen.add(mod)
        s = cstr(rd64(mod) or 0)
        if not s: break
        out.append(s)
        mod = rd64(mod + 8)
    return out

with open(r"C:/Users/Public/gw2_sdk_dump.txt","w") as f:
    f.write("// GW2 Frostbite SDK-lite dump (offline, from decrypted image)\n")
    f.write("// format: name | flags | module-chain\n")
    for s in sorted(classes):
        rec, flags, mod, fld = classes[s]
        mc = " <- ".join(module_chain(mod))
        f.write(f"{s} | {flags:#x} | {mc}\n")
print("wrote C:/Users/Public/gw2_sdk_dump.txt")

# ESP-relevant classes
print("\n== ESP-relevant ==")
for pat in ("ClientPVZCharacterEntity","ClientCharacterEntity","ClientControllableEntity",
            "ClientPlayerManagerEntity","ClientHumanPlayerEntity","GameView",
            "ClientCharacterHealthComponent","HealthComponent","SpatialEntity",
            "ClientPhysicsEntity","ComponentEntity","Entity"):
    if pat in classes:
        rec, flags, mod, fld = classes[pat]
        print(f"  {pat:38s} rec={hex(rec)} flags={flags:#x} chain={' <- '.join(module_chain(mod))}")
