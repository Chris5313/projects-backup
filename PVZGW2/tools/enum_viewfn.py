#!/usr/bin/env python3
"""Decompile sub_1409E68C0 (per-view update in submit loop) and look for
stores of a 4x4 into the view object, to confirm where cam-world lives."""
import subprocess, os, json

TOOL = os.path.join(os.path.dirname(__file__), "ida_rpc.py")
def call(name, args):
    out = subprocess.run(["python", TOOL, "call", name, json.dumps(args)],
                         capture_output=True, text=True, timeout=600)
    s = out.stdout.strip()
    try:
        return json.loads(s)
    except Exception:
        return {"raw": s}

d = call("decompile", {"addr": "0x1409E68C0"})
code = d.get("code", d.get("raw", ""))
if code:
    lines = code.splitlines()
    print(f"total lines: {len(lines)}")
    for i, ln in enumerate(lines):
        if any(k in ln for k in ("240", "320", "3E0", "360", "2A0", "0x140", "144")):
            print(f"{i}: {ln}")
else:
    print(code[:800])
