#!/usr/bin/env python3
"""Find E8-callers of 0x1409E2B80 by brute-force over call sites:
scan all E8 byte patterns is too broad; instead search the memory regions of
the few known job-queue functions. Simpler: get all E8 calls whose target
resolves to 0x1409E2B80 by scanning .text in the IDB via get_bytes chunks."""
import subprocess, json, struct

def call(name, args):
    out = subprocess.run(["python", "ida_rpc.py", "call", name, json.dumps(args)],
                         capture_output=True, text=True, timeout=600)
    s = out.stdout.strip()
    try: return json.loads(s)
    except Exception: return {"raw": s[:300]}

TARGET = 0x1409E2B80
START, END = 0x140001000, 0x142C00000
CH = 1 << 21
callers = []
a = START
while a < END:
    d = call("get_bytes", {"regions": [{"addr": hex(a), "size": min(CH, END - a)}]})
    if not isinstance(d, list) or not d:
        a += CH; continue
    hexs = d[0].get("data", "")
    data = bytes.fromhex(hexs.replace("0x", "").replace(" ", ""))
    for i in range(0, len(data) - 5):
        if data[i] == 0xE8:
            rel = struct.unpack_from("<i", data, i + 1)[0]
            if a + i + 5 + rel == TARGET:
                callers.append(a + i)
    a += CH

print(f"callers of submit: {[hex(c) for c in callers]}")
