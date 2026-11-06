#!/usr/bin/env python3
"""解析 v4 内嵌 assembly 的段表、MethodDef RVA，并 dump 被补丁方法体的原始字节。"""
import sys, struct

p = sys.argv[1]
a = open(p, 'rb').read()

pe = struct.unpack_from('<I', a, 0x3c)[0]
assert a[pe:pe+4] == b'PE\0\0'
nsec = struct.unpack_from('<H', a, pe + 6)[0]
optsz = struct.unpack_from('<H', a, pe + 20)[0]
opt = pe + 24
magic = struct.unpack_from('<H', a, opt)[0]
dd = opt + (112 if magic == 0x20b else 96)
sectAlign = struct.unpack_from('<I', a, opt + 32)[0]
fileAlign = struct.unpack_from('<I', a, opt + 36)[0]
sizeOfImage = struct.unpack_from('<I', a, opt + 56)[0]
checksum = struct.unpack_from('<I', a, opt + 64)[0]
sechdr = opt + optsz

secs = []
for i in range(nsec):
    o = sechdr + i * 40
    name = a[o:o+8].rstrip(b'\0').decode()
    vs, va, rs, rp = struct.unpack_from('<IIII', a, o + 8)
    secs.append(dict(name=name, vs=vs, va=va, rs=rs, rp=rp, hdr=o))

print(f'{p}: {len(a):,} B  magic=0x{magic:x} sectAlign=0x{sectAlign:x} fileAlign=0x{fileAlign:x}')
print(f'SizeOfImage=0x{sizeOfImage:x} (aligned={sizeOfImage % sectAlign == 0})  CheckSum=0x{checksum:08x}')
for s in secs:
    print(f"  {s['name']:<9} va=0x{s['va']:06x} vs=0x{s['vs']:06x} rp=0x{s['rp']:06x} rs=0x{s['rs']:06x} "
          f"va+max=0x{s['va']+max(s['vs'],s['rs']):06x}")

# 尾部残留：最后一段之后的内容
last = max(secs, key=lambda s: s['rp'] + s['rs'])
end = last['rp'] + last['rs']
print(f'\n最后一段 {last["name"]} 覆盖到 file 0x{end:x}  文件 0x{len(a):x}  差 {len(a)-end} B')


def r2o(v):
    for s in secs:
        if s['va'] <= v < s['va'] + max(s['vs'], s['rs']):
            return s['rp'] + (v - s['va'])
    return None


# ---- MethodDef 表
clr_off = r2o(struct.unpack_from('<I', a, dd + 14 * 8)[0])
md_off = r2o(struct.unpack_from('<I', a, clr_off + 8)[0])
verLen = struct.unpack_from('<I', a, md_off + 12)[0]
q = md_off + 16 + verLen
nstreams = struct.unpack_from('<H', a, q + 2)[0]
q += 4
tables = None
for _ in range(nstreams):
    so, ss = struct.unpack_from('<II', a, q)
    q += 8
    z = q
    while a[z] != 0:
        z += 1
    nm = a[q:z].decode()
    q = (z + 1 + 3) & ~3
    if nm == '#~':
        tables = md_off + so
    print(f'  stream {nm:<12} off=0x{so:x} size=0x{ss:x}')

heap = a[tables + 6]
valid = struct.unpack_from('<Q', a, tables + 8)[0]
t = tables + 24
rows = [0] * 64
for i in range(64):
    if valid >> i & 1:
        rows[i] = struct.unpack_from('<I', a, t)[0]
        t += 4
sid = 4 if heap & 1 else 2
bid = 4 if heap & 4 else 2
gid = 4 if heap & 2 else 2


def idx(tb):
    return 4 if rows[tb] >= 0x10000 else 2


def coded(tbls, bits):
    m = max(rows[x] for x in tbls)
    return 4 if m >= (1 << (16 - bits)) else 2


sz = {
    0x00: 2 + sid + 3 * gid,
    0x01: coded((0x00, 0x1A, 0x23, 0x01), 2) + 2 * sid,
    0x02: 4 + 2 * sid + coded((0x02, 0x01, 0x1B), 2) + idx(0x04) + idx(0x06),
    0x03: idx(0x04),
    0x04: 2 + sid + bid,
    0x05: idx(0x06),
}
mdt = t
for tb in range(6):
    if valid >> tb & 1:
        mdt += rows[tb] * sz[tb]
rowsize = 4 + 2 + 2 + sid + bid + idx(0x08)
print(f'\nMethodDef @0x{mdt:x} rows={rows[6]} rowsize={rowsize}')
print(f'表流结束 @0x{mdt + rows[6]*rowsize:x}')


def body(row):
    rva = struct.unpack_from('<I', a, mdt + (row - 1) * rowsize)[0]
    o = r2o(rva)
    u = struct.unpack_from('<H', a, o)[0]
    hs = ((u >> 12) & 0xF) * 4
    if hs == 0:
        n = (u >> 2) & 0x3F
        st = o + 1
        hdr = f'TINY u16=0x{u:04x}'
    else:
        n = struct.unpack_from('<I', a, o + 4)[0]
        st = o + hs
        hdr = f'FAT flags=0x{u & 0xf:x} size(u16)=0x{u>>12:x} maxstack={struct.unpack_from("<H",a,o+2)[0]} localsig=0x{struct.unpack_from("<I",a,o+8)[0]:x}'
    return rva, o, n, a[st:st+n], hdr


name = 'v4' if 'v4' in p else 'orig'
print(f'\n--- {name}: 方法体 ---')
for row in (2747, 2748, 2749, 3554, 7121):
    rva, o, n, il, hdr = body(row)
    print(f'row {row}: rva=0x{rva:06x} file=0x{o:06x} codesize={n} {hdr}')
    print(f'           il={il.hex()}')
    raw = a[o:o + 16]
    print(f'           raw16={raw.hex()}')
