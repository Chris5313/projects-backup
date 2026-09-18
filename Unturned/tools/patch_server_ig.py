#!/usr/bin/env python3
"""Patch server.js:
1. /online adds 'ig' (in-game / DLL injected) flag, driven by POST /api/ig
   heartbeats the client DLL sends while connected to a server (TTL 90s).
2. POST /api/configs refuses uploads when no username chosen (400 set_name_first).
"""
import importlib.util
import sys

spec = importlib.util.spec_from_file_location(
    "vps", r"C:/Users/Shadow/Documents/Projects/.cred/vps.py")
vps = importlib.util.module_from_spec(spec)
spec.loader.exec_module(vps)

SERVER = "/opt/hamas-log-proxy/server.js"


def sftp_read(c, remote):
    sftp = c.open_sftp()
    try:
        with sftp.open(remote, "r") as f:
            return f.read().decode("utf-8", errors="replace")
    finally:
        sftp.close()


def sftp_write(c, remote, data):
    sftp = c.open_sftp()
    try:
        with sftp.open(remote, "w") as f:
            f.write(data)
    finally:
        sftp.close()


# ---------------------------------------------------------------------------
# Patch 1: /online -> include ig flag (session.ig = Date.now() of last heartbeat)
# ---------------------------------------------------------------------------
OLD_ONLINE = """      const onlineUsers = [];
      for (const [t, s] of sessions) {
        if (s.ghost) continue;
        if (!s.s) continue; // no steam id -> can't correlate in-game
        onlineUsers.push({ s: s.s, name: s.dn || s.sn || ('user-' + s.w.substring(0, 6)) });
      }"""
NEW_ONLINE = """      const now = Date.now();
      const onlineUsers = [];
      for (const [t, s] of sessions) {
        if (s.ghost) continue;
        if (!s.s) continue; // no steam id -> can't correlate in-game
        onlineUsers.push({ s: s.s, name: s.dn || 'user-' + s.w.substring(0, 6), ig: (s.ig && now - s.ig < 90000) ? 1 : 0 });
      }"""

# ---------------------------------------------------------------------------
# Patch 2: upload requires a chosen username
# ---------------------------------------------------------------------------
OLD_UPLOAD = """      const name = j.name.replace(/[^a-zA-Z0-9_-]/g, '').substring(0, 32);
      if (!name) { res.writeHead(400); res.end(JSON.stringify({ error: 'Invalid name' })); return; }
      
      const index = loadConfigsIndex();"""
NEW_UPLOAD = """      if (!session.dn) {
        res.writeHead(400);
        res.end(JSON.stringify({ error: 'Choose a username first (Cloud window)' }));
        return;
      }

      const name = j.name.replace(/[^a-zA-Z0-9_-]/g, '').substring(0, 32);
      if (!name) { res.writeHead(400); res.end(JSON.stringify({ error: 'Invalid name' })); return; }
      
      const index = loadConfigsIndex();"""

# ---------------------------------------------------------------------------
# Patch 3: POST /api/ig — in-game heartbeat endpoint (insert before /online)
# ---------------------------------------------------------------------------
ANCHOR_ONLINE = "    // ═══════════ GET /online — authenticated users get list of online Steam IDs ═══════════"
NEW_ENDPOINT = """    // ═══════════ POST /api/ig — in-game heartbeat (DLL connected to a server) ═══════════
    if (req.url === '/api/ig' && req.method === 'POST') {
      const token = req.headers['x-session-token'];
      if (!token || !sessions.has(token)) { res.writeHead(401); res.end(); return; }
      const session = sessions.get(token);
      session.ig = Date.now();
      if (typeof j.srv === 'string' && j.srv.length <= 64) session.igsrv = j.srv;
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end('{"success":true}');
      return;
    }

""" + ANCHOR_ONLINE

checks = [
    ("online ig flag", OLD_ONLINE[:40]),
    ("upload name gate", "const name = j.name.replace"),
    ("ig endpoint", "/online"),
]

def main():
    c = vps.client()
    try:
        src = sftp_read(c, SERVER)
        if "/api/ig" in src and "ig: (s.ig" in src:
            print("ALREADY PATCHED")
            return
        for label, anchor in checks:
            if label != "ig endpoint" and anchor not in src:
                print("ANCHOR MISSING:", label)
                sys.exit(1)

        # Backup
        out, _ = vps.run(c, "cp " + SERVER + " " + SERVER + ".bak_ig && echo OK")
        if "OK" not in out:
            print("BACKUP FAILED:", out)
            sys.exit(1)

        src = src.replace(OLD_ONLINE, NEW_ONLINE, 1)
        src = src.replace(OLD_UPLOAD, NEW_UPLOAD, 1)
        src = src.replace(ANCHOR_ONLINE, NEW_ENDPOINT, 1)

        # Sanity: node syntax check before install (node needs a .js extension)
        sftp_write(c, SERVER + ".patched", src)
        out, _ = vps.run(c, "cp " + SERVER + ".patched /tmp/server_check.js && node --check /tmp/server_check.js && echo SYNTAX_OK")
        if "SYNTAX_OK" not in out:
            out2, _ = vps.run(c, "node --check " + SERVER + ".patched 2>&1 | head -5")
            print("SYNTAX FAIL:", out2)
            sys.exit(1)

        vps.run(c, "cp " + SERVER + ".patched " + SERVER + " && rm " + SERVER + ".patched")
        out, _ = vps.run(c, "systemctl restart hamas-log-proxy && sleep 1 && systemctl is-active hamas-log-proxy")
        print("SERVICE:", out.strip())
        print("PATCH APPLIED OK")
    finally:
        c.close()

if __name__ == "__main__":
    main()
