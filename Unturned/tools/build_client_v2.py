#!/usr/bin/env python3
"""Generate _v2_build.rsp for the current tree and build HAMASCLIENT.dll."""
import glob
import os
import subprocess
import sys

ROOT = r"C:\Users\Shadow\Documents\Projects\Unturned\dll-src\dll-src"
HC = os.path.join(ROOT, "HAMASCLIENT")
LIB = os.path.join(ROOT, "lib")
OUT = os.path.join(HC, "bin", "Release", "HAMASCLIENT.dll")
RSP = os.path.join(HC, "_v2_build.rsp")

lines = [
    "/target:library", "/optimize+", "/unsafe+", "/langversion:9.0",
    "/nologo", "/nowarn:CS0618",
    '/out:"%s"' % OUT,
    '/reference:"C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319\\netstandard.dll"',
]
for dll in sorted(glob.glob(os.path.join(LIB, "*.dll"))):
    if os.path.basename(dll).lower() == "mscorlib.dll":
        continue
    lines.append('/reference:"%s"' % dll)
for pat in ("*.png", "*.fbx", "*.wav", "*.jpg"):
    for res in sorted(glob.glob(os.path.join(HC, pat))):
        name = os.path.basename(res)
        lines.append('/resource:"%s",HAMASCLIENT.%s' % (res, name))
for cs in sorted(glob.glob(os.path.join(HC, "*.cs"))):
    lines.append('"%s"' % cs)

with open(RSP, "w") as f:
    f.write("\n".join(lines) + "\n")

CSC = (r"C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools"
       r"\MSBuild\Current\Bin\Roslyn\csc.exe")
p = subprocess.run([CSC, "@" + RSP], capture_output=True, text=True,
                   errors="replace", cwd=HC)
sys.stdout.write(p.stdout[-6000:])
sys.stderr.write(p.stderr[-2000:])
print("EXIT =", p.returncode)
sys.exit(p.returncode)
