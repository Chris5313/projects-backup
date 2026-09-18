#!/usr/bin/env python3
"""What does sub_141475110 do with the proj matrix? (Called by the active-view
consumer with (matrix, 0, 0, -1).) Decompile it. Also check sub_141472D00."""
import subprocess, json

def call(name, args):
    out = subprocess.run(["python", "ida_rpc.py", "call", name, json.dumps(args)],
                         capture_output=True, text=True, timeout=600)
    s = out.stdout.strip()
    try: return json.loads(s)
    except Exception: return {"raw": s[:300]}

for fn in ("0x141475110", "0x141472D00"):
    print(f"===== {fn} =====")
    d = call("decompile", {"addr": fn})
    code = d.get("code", "")
    print(code[:2500])
    print()
