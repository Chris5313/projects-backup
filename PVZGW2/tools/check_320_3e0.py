#!/usr/bin/env python3
"""Check writers of [rcx+3E0h]: 0x1402C90AA, 0x1402DA585, 0x1402DC21A.
Disassemble context to find containing functions and whether they also touch +0x320."""
import subprocess, json

def call(name, args):
    out = subprocess.run(["python", "ida_rpc.py", "call", name, json.dumps(args)],
                         capture_output=True, text=True, timeout=600)
    s = out.stdout.strip()
    try: return json.loads(s)
    except Exception: return {"raw": s}

for ea in ("0x1402C90AA", "0x1402DA585", "0x1402DC21A"):
    print("="*20, ea)
    d = call("disasm", {"addr": ea, "max_instructions": 14})
    for ln in d.get("asm", {}).get("lines", []):
        print("  ", ln["addr"], ln["instruction"])
    fn = d.get("asm", {}).get("name")
    print("   function:", fn)
