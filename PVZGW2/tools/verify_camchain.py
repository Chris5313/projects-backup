#!/usr/bin/env python3
"""Check CAM_STATIC value in the image dump + what camMgr+0x70 holds.

Runtime log (v55) said: CAMMGR: 0x31FB4300 +70=0x7A639F0 — but the REAL live
view (proj signature) is at 0x3C7F9B0. So camMgr+0x70 was a DIFFERENT object
(that's why poll never sees live data). Check what the engine's submit fn
actually uses: from decompile, views come from [ctx+416..424) where ctx is
the arg a1 of sub_1409E2B80. Find what a1 is: check callers/xrefs of the
submit fn for the global that holds ctx.
"""
import struct

BASE = 0x140000000
img = open(r"C:/Users/Public/gw2_dump_image.bin","rb").read()

def imgq(va):
    o = va - BASE
    return struct.unpack_from("<Q", img, o)[0] if 0 <= o <= len(img)-8 else None

CAM_STATIC = 0x142CEE730
print("CAM_STATIC value in image:", hex(imgq(CAM_STATIC) or 0))
ACT = 0x142D05410
print("ACTIVE_VIEW_STATIC value in image:", hex(imgq(ACT) or 0))

# search image for qword pointing at real view page (0x3C7F9B0 case: heap addr, impossible in image)
# The engine's ctx (a1) must come from a static. Look at sub_1409E2B80 callers:
# from earlier xref work, find calls to 0x1409E2B80:
sig = struct.pack("<Q", 0x1409E2B80)
start = 0; cnt = 0
print("qword refs to submit fn:")
while cnt < 5:
    i = img.find(sig, start)
    if i < 0: break
    print(f"  img VA {hex(BASE+i)}")
    cnt += 1; start = i+1
