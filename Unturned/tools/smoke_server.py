#!/usr/bin/env python3
"""Smoke-test the new username endpoints."""
import importlib.util

spec = importlib.util.spec_from_file_location(
    "vps", r"C:/Users/Shadow/Documents/Projects/.cred/vps.py")
vps = importlib.util.module_from_spec(spec)
spec.loader.exec_module(vps)

c = vps.client()
try:
    for cmd in [
        "curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:3999/api/me",
        "curl -s -o /dev/null -w '%{http_code}' -X POST -H 'Content-Type: application/json' -d '{\"name\":\"abc\"}' http://127.0.0.1:3999/api/name",
        "curl -s http://127.0.0.1:3999/api/configs | head -c 500",
        "grep -n 'dn\\|api/name\\|api/me' /opt/hamas-log-proxy/server.js | head -20",
    ]:
        out, err = vps.run(c, cmd)
        print("$", cmd[:60])
        print(out.strip() or err.strip())
        print()
finally:
    c.close()
