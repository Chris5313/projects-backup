#!/usr/bin/env python3
"""GameView fields @0x143254E60 look like 0x30-byte records: {p1, p2, u32hash?}.
p1/p2 point elsewhere in 0x1432 zone (nested descriptors).

Dump the two pointers' targets; find the string 'cameraController' or similar
GameView member names to identify which structure holds names.
Known Frostbite GameView fields: cameraController, trackCameraController,
cameraId, cinematicCameraController, trackCinematicCameraController...
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

for label, va in (("GameView.f0.p1", 0x143229498), ("GameView.f0.p2", 0x143256630),
                  ("GameView.f1.p1", 0x143229d20), ("GameView.f1.p2", 0x143256720),
                  ("GameView.f3.p1", 0x143243b00)):
    print(f"== {label} @ {hex(va)} ==")
    for d in range(0, 0x40, 8):
        q = rd64(va+d)
        s = cstr(q) if q else None
        mark = ""
        if s is None and q and 0x140000000 <= q < BASE+N:
            s2 = cstr(rd64(q) or 0)
            if s2: mark = f" ->{s2!r}"
        print(f"  +{d:#04x}: {hex(q or 0)} {s!r}{mark}")
    print()
# search image for known member names of GameView
for pat in (b"cameraController\x00", b"trackCameraController\x00", b"cinematicCameraController\x00", b"cameraId\x00"):
    i = data.find(pat)
    print(pat, "->", hex(BASE+i) if i>=0 else None)
