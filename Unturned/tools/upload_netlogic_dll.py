#!/usr/bin/env python3
"""Upload the freshly built HAMASCLIENT.dll to the VPS with backup + MD5 verify."""
import hashlib
import importlib.util
import os

spec = importlib.util.spec_from_file_location(
    "vps", r"C:/Users/Shadow/Documents/Projects/.cred/vps.py")
vps = importlib.util.module_from_spec(spec)
spec.loader.exec_module(vps)

LOCAL = r"C:/Users/Shadow/Documents/Projects/Unturned/dll-src/dll-src/HAMASCLIENT/bin/Release/HAMASCLIENT.dll"
REMOTE = "/opt/hamas-log-proxy/files/HAMASCLIENT.dll"


def md5(path):
    h = hashlib.md5()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 16), b""):
            h.update(chunk)
    return h.hexdigest()


def main():
    local_md5 = md5(LOCAL)
    print("local  md5:", local_md5)
    size = os.path.getsize(LOCAL)
    print("local  size:", size)

    c = vps.client()
    try:
        # Backup current
        out, _ = vps.run(c, "cp " + REMOTE + " " + REMOTE + ".pre_netlogic && echo OK")
        print("backup:", out.strip())

        # Streamed upload (plain sftp put is rejected by this server path)
        sftp = c.open_sftp()
        with open(LOCAL, "rb") as f:
            with sftp.open(REMOTE, "wb") as r:
                while True:
                    chunk = f.read(1 << 16)
                    if not chunk:
                        break
                    r.write(chunk)
        sftp.close()

        out, _ = vps.run(c, "md5sum " + REMOTE)
        remote_md5 = out.split()[0] if out.split() else "?"
        print("remote md5:", remote_md5)
        if remote_md5 != local_md5:
            print("MISMATCH - restoring backup")
            vps.run(c, "cp " + REMOTE + ".pre_netlogic " + REMOTE)
            raise SystemExit(1)
        print("UPLOAD VERIFIED")
    finally:
        c.close()


if __name__ == "__main__":
    main()
