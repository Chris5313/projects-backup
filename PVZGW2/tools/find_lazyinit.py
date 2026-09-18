#!/usr/bin/env python3
"""Find the function containing the lazy-init store at 0x1412401C6:
scan back over int3 padding to find the prologue, define nothing (read-only),
decompile the enclosing region by finding the nearest preceding function via
list_funcs/lookup. Simplest: search_text backward is unavailable; use
'func_query' with range or 'basic_blocks'. Fallback: disassemble backwards
manually via get_bytes and locate 0xCC padding, then call decompile on the
function start guessed from alignment.
"""
import subprocess, os, json, re

TOOL = os.path.join(os.path.dirname(__file__), "ida_rpc.py")
def call(name, args):
    out = subprocess.run(["python", TOOL, "call", name, json.dumps(args)],
                         capture_output=True, text=True, timeout=300)
    return out.stdout.strip()

# get bytes 0x400 back and locate the last int3 run before 0x1412401c6
start = 0x1412401c6 - 0x600
b = call("get_bytes", {"addr": hex(start), "size": 0x600})
print("bytes:", b[:400])
