#!/usr/bin/env python3
"""独立解析 MethodDef/TypeDef 表，把指定 row 还原成「声明类型 + 方法名 + 签名」。

不依赖 ILSpy / .NET，直接按 ECMA-335 表布局读。
用途：验证 row N 到底属于哪个类型。.NET Reactor 随机化标识符后同名方法满街跑，
只有 row + 声明类型才可信。
"""
import sys, struct, bisect

p = sys.argv[1]
rows_wanted = [int(x) for x in sys.argv[2:]] or [2747, 2748, 2749, 3554, 7121]
a = open(p, 'rb').read()

pe = struct.unpack_from('<I', a, 0x3c)[0]
nsec = struct.unpack_from('<H', a, pe + 6)[0]
optsz = struct.unpack_from('<H', a, pe + 20)[0]
opt = pe + 24
magic = struct.unpack_from('<H', a, opt)[0]
dd = opt + (112 if magic == 0x20b else 96)
sechdr = opt + optsz

secs = []
for i in range(nsec):
    o = sechdr + i * 40
    vs, va, rs, rp = struct.unpack_from('<IIII', a, o + 8)
    secs.append((va, vs, rp, rs))


def r2o(v):
    for va, vs, rp, rs in secs:
        if va <= v < va + max(vs, rs):
            return rp + (v - va)
    return None


clr = r2o(struct.unpack_from('<I', a, dd + 14 * 8)[0])
md = r2o(struct.unpack_from('<I', a, clr + 8)[0])
verLen = struct.unpack_from('<I', a, md + 12)[0]
q = md + 16 + verLen
nstreams = struct.unpack_from('<H', a, q + 2)[0]
q += 4
streams = {}
for _ in range(nstreams):
    so, ss = struct.unpack_from('<II', a, q); q += 8
    z = q
    while a[z] != 0:
        z += 1
    nm = a[q:z].decode()
    q = (z + 1 + 3) & ~3
    streams[nm] = (md + so, ss)

T, _ = streams['#~']
STR, _ = streams['#Strings']
BLOB, _ = streams['#Blob']

heap = a[T + 6]
valid = struct.unpack_from('<Q', a, T + 8)[0]
q = T + 24
rows = [0] * 64
for i in range(64):
    if valid >> i & 1:
        rows[i] = struct.unpack_from('<I', a, q)[0]
        q += 4

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
    0x02: 4 + sid + sid + coded((0x02, 0x01, 0x1B), 2) + idx(4) + idx(6),
    0x03: idx(4),
    0x04: 2 + sid + bid,
    0x05: idx(6),
    0x06: 4 + 2 + 2 + sid + bid + idx(8),
    0x07: idx(8),
    0x08: 2 + 2 + sid,
    0x09: idx(2),
}

offsets = {}
cur = q
for tb in range(64):
    offsets[tb] = cur
    if valid >> tb & 1:
        cur += rows[tb] * sz.get(tb, 0)


def s(off):
    z = STR + off
    e = a.index(b'\0', z)
    return a[z:e].decode('utf8', 'replace')


def blob(off):
    z = BLOB + off
    b0 = a[z]
    if b0 & 0x80 == 0:
        n, z = b0, z + 1
    elif b0 & 0xC0 == 0x80:
        n, z = ((b0 & 0x3F) << 8) | a[z + 1], z + 2
    else:
        n, z = ((b0 & 0x1F) << 24) | (a[z + 1] << 16) | (a[z + 2] << 8) | a[z + 3], z + 4
    return a[z:z + n]


SID = '<H' if sid == 2 else '<I'
BID = '<H' if bid == 2 else '<I'

TD = offsets[2]
td = []
for i in range(rows[2]):
    o = TD + i * sz[0x02]
    name = s(struct.unpack_from(SID, a, o + 4)[0])
    ns = s(struct.unpack_from(SID, a, o + 4 + sid)[0])
    e = o + 4 + 2 * sid + coded((0x02, 0x01, 0x1B), 2)
    fl = struct.unpack_from('<I' if idx(4) == 4 else '<H', a, e)[0]
    e += idx(4)
    ml = struct.unpack_from('<I' if idx(6) == 4 else '<H', a, e)[0]
    td.append(dict(row=i + 1, name=name, ns=ns, firstMethod=ml, firstField=fl))

starts = [t['firstMethod'] for t in td]
assert starts == sorted(starts), 'TypeDef.MethodList 必须单调递增'
print(f'MethodDef 行数={rows[6]}  TypeDef 行数={rows[2]}  MethodDef 表 @0x{offsets[6]:x} 行宽 {sz[0x06]}')
print(f'TypeDef 覆盖的方法行：{starts[0]}..{rows[6]}  (合计 {rows[6] - starts[0] + 1})')

MD_T = offsets[6]


def method(row):
    o = MD_T + (row - 1) * sz[0x06]
    rva = struct.unpack_from('<I', a, o)[0]
    flags = struct.unpack_from('<H', a, o + 6)[0]
    nm = s(struct.unpack_from(SID, a, o + 8)[0])
    sig = blob(struct.unpack_from(BID, a, o + 8 + sid)[0])
    i = bisect.bisect_right(starts, row) - 1
    t = td[i]
    end = starts[i + 1] - 1 if i + 1 < len(starts) else rows[6]
    return dict(rva=rva, flags=flags, name=nm, sig=sig, type=t, span=(starts[i], end))


print('\n--- 归属判定 ---')
for r in rows_wanted:
    m = method(r)
    t = m['type']
    full = f'{t["ns"]}.{t["name"]}' if t['ns'] else t['name']
    print(f'row {r:5d}  rva=0x{m["rva"]:06x}  flags=0x{m["flags"]:04x}')
    print(f'          声明类型 TypeDef#{t["row"]:4d}  {full}')
    print(f'                       该类型方法 row {m["span"][0]}..{m["span"][1]}')
    print(f'          方法名   {m["name"]}')
    print(f'          签名     {m["sig"].hex()}')
