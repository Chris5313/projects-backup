#!/usr/bin/env python3
"""Evidence block: the per-view proj write sequence in sub_1409E2B80."""
import subprocess, json

def call(name, args):
    out = subprocess.run(["python", "ida_rpc.py", "call", name, json.dumps(args)],
                         capture_output=True, text=True, timeout=240)
    s = out.stdout.strip()
    try: return json.loads(s)
    except Exception: return {"raw": s[:300]}

d = call("disasm", {"addr": "0x1409E3670", "max_instructions": 20})
for ln in d.get("asm", {}).get("lines", []):
    print(ln["addr"], ln["instruction"])
