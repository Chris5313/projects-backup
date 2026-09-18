#!/usr/bin/env python3
"""Patch server.js:
1. sessions carry the user's chosen displayName
2. GET /api/me  +  POST /api/name (choose/change display name, per-hwid, unique)
3. GET /online returns [{s,name}] instead of bare steam ids
4. config ownership keyed by hwid (steamId stays as legacy fallback) -> fixes
   configs becoming undeletable when steamId is null
5. uploads stamped with the chosen username as author
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
    # ── sanity: read back the exact anchors ──
    checks = [
        "const w = RE_HWID.test(j.w||'') ? j.w : null;",
        "sessions.set(token, { w, s, sn, u, h, ip: clientIP, created: Date.now(), ghost: j.ghost === true });",
        "if (req.url === '/online' && req.method === 'GET') {",
        "author: session.sn || session.u || 'Anonymous',",
    ]
    ok = True
    for anchor in checks:
        out, _ = vps.run(c, "grep -cF " + shlex.quote(anchor) + " /opt/hamas-log-proxy/server.js")
        if out.strip() == "0":
            print("[!] ANCHOR MISSING:", anchor)
            ok = False
    if not ok:
        print("ABORT - anchors not found, nothing written")
        sys.exit(1)

    # ── read live file for surgical line edits ──
    out, _ = vps.run(c, "cat /opt/hamas-log-proxy/server.js")
    src = out

    # 1. session creation carries display name
    old_set = "sessions.set(token, { w, s, sn, u, h, ip: clientIP, created: Date.now(), ghost: j.ghost === true });"
    new_set = ("const dname = users[w] && users[w].dn ? users[w].dn : '';\n"
               "      sessions.set(token, { w, s, sn, u, h, dn: dname, ip: clientIP, created: Date.now(), ghost: j.ghost === true });")
    assert old_set in src, "session.set anchor missing"
    src = src.replace(old_set, new_set, 1)

    # 2. /online -> named users (kept token check intact)
    old_online = """      // Collect Steam IDs of all non-ghost online sessions
      const steamIds = [];
      for (const [t, s] of sessions) {
        if (s.ghost) continue;
        if (s.s) steamIds.push(s.s); // s.s is the Steam ID
      }
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ online: steamIds }));
      return;"""
    new_online = """      // Collect all non-ghost online sessions: steamid + chosen display name
      const onlineUsers = [];
      for (const [t, s] of sessions) {
        if (s.ghost) continue;
        if (!s.s) continue; // no steam id -> can't correlate in-game
        onlineUsers.push({ s: s.s, name: s.dn || s.sn || ('user-' + s.w.substring(0, 6)) });
      }
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ online: onlineUsers }));
      return;"""
    assert old_online in src, "online anchor missing"
    src = src.replace(old_online, new_online, 1)

    # 3. username endpoints + /api/me, right before the /online route
    username_block = r'''
    // ═══════════ GET /api/me — this session's identity (token, name, steam) ═══════════
    if (req.url === '/api/me' && req.method === 'GET') {
      const token = req.headers['x-session-token'];
      if (!token || !sessions.has(token)) { res.writeHead(401); res.end(); return; }
      const session = sessions.get(token);
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ w: session.w, s: session.s, name: session.dn || '' }));
      return;
    }

    // ═══════════ POST /api/name — choose / change your display name ═══════════
    // j: { name }. Per-hwid, unique across users. Backfills configs you already own.
    if (req.url === '/api/name' && req.method === 'POST') {
      const token = req.headers['x-session-token'];
      if (!token || !sessions.has(token)) { res.writeHead(401); res.end(); return; }
      const session = sessions.get(token);

      const raw = typeof j.name === 'string' ? j.name.trim() : '';
      if (!raw) { res.writeHead(400); res.end(JSON.stringify({ error: 'Name required' })); return; }
      const name = raw.replace(/[^a-zA-Z0-9_\-]/g, '').substring(0, 16);
      if (name.length < 3) { res.writeHead(400); res.end(JSON.stringify({ error: 'Name must be 3-16 chars (letters, numbers, _ -)' })); return; }

      const takenBy = Object.keys(users).find(hw => hw !== session.w && users[hw].dn && users[hw].dn.toLowerCase() === name.toLowerCase());
      if (takenBy) { res.writeHead(409); res.end(JSON.stringify({ error: 'Name already taken' })); return; }

      const now = new Date().toISOString();
      const usr = users[session.w] || (users[session.w] = { w: session.w, first_seen: now, last_seen: now, sessions: 0, steam_history: [], ip_history: [], username_history: [], hostname_history: [] });
      const prev = usr.dn || '';
      usr.dn = name;
      usr.dn_changed = now;
      usr.last_seen = now;
      addToHistory(usr.username_history, 'u', name, 10);
      saveUsers();

      // Backfill authorship on configs this hwid owns
      try {
        const index = loadConfigsIndex();
        let backfilled = 0;
        for (const k of Object.keys(index)) {
          if (index[k].hwid === session.w && index[k].author !== name) { index[k].author = name; backfilled++; }
        }
        if (backfilled > 0) saveConfigsIndex(index);
      } catch (e) {}

      session.dn = name;
      console.log('[' + now + '] NAME_SET ' + name + ' hwid=' + session.w + (prev ? ' (was: ' + prev + ')' : ' (first)'));
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ success: true, name }));
      return;
    }
'''
    marker = "    // ═══════════ GET /online — authenticated users get list of online Steam IDs ═══════════"
    assert marker in src, "online comment marker missing"
    src = src.replace(marker, username_block + "\n" + marker, 1)

    # 4. upload: stamp hwid + chosen username as author
    old_idx = """        index[name] = {
          author: session.sn || session.u || 'Anonymous',
          steamId: session.s,
          description: (j.description || '').substring(0, 200),"""
    new_idx = """        index[name] = {
          author: session.dn || session.sn || session.u || 'Anonymous',
          hwid: session.w,
          steamId: session.s,
          description: (j.description || '').substring(0, 200),"""
    assert old_idx in src, "upload index anchor missing"
    src = src.replace(old_idx, new_idx, 1)

    # 5. list: own = hwid match OR legacy steamId match; expose hwid
    old_own = "        own: sess && meta.steamId && meta.steamId === sess.s ? 1 : 0"
    new_own = "        hwid: meta.hwid || '',\n        own: sess && ((meta.hwid && meta.hwid === sess.w) || (!meta.hwid && meta.steamId && meta.steamId === sess.s)) ? 1 : 0"
    assert old_own in src, "list own anchor missing"
    src = src.replace(old_own, new_own, 1)

    # 6. delete ownership: hwid match OR legacy steamId match
    old_del = "      // Only owner can delete\n      if (index[name].steamId !== session.s) {"
    new_del = "      // Only owner can delete (hwid is the real key; steamId = legacy fallback)\n      const owns = (index[name].hwid && index[name].hwid === session.w) || (!index[name].hwid && index[name].steamId && index[name].steamId === session.s);\n      if (!owns) {"
    assert old_del in src, "delete ownership anchor missing"
    src = src.replace(old_del, new_del, 1)

    # 7. upload conflict check: same hwid parity
    old_conflict = "      if (index[name] && index[name].steamId !== session.s) {"
    new_conflict = "      const conflict = index[name] && !((index[name].hwid && index[name].hwid === session.w) || (!index[name].hwid && index[name].steamId && index[name].steamId === session.s));\n      if (conflict) {"
    assert old_conflict in src, "upload conflict anchor missing"
    src = src.replace(old_conflict, new_conflict, 1)

    # ── backup + upload + restart + verify ──
    vps.run(c, "cp /opt/hamas-log-proxy/server.js /opt/hamas-log-proxy/server.js.bak_usernames")
    sftp = c.open_sftp()
    with sftp.open("/opt/hamas-log-proxy/server.js", "w") as f:
        f.write(src)
    sftp.close()

    out, err = vps.run(c, "node --check /opt/hamas-log-proxy/server.js && systemctl restart hamas-log-proxy && sleep 1 && systemctl is-active hamas-log-proxy")
    print("RESTART:", out.strip(), err.strip())
finally:
    c.close()
