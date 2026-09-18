#!/usr/bin/env python3
"""Full smoke test on the VPS: auth -> name -> heartbeat -> online ig flag -> upload gate."""
import importlib.util

spec = importlib.util.spec_from_file_location(
    "vps", r"C:/Users/Shadow/Documents/Projects/.cred/vps.py")
vps = importlib.util.module_from_spec(spec)
spec.loader.exec_module(vps)

CMD = r'''
set -e
PORT=$(ss -tlnp | grep node | grep -oP ':\K[0-9]+' | head -1)
KEY=$(node -e "console.log(JSON.parse(require('fs').readFileSync('/opt/hamas-log-proxy/license.json','utf8')).key)")
KH=$(printf '%s' "$KEY" | sha256sum | cut -d' ' -f1)

TOK=$(curl -s -X POST http://127.0.0.1:$PORT/auth -H 'Content-Type: application/json' \
  -d "{\"lv\":6,\"kh\":\"$KH\",\"w\":\"A1:B2:C3:D4:E5:F6\",\"s\":\"76561198000000001\",\"sn\":\"SmokeTester\",\"u\":\"smoketest\",\"h\":\"smoke-host\"}" \
  | node -e "let d='';process.stdin.on('data',c=>d+=c).on('end',()=>{const j=JSON.parse(d);console.log(j.token||'FAIL:'+d)})")
case "$TOK" in FAIL*) echo "$TOK"; exit 1;; esac
echo "TOKEN OK"

H=(-H "Content-Type: application/json" -H "X-Session-Token: $TOK")

echo "-- upload WITHOUT name (expect 400 set_name_first):"
curl -s -w ' [%{http_code}]' -X POST http://127.0.0.1:$PORT/api/configs \
  "${H[@]}" -d '{"name":"smoketest","data":"aGk="}'; echo

echo "-- set name:"
curl -s -X POST http://127.0.0.1:$PORT/api/name "${H[@]}" -d '{"name":"SmokeTester1"}'; echo

echo "-- upload now (expect success):"
curl -s -X POST http://127.0.0.1:$PORT/api/configs "${H[@]}" -d '{"name":"smoketest","data":"aGk="}'; echo

echo "-- heartbeat:"
curl -s -X POST http://127.0.0.1:$PORT/api/ig "${H[@]}" -d '{"srv":"smoke-server"}'; echo

echo "-- /online (expect ig:1 for our sid):"
curl -s http://127.0.0.1:$PORT/online -H "X-Session-Token: $TOK"; echo

echo "-- cleanup:"
curl -s -X DELETE http://127.0.0.1:$PORT/api/configs/smoketest "${H[@]}"; echo
'''

c = vps.client()
try:
    out, err = vps.run(c, CMD, timeout=90)
    print(out)
    if err.strip():
        print("[stderr]", err[:500])
finally:
    c.close()
