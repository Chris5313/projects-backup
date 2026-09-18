#!/usr/bin/env python3
"""Patch server.js (round 2):
1. author stamping: session.dn only (no steam-name fallback)
2. /api/me: add fresh:1 when no name chosen yet
3. /api/name: backfill legacy configs (steamId-owned, no hwid) to the new username
"""
import importlib.util
import shlex
import sys

spec = importlib.util.spec_from_file_location(
    "vps", r"C:/Users/Shadow/Documents/Projects/.cred/vps.py")
vps = importlib.util.module_from_spec(spec)
spec.loader.exec_module(vps)

c = vps.client()
try:
    out, _ = vps.run(c, "cat /opt/hamas-log-proxy/server.js")
    src = out

    # ── 1. author stamping: dn only ──
    old_author = "author: session.dn || session.sn || session.u || 'Anonymous',"
    new_author = "author: session.dn || 'Anonymous',"
    assert old_author in src, "author anchor missing"
    src = src.replace(old_author, new_author, 1)

    # ── 2. /api/me fresh flag ──
    old_me = "res.end(JSON.stringify({ w: session.w, s: session.s, name: session.dn || '' }));"
    new_me = ("const fresh = !session.dn;\n"
              "      res.end(JSON.stringify({ w: session.w, s: session.s, name: session.dn || '', fresh: fresh ? 1 : 0 }));")
    assert old_me in src, "/api/me anchor missing"
    src = src.replace(old_me, new_me, 1)

    # ── 3. backfill legacy configs on name set ──
    old_bf = "        for (const k of Object.keys(index)) {\n          if (index[k].hwid === session.w && index[k].author !== name) { index[k].author = name; backfilled++; }\n        }"
    new_bf = "        for (const k of Object.keys(index)) {\n          const m = index[k];\n          const mine = (m.hwid && m.hwid === session.w) || (!m.hwid && m.steamId && m.steamId === session.s);\n          if (mine && m.author !== name) { m.author = name; backfilled++; }\n        }"
    assert old_bf in src, "backfill anchor missing"
    src = src.replace(old_bf, new_bf, 1)

    vps.run(c, "cp /opt/hamas-log-proxy/server.js /opt/hamas-log-proxy/server.js.bak_names2")
    sftp = c.open_sftp()
    with sftp.open("/opt/hamas-log-proxy/server.js", "w") as f:
        f.write(src)
    sftp.close()

    out, err = vps.run(c, "node --check /opt/hamas-log-proxy/server.js && systemctl restart hamas-log-proxy && sleep 1 && systemctl is-active hamas-log-proxy")
    print("RESTART:", out.strip(), err.strip())

    # backfill the two legacy configs right away (sa has steamId null -> stays; se was uploaded by this hwid's steam account)
    out, err = vps.run(c, "cat /opt/hamas-log-proxy/configs/index.json 2>/dev/null || ls /opt/hamas-log-proxy/ | head -20")
    print("INDEX:", out[:500], err[:200])
finally:
    c.close()
