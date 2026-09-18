#!/usr/bin/env python3
"""Find the exact upload-handler anchor text (line endings / spacing)."""
import importlib.util
import sys
sys.path.insert(0, r"C:/Users/Shadow/Documents/Projects/Unturned/tools")
from patch_server_ig import sftp_read, SERVER, vps


def main():
    c = vps.client()
    try:
        src = sftp_read(c, SERVER)
        idx = src.find("const name = j.name.replace")
        if idx < 0:
            print("NOT FOUND")
            return
        chunk = src[idx - 200: idx + 400]
        print(repr(chunk))
    finally:
        c.close()


if __name__ == "__main__":
    main()
