#!/usr/bin/env python3
"""Deploy v12 (freeze fix) payload: local embed copy + VPS upload + md5 verify."""
import hashlib
import shutil
import subprocess
import sys

SRC = r"C:\Users\Shadow\Documents\Projects\PVZGW2\payload\build_release\gw2_payload.dll"
EMBED = r"C:\Users\Shadow\Documents\Projects\Unturned\HamasLoader\embed\gw2_payload.dll"
VPS_PY = r"C:\Users\Shadow\Documents\Projects\.cred\vps.py"
REMOTE = "/opt/hamas-log-proxy/files/gw2_payload.dll"


def md5(path):
    h = hashlib.md5()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def main():
    src_md5 = md5(SRC)
    print(f"local  {SRC}\n       md5 {src_md5}")

    shutil.copyfile(SRC, EMBED)
    emb_md5 = md5(EMBED)
    status = "OK" if emb_md5 == src_md5 else "MISMATCH!"
    print(f"embed  {EMBED}\n       md5 {emb_md5}  [{status}]")
    if emb_md5 != src_md5:
        sys.exit(1)

    r = subprocess.run(
        [sys.executable, VPS_PY, "--put", SRC, REMOTE],
        capture_output=True, text=True, timeout=120,
    )
    print(r.stdout.strip())
    if r.returncode != 0:
        print(r.stderr.strip())
        sys.exit(1)

    r = subprocess.run(
        [sys.executable, VPS_PY, f"md5sum {REMOTE}"],
        capture_output=True, text=True, timeout=120,
    )
    out = r.stdout.strip()
    print(f"vps    {REMOTE}\n       {out}")
    if src_md5 not in out:
        print("VPS MISMATCH!")
        sys.exit(1)

    print("\ndeployed: v12 freeze-fix payload live on VPS + loader embed")


if __name__ == "__main__":
    main()
