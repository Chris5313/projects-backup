#!/usr/bin/env python3
"""Upload gw2_payload.dll to the VPS via streamed SFTP write (server rejects 'wb' mode).

Backs up the current DLL, uploads the new one, verifies MD5 remotely, restores
backup on mismatch.
"""
import hashlib
import sys

sys.path.insert(0, r"C:/Users/Shadow/Documents/Projects/.cred")
import paramiko  # noqa: E402

HOST = "206.245.132.174"
USER = "root"
KEY = r"C:/Users/Shadow/.ssh/id_vps"
LOCAL = r"C:/Users/Shadow/Documents/Projects/PVZGW2/payload/build_release/gw2_payload.dll"
REMOTE = "/opt/hamas-log-proxy/files/gw2_payload.dll"
BACKUP = "/opt/hamas-log-proxy/files/gw2_payload.dll.pre_v36"

data = open(LOCAL, "rb").read()
md5_local = hashlib.md5(data).hexdigest()
print(f"local: {len(data)} bytes md5={md5_local}")

c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
c.connect(HOST, username=USER, key_filename=KEY, timeout=20)

def run(cmd):
    _, out, err = c.exec_command(cmd, timeout=60)
    return out.read().decode(errors="replace"), err.read().decode(errors="replace")

# 1. backup current
o, _ = run(f"cp -f {REMOTE} {BACKUP} 2>/dev/null; ls -la {BACKUP}")
print("backup:", o.strip() or "(none existed)")

# 2. streamed upload
sftp = c.open_sftp()
with sftp.open(REMOTE, "w") as f:
    f.write(data)
sftp.close()

# 3. verify
o, _ = run(f"md5sum {REMOTE} && stat -c %s {REMOTE}")
remote_md5 = o.split()[0] if o.split() else "?"
remote_size = o.split()[1] if len(o.split()) > 1 else "?"
print(f"remote: {remote_size} bytes md5={remote_md5}")

if remote_md5 == md5_local:
    run(f"chmod 644 {REMOTE}")
    print("OK — v35 deployed, backup kept at gw2_payload.dll.pre_v36")
else:
    run(f"cp -f {BACKUP} {REMOTE}")
    print("MISMATCH — previous DLL restored, do not inject")
    sys.exit(1)

c.close()
