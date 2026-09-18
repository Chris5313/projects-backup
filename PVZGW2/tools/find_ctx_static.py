#!/usr/bin/env python3
"""Who calls sub_1409E2B80 and with what ctx? Find its callers."""
import subprocess, json

def call(name, args):
    out = subprocess.run(["python", "ida_rpc.py", "call", name, json.dumps(args)],
                         capture_output=True, text=True, timeout=600)
    s = out.stdout.strip()
    try: return json.loads(s)
    except Exception: return {"raw": s[:300]}

d = call("xref_query", {"addrs": ["0x1409E2B80"], "direction": "xrefs"})
print(json.dumps(d, indent=1)[:3000])
