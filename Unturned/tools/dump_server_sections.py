#!/usr/bin/env python3
"""Dump specific server.js sections for the username work."""
import importlib.util

spec = importlib.util.spec_from_file_location(
    "vps", r"C:/Users/Shadow/Documents/Projects/.cred/vps.py")
vps = importlib.util.module_from_spec(spec)
spec.loader.exec_module(vps)

c = vps.client()
try:
    out, _ = vps.run(c, "sed -n '735,800p' /opt/hamas-log-proxy/server.js")
    print("=== AUTH (735-800) ===")
    print(out)
    out, _ = vps.run(c, "sed -n '820,845p' /opt/hamas-log-proxy/server.js")
    print("=== LIST (820-845) ===")
    print(out)
    out, _ = vps.run(c, "sed -n '650,670p' /opt/hamas-log-proxy/server.js")
    print("=== ONLINE (650-670) ===")
    print(out)
finally:
    c.close()
