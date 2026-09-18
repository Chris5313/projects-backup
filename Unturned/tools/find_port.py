#!/usr/bin/env python3
"""Find server port + re-run smoke tests."""
import importlib.util

spec = importlib.util.spec_from_file_location(
    "vps", r"C:/Users/Shadow/Documents/Projects/.cred/vps.py")
vps = importlib.util.module_from_spec(spec)
spec.loader.exec_module(vps)

c = vps.client()
try:
    out, _ = vps.run(c, "grep -n 'listen\\|PORT\\|443\\|createServer' /opt/hamas-log-proxy/server.js | head -10")
    print("PORT GREP:", out)
    out, _ = vps.run(c, "ss -tlnp | grep -i node | head -5")
    print("LISTEN:", out)
finally:
    c.close()
