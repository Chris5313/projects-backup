#!/usr/bin/env python3
"""Print sub_1412411D0 tail (active-view consumer) to see the math."""
import subprocess, json

def call(name, args):
    out = subprocess.run(["python", "ida_rpc.py", "call", name, json.dumps(args)],
                         capture_output=True, text=True, timeout=600)
    s = out.stdout.strip()
    try: return json.loads(s)
    except Exception: return {"raw": s[:300]}

d = call("decompile", {"addr": "0x1412411D0"})
lines = d.get("code","").splitlines()
print(f"total {len(lines)}")
print("\n".join(lines[440:480]))
