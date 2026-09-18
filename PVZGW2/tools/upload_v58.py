#!/usr/bin/env python3
"""Upload gw2_payload.dll to the VPS and verify MD5."""
import importlib.util, hashlib, os

spec = importlib.util.spec_from_file_location(
    "vps", r"C:/Users/Shadow/Documents/Projects/.cred/vps.py")
vps = importlib.util.module_from_spec(spec)
spec.loader.exec_module(vps)

DLL = r"C:/Users/Shadow/Documents/Projects/PVZGW2/payload/build_release/gw2_payload.dll"
REMOTE = "/opt/hamas-log-proxy/files/gw2_payload.dll"

md5 = hashlib.md5(open(DLL, "rb").read()).hexdigest()
print("local  md5:", md5[:8], "size:", os.path.getsize(DLL))

c = vps.client()
try:
    sftp = c.open_sftp()
    sftp.put(DLL, REMOTE)
    sftp.close()
    out, _ = vps.run(c, f"md5sum {REMOTE}")
    remote_md5 = out.strip().split()[0] if out.strip() else ""
    print("remote md5:", remote_md5[:8])
    print("MATCH" if remote_md5 == md5 else "MISMATCH!")
finally:
    c.close()
