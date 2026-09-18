import struct, hashlib, sys

exe_path = r'C:\Users\Shadow\Desktop\HamasClient.exe'
data = open(exe_path, 'rb').read()
print('exe size', len(data))

e_lfanew = struct.unpack_from('<I', data, 0x3C)[0]
assert data[e_lfanew:e_lfanew+4] == b'PE\0\0', 'not PE'
coff = e_lfanew + 4
num_sec = struct.unpack_from('<H', data, coff+2)[0]
opt_size = struct.unpack_from('<H', data, coff+16)[0]
opt = coff + 20
magic = struct.unpack_from('<H', data, opt)[0]
dd_off = opt + (0x70 if magic == 0x10b else 0x80)
res_rva, res_size = struct.unpack_from('<II', data, dd_off + 8*2)  # dir entry 2 = .rsrc
print('.rsrc RVA', hex(res_rva), 'size', res_size)

# map rva->file offset via sections
sec_off = opt + opt_size
secs = []
for i in range(num_sec):
    o = sec_off + 40*i
    name = data[o:o+8].rstrip(b'\0').decode('ascii','ignore')
    vsz, va, rsz, ro = struct.unpack_from('<IIII', data, o+8)
    secs.append((name, va, min(vsz,rsz) or rsz, ro))

def rva2off(rva):
    for name, va, sz, ro in secs:
        if va <= rva < va + sz:
            return ro + (rva - va)
    return None

def u16(p):
    # resource dir entry name: high bit set -> offset to UTF-16 string
    ln = struct.unpack_from('<H', data, p)[0]
    return data[p+2:p+2+2*ln].decode('utf-16le','ignore')

def walk(off, level, path, out):
    chars, ts, mj, mn, nname, nid = struct.unpack_from('<IIHHHH', data, off)
    entries = off + 16
    for i in range(nname + nid):
        eo = entries + 8*i
        idfield, ofs = struct.unpack_from('<II', data, eo)
        if idfield & 0x80000000:
            nm = u16(res_rva + (idfield & 0x7FFFFFFF))
        else:
            nm = idfield
        if ofs & 0x80000000:
            walk(res_rva + (ofs & 0x7FFFFFFF), level+1, path+[nm], out)
        else:
            # data entry: rva, size, cp, reserved
            drva, dsz, cp, rsv = struct.unpack_from('<IIII', data, ofs)
            fo = rva2off(drva)
            out.append((path+[nm], drva, dsz, fo))

out = []
walk(res_rva, 0, [], out)
rcdata = [e for e in out if len(e[0]) >= 2 and e[0][0] == 10 or (len(e[0])>=2 and e[0][0]=='RT_RCDATA')]
# type may be int 10; normalize
rcdata = [e for e in out if str(e[0][0]) in ('10','RT_RCDATA')]
print('RCDATA entries:', len(rcdata))
for path, rva, sz, fo in sorted(rcdata, key=lambda x: int(x[0][1]) if str(x[0][1]).isdigit() else 0):
    rid = path[1]
    blob = data[fo:fo+sz] if fo is not None else b''
    md5 = hashlib.md5(blob).hexdigest()[:12] if blob else '??'
    print('res', rid, 'size', sz, 'md5', md5)
    if str(rid) in ('401','402','403','405'):
        blobw = blob[:600000]
        tags = []
        for sig, label in [
            ('\\??\\C:\\Users\\Public\\gw2_payload.dll', 'GW2-driver-path'),
            ('\\??\\C:\\Users\\Public\\payload.dll', 'UNTURNED-driver-path'),
            ('\\??\\C:\\Users\\Public\\kmap_status.txt', 'GW2-logpath'),
            ('\\??\\C:\\ProgramData\\Microsoft\\Windows\\WER\\ReportArchive\\status.log', 'UNTURNED-logpath'),
            ('d3d11.dll', 'd3d11-wait'),
            ('mono-2.0-bdwgc.dll', 'mono-wait'),
            ('esp-on default', 'v33-payload-marker'),
            ('measured view resolver', 'v33b-payload-marker'),
            ('MZ', 'MZ-head'),
        ]:
            if sig.encode('utf-16le') in blobw: tags.append(label)
            if sig.encode('ascii', 'ignore') in blobw and label not in tags: tags.append(label+'(a)')
        print('   tags:', tags)
open(r'C:\Users\Public\rc_402.bin','wb').write(data[ [e for e in rcdata if str(e[0][1])=='402'][0][3] : ][:1]) if any(str(e[0][1])=='402' for e in rcdata) else None
print('done')
