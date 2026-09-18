import sys
sys.path.insert(0, r'C:/Users/Shadow/Documents/Projects/.cred')
import paramiko, vps

c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
kw = {'key_filename': vps.KEY} if getattr(vps, 'KEY', None) else {'password': getattr(vps, 'PASSWORD', None)}
c.connect(vps.HOST, username=vps.USER, **kw)

def run(cmd):
    i, o, e = c.exec_command(cmd)
    out = o.read().decode('utf-8', 'replace').strip()
    err = e.read().decode('utf-8', 'replace').strip()
    return out, err

# 1) backup
print(run("cp -f /opt/hamas-log-proxy/server.js /opt/hamas-log-proxy/server.js.bak_ownflag && echo backed-up")[0])

# 2) patch the list endpoint: add ownership flag (1/0 — client parser reads strings/numbers only)
script = r'''
import sys
p = '/opt/hamas-log-proxy/server.js'
src = open(p, encoding='utf-8').read()

old = """    // GET /api/configs - list all public configs
    if (req.method === 'GET' && req.url === '/api/configs') {
      const index = loadConfigsIndex();
      const list = Object.entries(index).map(([name, meta]) => ({
        name,
        author: meta.author,
        description: meta.description || '',
        downloads: meta.downloads || 0,
        created: meta.created,
        size: meta.size
      }));"""

new = """    // GET /api/configs - list all public configs
    if (req.method === 'GET' && req.url === '/api/configs') {
      const index = loadConfigsIndex();
      // Optional auth: if a valid session token is present, mark which
      // configs belong to this user so the client can offer Delete on them.
      const tok = req.headers['x-session-token'];
      const sess = tok && sessions.has(tok) ? sessions.get(tok) : null;
      const list = Object.entries(index).map(([name, meta]) => ({
        name,
        author: meta.author,
        description: meta.description || '',
        downloads: meta.downloads || 0,
        unturned: meta.steamId || '',
        created: meta.created,
        size: meta.size,
        own: sess && meta.steamId && meta.steamId === sess.s ? 1 : 0
      }));"""

if old not in src:
    print('PATCH FAILED: anchor not found')
    sys.exit(1)

open(p, 'w', encoding='utf-8').write(src.replace(old, new, 1))
print('PATCH OK')
'''

out, err = run("python3 - " + " <<'PYEOF'\n" + script + "PYEOF")
print(out, err)
if 'PATCH OK' not in out:
    print('ABORT')
    c.close()
    sys.exit(1)

# 3) syntax check, restart, verify
out, err = run("node --check /opt/hamas-log-proxy/server.js && echo SYNTAX-OK")
print(out, err)
if 'SYNTAX-OK' not in out:
    print('restoring backup...')
    print(run("cp -f /opt/hamas-log-proxy/server.js.bak_ownflag /opt/hamas-log-proxy/server.js && echo restored")[0])
    c.close()
    sys.exit(1)

print('SERVICE:', run("systemctl restart hamas-log-proxy && sleep 1 && systemctl is-active hamas-log-proxy")[0])
print('VERIFY:', run("curl -s http://127.0.0.1:3999/api/configs | head -c 400")[0])
c.close()
print('DONE')
