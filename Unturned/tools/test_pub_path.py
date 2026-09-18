#!/usr/bin/env python3
"""Test the PUBLIC endpoint path (https://israeliclient.xyz) exactly as the client uses it."""
import importlib.util

spec = importlib.util.spec_from_file_location(
    "vps", r"C:/Users/Shadow/Documents/Projects/.cred/vps.py")
vps = importlib.util.module_from_spec(spec)
spec.loader.exec_module(vps)

NODE_SCRIPT = r'''
const https = require('https');
const crypto = require('crypto');

function req(method, path, body, token) {
  return new Promise((resolve, reject) => {
    const data = body ? JSON.stringify(body) : null;
    const r = https.request({ host: 'israeliclient.xyz', port: 443, path, method,
      headers: Object.assign({ 'Content-Type': 'application/json' },
        data ? { 'Content-Length': Buffer.byteLength(data) } : {},
        token ? { 'X-Session-Token': token } : {}) }, res => {
      let b = ''; res.on('data', d => b += d);
      res.on('end', () => resolve({ status: res.statusCode, body: b }));
    });
    r.on('error', reject);
    if (data) r.write(data);
    r.end();
  });
}

(async () => {
  const lic = JSON.parse(fs.readFileSync('/opt/hamas-log-proxy/license.json', 'utf8'));
  const kh = crypto.createHash('sha256').update(lic.key).digest('hex');

  // auth exactly like the loader (lv + kh)
  const a = await req('POST', '/api/auth', {
    w: 'AA:BB:CC:DD:EE:F1', s: '76561199000000001', sn: 'TestSteam',
    u: 'testuser', h: 'testhost', ghost: false, lv: 6, kh
  });
  console.log('PUB AUTH:', a.status, a.body.slice(0, 150));
  let tok = null;
  try { tok = JSON.parse(a.body).token; } catch (e) {}

  if (!tok) { console.log('NO TOKEN - stopping'); return; }

  console.log('PUB ME:', (await req('GET', '/api/me', null, tok)).status);

  const nm = await req('POST', '/api/name', { name: 'TesterOne' }, tok);
  console.log('PUB NAME SET:', nm.status, nm.body.slice(0, 200));

  const me2 = await req('GET', '/api/me', null, tok);
  console.log('PUB ME AFTER:', me2.status, me2.body.slice(0, 150));

  // upload a config as this test user, then check own flag on list
  const up = await req('POST', '/api/configs', { name: 'owntest', description: 't', data: Buffer.from('test').toString('base64') }, tok);
  console.log('PUB UPLOAD:', up.status, up.body.slice(0, 120));
  const list = await req('GET', '/api/configs', null, tok);
  const found = JSON.parse(list.body).configs.find(c => c.name === 'owntest');
  console.log('PUB LIST OWN (expect 1):', found ? found.own : 'NOT FOUND');

  // delete it again
  const del = await req('DELETE', '/api/configs/owntest', null, tok);
  console.log('PUB DELETE:', del.status);

  // cleanup test user
  const u = JSON.parse(fs.readFileSync('/opt/hamas-log-proxy/users.json', 'utf8'));
  delete u['AA:BB:CC:DD:EE:F1'];
  fs.writeFileSync('/opt/hamas-log-proxy/users.json', JSON.stringify(u, null, 2));
  console.log('CLEANUP: done');
})().catch(e => { console.error('FAIL:', e.message); process.exit(1); });
'''
# fs is needed in the script
NODE_SCRIPT = "const fs = require('fs');\n" + NODE_SCRIPT

c = vps.client()
try:
    sftp = c.open_sftp()
    with sftp.open("/tmp/test_pub.js", "w") as f:
        f.write(NODE_SCRIPT)
    sftp.close()
    out, err = vps.run(c, "node /tmp/test_pub.js; rm -f /tmp/test_pub.js", timeout=90)
    print(out)
    if err.strip():
        print("STDERR:", err[:1200])
finally:
    c.close()
