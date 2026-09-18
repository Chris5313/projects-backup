#!/usr/bin/env python3
"""Decompile sub_1409E43B0 and sub_1409E4150 (camera-matrix producers in the
submit function) to find where the live camera transform comes from."""
import subprocess, json

def call(name, args):
    out = subprocess.run(["python", "ida_rpc.py", "call", name, json.dumps(args)],
                         capture_output=True, text=True, timeout=600)
    s = out.stdout.strip()
    try: return json.loads(s)
    except Exception: return {"raw": s[:300]}

for fn in ("0x1409E43B0", "0x1409E4150"):
    print(f"===== {fn} =====")
    d = call("decompile", {"addr": fn})
    code = d.get("code", "")
    if not code:
        print("  (fail)"); continue
    lines = code.splitlines()
    print(f"lines: {len(lines)}")
    # print head + any matrix-like ops
    for i, ln in enumerate(lines[:40]):
        print(f"  {i}: {ln.strip()[:150]}")
    print("  ...")
    for i, ln in enumerate(lines):
        if any(k in ln for k in ("34192", "34184", "1344", "34400", "33400")):
            print(f"  * {i}: {ln.strip()[:160]}")
    print()
