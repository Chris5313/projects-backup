#!/usr/bin/env python3
"""Decode GW2's Frostbite ITypedObject metadata (image only).

Facts so far:
  - 'Modules': Core, PVZ, PVZClient... linked by +0x08 (prev) — these are
    namespace/type-group objects, each holding a member table.
  - Class-ish records: ClientPlayerManagerEntity CI:
      +0x00 'PVZClient'   -> owner module
      +0x08 0x1429A95D8   -> owner 'PVZ' (module chain)
      +0x18 0x8000001560  -> packed: class id 0x15? + size 0x60<<?
      +0x20 0x8
      +0x28 'GameplayClientPlayerExtent'  -> first member name!
      +0x30..0x48 3 fn ptrs
      +0x50.. pairs {hash(u32), pad, fnptr} - member list w/ hashes
So GW2's 'ClassInfo' = module objects owning member tables. The 62-node chain
we found earlier (WebUtils etc.) = singleton systems list, not classes.

Next: find the type registry mapping id->name for SDK gen. Look at +0x18
packed value across classes to confirm size field. ClientPlayerManagerEntity
runtime size? Its extent member at +0x28 suggests members only.
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

# Hypothesis: this whole zone 0x1429a0000-0x142a00000 = 'ITypedObject' metas
# built by static initializers. Each meta: {ownerModule, prevMeta, ?, packed,
#  memberName, fnptr, fnptr, fnptr, {hash,fnptr}*}
# The 8461 names from TypeMeta (0x1432xxxxx region) each point to one meta via
# +0x10. Those metas ARE the classes; member metas separate.
# Verify: take 20 random class names, follow to meta, read meta+0x00 (module),
# meta+0x18 packed. If packed low bits vary with class complexity -> size.

import random
names = [l.split("\t")[0] for l in open(r"C:/Users/Public/gw2_fb_class_index.txt")]
random.seed(1)
for nm in random.sample(names, 14):
    rec, cls, fl = None, None, None
    for line in open(r"C:/Users/Public/gw2_fb_class_index.txt"):
        pass
    break
# rebuild dict quickly
idx = {}
for line in open(r"C:/Users/Public/gw2_fb_class_index.txt"):
    s, rec, cls, fl = line.rstrip("\n").split("\t")
    idx[s] = (int(rec,16), int(cls,16), int(fl,16))
for nm in random.sample(sorted(idx), 14):
    rec, cls, fl = idx[nm]
    packed = rd64(cls + 0x18)
    mem0 = cstr(rd64(cls + 0x28) or 0) if (rd64(cls + 0x28) or 0) > 0x140000000 else None
    mod  = cstr(rd64(cls) or 0)
    print(f"{nm:48s} meta={hex(cls)} packed={hex(packed or 0)} mod={mod!r} m0={mem0!r}")
