#!/usr/bin/env python3
"""Run /api/name end-to-end test ON the VPS (node script over SSH)."""
import importlib.util
import json

spec = importlib.util.spec_from_file_location(
    "vps", r"C:/Users/Shadow/Documents/Projects/.cred/vps.py")
vps = importlib.util.module_from_spec(spec)
spec.loader.exec_module(vps)

NODE_SCRIPT = r'''
const http = require('http');
const crypto = require('crypto');
const fs = require('fs');

function req(method, url, body, token) {
  return new Promise((resolve, reject) => {
    const data = body ? JSON.stringify(body) : null;
    const r = http.request({ host: '127.0.0.1', port: 3999, path: url, method,
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
  const keyHash = crypto.createHash('sha256').update(lic.key).digest('hex');

  const a1 = await req('POST', '/api/auth', { w: 'test-hwid-0001', s: '76561199000000001', sn: 'TestSteam', u: 'testuser', h: 'testhost', ghost: false, k: keyHash });
  console.log('AUTH1:', a1.status, a1.body.slice(0, 120));
  const tok1 = JSON.parse(a1.body).token;

  console.log('ME BEFORE:', JSON.stringify(await req('GET', '/api/me', null, tok1)));

  const ns = await req('POST', '/api/name', { name: 'TesterOne' }, tok1);
  console.log('NAME SET:', ns.status, ns.body.slice(0, 200));

  console.log('ME AFTER:', JSON.stringify(await req('GET', '/api/me', null, tok1)));

  console.log('ONLINE:', (await req('GET', '/online', null, tok1)).body.slice(0, 300));

  const a2 = await req('POST', '/api/auth', { w: 'test-hwid-0002', s: '76561199000000002', sn: 'TestSteam2', u: 'testuser2', h: 'testhost2', ghost: false, k: keyHash });
  const tok2 = JSON.parse(a2.body).token;
  const dup = await req('POST', '/api/name', { name: 'testerone' }, tok2);
  console.log('DUP (expect 409):', dup.status, dup.body.slice(0, 150));

  const bad = await req('POST', '/api/name', { name: 'ab' }, tok2);
  console.log('TOO SHORT (expect 400):', bad.status, bad.body.slice(0, 150));

  // cleanup test users
  const u = JSON.parse(fs.readFileSync('/opt/hamas-log-proxy/users.json', 'utf8'));
  delete u['test-hwid-0001']; delete u['test-hwid-0002'];
  fs.writeFileSync('/opt/hamas-log-proxy/users.json', JSON.stringify(u, null, 2));
  console.log('CLEANUP: done');
})().catch(e => { console.error('FAIL:', e.message); process.exit(1); });
'''

c = vps.client()
try:
    sftp = c.open_sftp()
    with sftp.open("/tmp/test_name.js", "w") as f:
        f.write(NODE_SCRIPT)
    sftp.close()
    out, err = vps.run(c, "node /tmp/test_name.js; rm -f /tmp/test_name.js", timeout=60)
    print(out)
    if err.strip():
        print("STDERR:", err[:1500])
finally:
    c.close()
