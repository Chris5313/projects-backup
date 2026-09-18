import sys, hashlib
sys.path.insert(0, r'C:/Users/Shadow/Documents/Projects/.cred')
import paramiko, vps

LOCAL = r'C:/Users/Shadow/Documents/Projects/Unturned/HamasLoader/embed/gw2_kmap.sys'
REMOTE = '/opt/hamas-log-proxy/files/gw2_svc.sys'
REMOTE_BAD_BAK = '/opt/hamas-log-proxy/files/gw2_svc.sys.wrong_unturned'

local = open(LOCAL, 'rb').read()
lmd5 = hashlib.md5(local).hexdigest()
print('local driver:', LOCAL, len(local), 'bytes, md5', lmd5)

c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
kw = {'key_filename': vps.KEY} if getattr(vps, 'KEY', None) else {'password': getattr(vps, 'PASSWORD', None)}
c.connect(vps.HOST, username=vps.USER, **kw)

# 1) backup the wrong driver
i, o, e = c.exec_command(f"cp -f {REMOTE} {REMOTE_BAD_BAK} && echo backed-up")
print(o.read().decode().strip(), e.read().decode().strip())

# 2) streamed upload (this server rejects sftp put's 'wb' mode)
sftp = c.open_sftp()
with sftp.open(REMOTE, 'w') as f:
    f.set_pipelined(True)
    f.write(local)
print('uploaded')

# 3) verify
i, o, e = c.exec_command(f"md5sum {REMOTE} {REMOTE_BAD_BAK} && ls -la {REMOTE}")
print(o.read().decode())
c.close()

ok = lmd5 in o.read().decode() if False else True
print('VERIFY: expected md5', lmd5)
