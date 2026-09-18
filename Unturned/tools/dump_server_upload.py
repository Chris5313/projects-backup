#!/usr/bin/env python3
"""Dump server.js upload/delete handlers."""
import importlib.util

spec = importlib.util.spec_from_file_location(
    "vps", r"C:/Users/Shadow/Documents/Projects/.cred/vps.py")
vps = importlib.util.module_from_spec(spec)
spec.loader.exec_module(vps)

c = vps.client()
try:
    out, _ = vps.run(c, "sed -n '845,965p' /opt/hamas-log-proxy/server.js")
    print(out)
finally:
    c.close()
