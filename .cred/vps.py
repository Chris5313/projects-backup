#!/usr/bin/env python3
"""VPS helper — run commands on the GW2 VPS (israeliclient.xyz / 206.245.132.174).

Usage:
  vps.py "command"                    # run command, print output
  vps.py --ls                         # list /opt/hamas-log-proxy/files/
  vps.py --get <remote> <local>       # sftp get
  vps.py --put <local> <remote>       # sftp put
"""
import sys
import paramiko

HOST = "206.245.132.174"
USER = "root"
KEY = r"C:\Users\Shadow\.ssh\id_vps"


def client():
    c = paramiko.SSHClient()
    c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    c.connect(HOST, username=USER, key_filename=KEY, timeout=20)
    return c


def run(c, cmd, timeout=120):
    _, out, err = c.exec_command(cmd, timeout=timeout)
    o = out.read().decode(errors="replace")
    e = err.read().decode(errors="replace")
    return o, e


def main():
    args = sys.argv[1:]
    c = client()
    try:
        if not args:
            o, e = run(c, "uname -a && uptime")
            print(o, e)
        elif args[0] == "--ls":
            o, e = run(c, "ls -la /opt/hamas-log-proxy/files/ 2>/dev/null; echo ---; ls /opt/ 2>/dev/null")
            print(o, e)
        elif args[0] == "--get":
            sftp = c.open_sftp()
            sftp.get(args[1], args[2])
            print(f"got {args[1]} -> {args[2]}")
        elif args[0] == "--put":
            sftp = c.open_sftp()
            sftp.put(args[1], args[2])
            print(f"put {args[1]} -> {args[2]}")
        else:
            o, e = run(c, " ".join(args), timeout=600)
            print(o)
            if e.strip():
                print("[stderr]", e, file=sys.stderr)
    finally:
        c.close()


if __name__ == "__main__":
    main()
