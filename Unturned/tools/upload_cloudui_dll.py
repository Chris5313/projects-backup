import sys, hashlib
sys.path.insert(0, r'C:/Users/Shadow/Documents/Projects/.cred')
import paramiko, vps

LOCAL = r'C:/Users/Shadow/Documents/Projects/Unturned/dll-src/dll-src/HAMASCLIENT/bin/Release/HAMASCLIENT.dll'
REMOTE = '/opt/hamas-log-proxy/files/HAMASCLIENT.dll'
BAK = '/opt/hamas-log-proxy/files/HAMASCLIENT.dll.pre_cloudui2'

local = open(LOCAL, 'rb').read()
lmd5 = hashlib.md5(local).hexdigest()
print('local:', len(local), 'bytes, md5', lmd5)

c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
kw = {'key_filename': vps.KEY} if getattr(vps, 'KEY', None) else {'password': getattr(vps, 'PASSWORD', None)}
c.connect(vps.HOST, username=vps.USER, **kw)

def run(cmd):
    i, o, e = c.exec_command(cmd)
    return o.read().decode('utf-8', 'replace').strip(), e.read().decode('utf-8', 'replace').strip()

# 1) backup current
print(run(f"cp -f {REMOTE} {BAK} && echo backed-up")[0])

# 2) streamed upload (this server rejects sftp put 'wb' mode)
sftp = c.open_sftp()
with sftp.open(REMOTE, 'w') as f:
    f.set_pipelined(True)
    f.write(local)
print('uploaded')

# 3) verify md5
out, _ = run(f"md5sum {REMOTE}")
print('remote:', out)
print('MATCH' if lmd5 in out else 'MISMATCH!')
c.close()
