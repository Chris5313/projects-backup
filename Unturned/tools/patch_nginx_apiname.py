#!/usr/bin/env python3
"""Add /api/me and /api/name location blocks to the nginx site config, reload."""
import importlib.util

spec = importlib.util.spec_from_file_location(
    "vps", r"C:/Users/Shadow/Documents/Projects/.cred/vps.py")
vps = importlib.util.module_from_spec(spec)
spec.loader.exec_module(vps)

BLOCK = """    location /api/me {
        proxy_pass http://127.0.0.1:3999/api/me;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header CF-Connecting-IP $http_cf_connecting_ip;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }
    location /api/name {
        proxy_pass http://127.0.0.1:3999/api/name;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header CF-Connecting-IP $http_cf_connecting_ip;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }
"""

ANCHOR = "    location / {\n        try_files"

c = vps.client()
try:
    out, _ = vps.run(c, "cat /etc/nginx/sites-enabled/hamasclient")
    src = out
    if "/api/me" in src:
        print("already patched")
    else:
        assert ANCHOR in src, "anchor missing"
        src = src.replace(ANCHOR, BLOCK + ANCHOR, 1)
        vps.run(c, "cp /etc/nginx/sites-enabled/hamasclient /etc/nginx/sites-enabled/hamasclient.bak_apiname")
        sftp = c.open_sftp()
        with sftp.open("/etc/nginx/sites-enabled/hamasclient", "w") as f:
            f.write(src)
        sftp.close()
        out, err = vps.run(c, "nginx -t 2>&1 && systemctl reload nginx && echo RELOADED")
        print(out.strip(), err.strip())
finally:
    c.close()
