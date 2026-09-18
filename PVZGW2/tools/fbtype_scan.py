#!/usr/bin/env python3
"""Walk GW2's real Frostbite type metadata.

Discovered: records at 0x143200000+ look like {namePtr, flags(u32), pad,
ClassInfo*, pad, ...} stride 0x20 (need to confirm). Enumerate all records by
scanning for qwords that point at known-name strings and dumping structure.
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

# Pass 1: how many qwords in region 0x143000000..N point to valid cstrings?
# and what's the stride between consecutive such hits?
region_lo, region_hi = 0x143000000, BASE+N
hits = []
off = region_lo - BASE
while off < N-8:
    v = struct.unpack_from("<Q", data, off)[0]
    if v and 0x141000000 <= v < BASE+N:
        s = cstr(v)
        if s and re.fullmatch(r"[A-Za-z][A-Za-z0-9_]{2,60}", s):
            hits.append((BASE+off, s))
    off += 8
print("name-like qwords in region:", len(hits))
if len(hits) > 2:
    d1 = hits[1][0]-hits[0][0]
    strides = {}
    for a, b in zip(hits, hits[1:]):
        d = b[0]-a[0]
        strides[d] = strides.get(d, 0)+1
    top = sorted(strides.items(), key=lambda kv:-kv[1])[:5]
    print("top strides:", [(hex(k), v) for k, v in top])
print("sample:", [(hex(a), s) for a, s in hits[:10]])

# Take the dominant stride, enumerate all records from region start
if hits:
    stride = sorted(strides.items(), key=lambda kv:-kv[1])[0][0]
    # align to first hit
    recs = []
    va = hits[0][0]
    while va >= region_lo and va < region_hi:
        nm = rd64(va)
        s = cstr(nm) if nm else None
        cls = rd64(va+0x10)
        recs.append((va, s, cls))
        va += stride
    named = [r for r in recs if r[1]]
    print(f"records walked: {len(recs)} named: {len(named)}")

    want = {"RenderView","ClientPlayerManagerEntity","ScreenComponent","RenderCamera",
            "CameraEntity","TypeInfo","DataContainer","GameView","HealthComponent",
            "ClientControllableEntity","SpatialEntity","ClientPlayerEntity"}
    for va, s, cls in named:
        if s in want:
            print(f"\n== {s} rec@{hex(va)} ClassInfo={hex(cls or 0)} ==")
            if cls:
                for d in range(0, 0x78, 8):
                    q = rd64(cls+d)
                    cs = cstr(q) if q else None
                    extra = ""
                    if cs is None and q and 0x140000000 < q < BASE+N:
                        s2 = cstr(rd64(q) or 0)
                        if s2: extra = f" -> {s2!r}"
                    print(f"  CI+{d:#04x}: {hex(q or 0)} {cs!r}{extra}")
