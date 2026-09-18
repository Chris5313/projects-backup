#!/usr/bin/env python3
"""Probe the live /api/name endpoint with a real session to see the exact failure."""
import importlib.util
import json
import urllib.request
import ssl
import sys

spec = importlib.util.spec_from_file_location(
    "vps", r"C:/Users/Shadow/Documents/Projects/.cred/vps.py")
vps = importlib.util.module_from_spec(spec)
spec.loader.exec_module(vps)

c = vps.client()
try:
    # Get a valid license key from the server config to mint a session
    out, _ = vps.run(c, "grep -n 'license\\|LICENSE\\|/api/auth\\|apiKey\\|API_KEY' /opt/hamas-log-proxy/server.js | head -20")
    print("AUTH GREP:", out)

    out, _ = vps.run(c, "tail -30 /opt/hamas-log-proxy/*.log 2>/dev/null; journalctl -u hamas-log-proxy --no-pager -n 30 2>/dev/null | tail -30")
    print("LOGS:", out[:3000])
finally:
    c.close()
