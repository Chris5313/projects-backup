#!/usr/bin/env python3
"""Offline RenderView analysis: find the ready-made ViewProjection matrix.

Reads gw2_dump_structs.txt, takes the view matrix (+0x2A0 region, the clean
one with a unit basis) and the projection (+0x3E0), computes P*V in all
convention combinations, and scans every 16-float window in the dump for a
match. Output: exact offsets + conventions for v14.
"""
import re

DUMP = r"C:/Users/Public/gw2_dump_structs.txt"

# ---- parse the hexdump-like matrix dump -------------------------------------
# Lines look like: "  +0x2A0: [  -0.9878    0.0322   -0.1525    0.0000] [..."
rows = {}
pat = re.compile(r"\+0x([0-9A-F]{3}):(.*)")
valpat = re.compile(r"(-?\d+\.\d+|-?\d+\.|-?\d+)")
for line in open(DUMP, errors="replace"):
    m = pat.search(line)
    if not m:
        continue
    off = int(m.group(1), 16)
    rest = m.group(2)
    if "# Matrix analysis" in line or "VIEWPROJ" in rest[:12] or "VIEW" in rest[:6] or "PROJECTION" in rest[:11]:
        rest = rest.split("#")[0]
    vals = [float(x) for x in valpat.findall(rest)]
    if len(vals) != 4:
        continue
    rows.setdefault(off, []).extend(vals)

# flat memory image of the RenderView (0x520 bytes)
mem = {}
for off, vals in rows.items():
    for i, v in enumerate(vals):
        mem[off + i * 4] = v
SIZE = 0x520
flat = [mem.get(a, None) for a in range(0, SIZE, 4)]

def get16(off):
    return [flat[off // 4 + i] for i in range(16)]

def matmul_rows(A, B):
    # both stored row-major; returns row-major C = A*B (4x4)
    C = [[0.0] * 4 for _ in range(4)]
    for r in range(4):
        for c in range(4):
            C[r][c] = sum(A[r * 4 + k] * B[k * 4 + c] for k in range(4))
    return C

def transpose(M):
    return [M[c * 4 + r] for r in range(4) for c in range(4)]

def show(M):
    return "\n".join("   [" + " ".join(f"{v:9.4f}" for v in M[r * 4:r * 4 + 4]) + "]" for r in range(4))

# ---- candidate view: the clean camera view ----------------------------------
# struct dump labels +0x2A0 as a VIEW with basis rows and translation row
# (322.3242, -45.0024, -120.0914, 1) — the camera eye at that spot.
VIEW_OFF = 0x2A0
V = get16(VIEW_OFF)
print(f"VIEW @ +{VIEW_OFF:#x}:\n{show(V)}")

# candidate projections: +0x3E0 (near 0.1) and +0x420
for PROJ_OFF in (0x3E0, 0x420):
    P = get16(PROJ_OFF)
    print(f"\nPROJ @ +{PROJ_OFF:#x}:\n{show(P)}")

    for pconv in (0, 1):
        Pm = P if pconv == 0 else transpose(P)
        for vconv in (0, 1):
            Vm = V if vconv == 0 else transpose(V)
            # row-major product P*V and V*P (both orderings, both transposes)
            for name, C in (("P*V", matmul_rows(Pm, Vm)), ("V*P", matmul_rows(Vm, Pm))):
                Cmem = C
                Ctr = transpose(C)
                for cmem_name, Ccmp in (("as-is", Cmem), ("transpose", Ctr)):
                    # scan every 16-float window in the RenderView
                    for off in range(0, SIZE - 64 + 4, 4):
                        W = get16(off)
                        if any(x is None for x in W):
                            continue
                        err = max(abs(a - b) for a, b in zip(W, Ccmp))
                        if err < 0.02:
                            print(f"\n*** MATCH: {name} pconv={pconv} vconv={vconv} compare={cmem_name} "
                                  f"FOUND @ +{off:#x} (maxerr {err:.5f})")
                            print(show(Ccmp))
