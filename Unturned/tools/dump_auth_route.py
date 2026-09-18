#!/usr/bin/env python3
"""Dump the /api/auth route entry conditions."""
import importlib.util

spec = importlib.util.spec_from_file_location(
    "vps", r"C:/Users/Shadow/Documents/Projects/.cred/vps.py")
vps = importlib.util.module_from_spec(spec)
spec.loader.exec_module(vps)

c = vps.client()
try:
    out, _ = vps.run(c, "grep -n \"api/auth\\|'/auth'\\|OUTDATED\\|404\" /opt/hamas-log-proxy/server.js | head -30")
    print("ROUTES:", out)
    out, _ = vps.run(c, "sed -n '700,742p' /opt/hamas-log-proxy/server.js")
    print("=== AUTH ENTRY ===")
    print(out)
finally:
    c.close()
