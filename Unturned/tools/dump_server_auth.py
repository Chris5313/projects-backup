#!/usr/bin/env python3
"""Dump the server.js sections needed for the username work."""
import importlib.util
import sys

spec = importlib.util.spec_from_file_location(
    "vps", r"C:/Users/Shadow/Documents/Projects/.cred/vps.py")
vps = importlib.util.module_from_spec(spec)
spec.loader.exec_module(vps)

c = vps.client()
try:
    out, _ = vps.run(c, "grep -n 'users\\|username\\|displayName\\|/api/auth\\|steamId\\|sessions' /opt/hamas-log-proxy/server.js | head -80")
    print(out)
finally:
    c.close()
