#!/usr/bin/env python3
"""Hunt the Frostbite reflection (ClassInfo) list in the decrypted image dump.

Approach (FrostbiteGen-style but offline):
 1. Pull all printable ASCII strings >= 5 chars from .rdata-like offsets.
 2. Find 8-byte LE pointers in the image that point INTO the string blob.
 3. Around each such 'name pointer' location, check the 0xC0-byte window for a
    plausible ClassInfo per FrostbiteGen layout:
       +0x00 self->typeInfo (== window start, i.e. points at the name)
       +0x08 next           (plausible heap/image ptr)
       +0x18 parent
    and per the v55 discovery that GW2's ClassInfo == TypeInfo:
       +0x00 name, +0x08 next, +0x18 parent  (also plausible)
 4. For the best-scoring candidate, follow 'next' up to 20k nodes and report
    where it dies, sample names, and the chain length.

If the FrostbiteGen layout wins with a long chain, we have the real list head
and the F11 in-process dump is a straight port.
"""
import re, struct, sys

IMG = r"C:/Users/Public/gw2_dump_image.bin"
BASE = 0x140000000  # load base used when building the dump (file off == RVA)

data = open(IMG, "rb").read()
N = len(data)

# ---- 1. strings -------------------------------------------------------------
strs = []  # (file_off, text)
for m in re.finditer(rb"[\x20-\x7e]{5,64}", data[0x1C00000:]):
    strs.append((0x1C00000 + m.start(), m.group().decode()))
str_offs = {off for off, _ in strs}
print(f"strings in tail blob: {len(strs)}")

# ---- 2. pointers into string blob ------------------------------------------
blob_lo, blob_hi = 0x1C00000, N
cands = []  # (ptr_loc, name_off)
step = 8
for off in range(0x100000, N - 8, step):
    v = struct.unpack_from("<Q", data, off)[0]
    fo = v - BASE
    if blob_lo <= fo < blob_hi and fo in str_offs:
        cands.append((off, fo))
print(f"pointers into string blob: {len(cands)}")

# ---- 3/4. score ClassInfo hypotheses ----------------------------------------
def rd64(fo):
    if 0 <= fo <= N - 8:
        return struct.unpack_from("<Q", data, fo)[0]
    return None

def plausible_ptr(v):
    if v is None: return False
    fo = v - BASE
    return (0x1000 <= fo < N) or (0x140000000 <= v < 0x180000000) or (0x10000 <= v < 0x140000000)

def chain_len(head_fo, off_next, off_name, name_set, limit=30000):
    """Follow next-chain; count nodes whose name field lands on a known string."""
    seen = set()
    good = 0
    fo = head_fo
    first_names = []
    while len(seen) < limit:
        if fo in seen or fo is None or fo < 0x1000 or fo > N - 0xC0: break
        seen.add(fo)
        nv = rd64(fo + off_name)
        nfo = (nv - BASE) if nv else None
        if nfo in name_set:
            good += 1
            if len(first_names) < 12:
                # read the string
                end = data.find(b"\x00", nfo)
                first_names.append(data[nfo:end].decode(errors="replace"))
        nxt = rd64(fo + off_next)
        if nxt is None: break
        fo = nxt - BASE
    return good, len(seen), first_names

# candidate ClassInfo windows: pointer-to-name at window+0 => window == ptr_loc
hypoA = dict(off_name=0x00, off_next=0x08)   # FrostbiteGen: typeInfo@0{name}, next@8
# group candidate windows
wins = [loc for loc, _ in cands]
print(f"candidate windows: {len(wins)}")

best = []
for fo in wins[:4000]:
    # FrostbiteGen: +0x00 = TypeInfo* whose +0x00 is name -> so window+0 points to TypeInfo, name at TypeInfo+0
    # window+0 IS our name pointer only if typeInfo==name (v55 finding). Handle both:
    pass

# Hypo A-direct (ClassInfo.typeInfo == name ptr itself): window+0 = name
name_set = set(str_offs)
gA, nA, namesA = 0, 0, []
for fo in wins:
    g, n, nm = chain_len(fo, 0x08, 0x00, name_set, 30000)
    if n > nA: gA, nA, namesA = g, n, nm
print(f"[A] name@+0 next@+8 : visited={nA} named={gA} sample={namesA[:8]}")

# Hypo B: window+0 = TypeInfo* -> name at TypeInfo+0 (FrostbiteGen proper)
def chain_len_B(head_fo, limit=30000):
    seen, good, fo, names = set(), 0, head_fo, []
    while len(seen) < limit:
        if fo in seen or fo < 0x1000 or fo > N - 0xC0: break
        seen.add(fo)
        tiv = rd64(fo)
        tifo = (tiv - BASE) if tiv else None
        ok = False
        if tifo is not None and 0x1000 <= tifo < N - 0x40:
            nv = rd64(tifo)
            nfo = (nv - BASE) if nv else None
            if nfo in name_set:
                ok = True
                if len(names) < 12:
                    end = data.find(b"\x00", nfo)
                    names.append(data[nfo:end].decode(errors="replace"))
        if ok: good += 1
        nxt = rd64(fo + 8)
        if nxt is None: break
        fo = nxt - BASE
    return good, len(seen), names

gB, nB, namesB = 0, 0, []
for fo in wins:
    g, n, nm = chain_len_B(fo, 30000)
    if n > nB: gB, nB, namesB = g, n, nm
print(f"[B] ti@+0->name    : visited={nB} named={gB} sample={namesB[:8]}")
