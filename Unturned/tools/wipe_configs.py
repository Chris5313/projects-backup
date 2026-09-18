#!/usr/bin/env python3
"""Wipe all cloud configs from the VPS (files + index), with a safety backup."""
import importlib.util

spec = importlib.util.spec_from_file_location(
    "vps", r"C:/Users/Shadow/Documents/Projects/.cred/vps.py")
vps = importlib.util.module_from_spec(spec)
spec.loader.exec_module(vps)

c = vps.client()
try:
    # Safety backup first
    out, err = vps.run(c, "tar czf /opt/hamas-log-proxy/configs_wiped_backup_$(date +%s).tar.gz -C /opt/hamas-log-proxy configs && echo backup-ok")
    print(out.strip(), err.strip())

    # Remove config files + reset index
    out, err = vps.run(c, "rm -f /opt/hamas-log-proxy/configs/*.conf && echo '{}' > /opt/hamas-log-proxy/configs/index.json && echo wiped")
    print(out.strip(), err.strip())

    # Verify
    out, _ = vps.run(c, "ls -la /opt/hamas-log-proxy/configs/ && cat /opt/hamas-log-proxy/configs/index.json && curl -s http://127.0.0.1:3999/api/configs")
    print(out)
finally:
    c.close()
