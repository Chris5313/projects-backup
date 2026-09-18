#!/usr/bin/env python3
"""Find the engine's own W2S helper: functions that read the active view's
proj (via qword_142D05410) AND do a reciprocal/w-divide. Decompile each
consumer and print the projection math."""
import subprocess, json

def call(name, args):
    out = subprocess.run(["python", "ida_rpc.py", "call", name, json.dumps(args)],
                         capture_output=True, text=True, timeout=600)
    s = out.stdout.strip()
    try: return json.loads(s)
    except Exception: return {"raw": s[:300]}

d = call("xrefs_to", {"addrs": ["0x142D05410"]})
xrefs = d[0]["xrefs"] if isinstance(d, list) else []
print(f"xrefs to active-view static: {len(xrefs)}")
for x in xrefs:
    print(f"  {x['addr']} fn={x.get('fn',{}).get('name') if x.get('fn') else None}")

# decompile each function consumer
for x in xrefs:
    fn = x.get("fn")
    if not fn: continue
    addr = fn["addr"]
    print(f"\n===== {fn['name']} @ {addr} =====")
    dd = call("decompile", {"addr": addr})
    code = dd.get("code", "")
    if not code:
        print("  (no decompile)"); continue
    lines = code.splitlines()
    for i, ln in enumerate(lines):
        if any(k in ln for k in ("142D05410", "992", "3E0h", "1008", "div", "/ ", "w2s", "screen")):
            print(f"  {i}: {ln.strip()[:160]}")
