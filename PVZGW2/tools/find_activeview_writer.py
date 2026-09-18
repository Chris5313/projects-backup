#!/usr/bin/env python3
"""Find what writes qword_142D05410 (the active RenderView pointer static)."""
import sys, subprocess, os, json

TOOL = os.path.join(os.path.dirname(__file__), "ida_rpc.py")

def call(name, args):
    out = subprocess.run(["python", TOOL, "call", name, json.dumps(args)],
                         capture_output=True, text=True, timeout=300)
    return out.stdout.strip() or out.stderr.strip()

# all 6 xrefs (2 in functions, 4 bare addresses) - disassemble around each
for ea in ("0x14123f669", "0x14124018f", "0x1412401c6", "0x14125d55e"):
    print("=" * 20, ea)
    print(call("disasm", {"addr": ea, "max_instructions": 8})[:2000])
    print()
