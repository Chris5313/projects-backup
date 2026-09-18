#!/usr/bin/env python3
"""Find functions that store to [view+0x320] (cam world) / [view+0x3E0] (proj).
Uses IDA insn_query over .text with mov/movaps/movups patterns. The IDB has
most code in a segment named '.srdata' (it's a memory dump)."""
import subprocess, json, re

def call(name, args):
    out = subprocess.run(["python", "ida_rpc.py", "call", name, json.dumps(args)],
                         capture_output=True, text=True, timeout=600)
    s = out.stdout.strip()
    try: return json.loads(s)
    except Exception: return {"raw": s}

# Strategy: use insn_query to find "movups [reg+3E0h]" and "movups [reg+320h]"
# and "movaps [reg+320h]" etc. First check insn_query schema:
d = call("insn_query", {"queries": [{"mnemonic": "movups", "operand_regex": r"\+\s*3E0h\]"}]})
print(json.dumps(d, indent=1)[:3000])
