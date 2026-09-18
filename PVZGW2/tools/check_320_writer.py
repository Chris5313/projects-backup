#!/usr/bin/env python3
"""Find writers of [reg+320h] and cross-check +3E0."""
import subprocess, json

def call(name, args):
    out = subprocess.run(["python", "ida_rpc.py", "call", name, json.dumps(args)],
                         capture_output=True, text=True, timeout=600)
    s = out.stdout.strip()
    try: return json.loads(s)
    except Exception: return {"raw": s}

d = call("search_text", {"pattern": "movaps  xmmword ptr [r+320h], xmm"})
if isinstance(d, dict) and "hits" in d:
    print(f"+320 writers: {d['n']}")
    for h in d["hits"][:20]:
        print("  ", h["addr"], h["matches"][0]["text"])
else:
    print(d)
