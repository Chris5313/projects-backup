#!/usr/bin/env python3
"""Remove fake smoke-test user (hwid A1:B2:C3:D4:E5:F6) from users.json."""
import importlib.util
import json

spec = importlib.util.spec_from_file_location(
    "vps", r"C:/Users/Shadow/Documents/Projects/.cred/vps.py")
vps = importlib.util.module_from_spec(spec)
spec.loader.exec_module(vps)


def main():
    c = vps.client()
    try:
        out, _ = vps.run(
            c, "node -e \"const fs=require('fs');const p='/opt/hamas-log-proxy/users.json';const u=JSON.parse(fs.readFileSync(p,'utf8'));if(u['A1:B2:C3:D4:E5:F6']){delete u['A1:B2:C3:D4:E5:F6'];fs.writeFileSync(p,JSON.stringify(u,null,2));console.log('removed')}else{console.log('not found')}\"")
        print(out.strip())
    finally:
        c.close()


if __name__ == "__main__":
    main()
