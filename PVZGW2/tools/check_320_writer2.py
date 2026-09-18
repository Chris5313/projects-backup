#!/usr/bin/env python3
"""Chunked search for xmmword stores to [reg+320h]."""
import subprocess, json

def call(name, args, timeout=240):
    out = subprocess.run(["python", "ida_rpc.py", "call", name, json.dumps(args)],
                         capture_output=True, text=True, timeout=timeout)
    s = out.stdout.strip()
    try: return json.loads(s)
    except Exception: return {"raw": s[:300]}

# chunk through the image in 1MB steps to avoid the 60s tool timeout
START, END = 0x140001000, 0x142C00000
STEP = 0x100000
hits = []
a = START
while a < END:
    d = call("search_text", {"pattern": "xmmword ptr [r+320h], xmm",
                              "start_ea": hex(a), "end_ea": hex(a + STEP)})
    if isinstance(d, dict):
        for h in d.get("hits", []):
            hits.append((h["addr"], h["matches"][0]["text"]))
    a += STEP
print(f"total +320h xmm stores: {len(hits)}")
for addr, txt in hits[:30]:
    print("  ", addr, txt)
