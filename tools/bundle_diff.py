#!/usr/bin/env python3
"""逐条比较两个 exe 的 bundle 目录（偏移/大小/内容），确认只有 RaiDrive.Mount.dll 变了。"""
import struct, sys, hashlib

def parse(path):
    d = open(path, 'rb').read()
    m = struct.unpack_from('<q', d, 0x24f38)[0]

    def rstr(p):
        v = 0; s = 0
        while True:
            b = d[p]; p += 1
            v |= (b & 0x7f) << s
            if not b & 0x80: break
            s += 7
        return d[p:p + v].decode('utf8', 'replace'), p + v

    p = m + 12
    bid, p = rstr(p)
    deps_off, deps_size, rc_off, rc_size = struct.unpack_from('<qqqq', d, p)
    p += 32
    flags = struct.unpack_from('<Q', d, p)[0]
    p += 8
    hdr = dict(manifest=m, ver=struct.unpack_from('<II', d, m), bundle_id=bid,
               deps=(deps_off, deps_size), rc=(rc_off, rc_size), flags=flags)
    ents = []
    for i in range(49):
        off, size, csize = struct.unpack_from('<qqq', d, p); p += 24
        t = d[p]; p += 1
        nm, p = rstr(p)
        ents.append(dict(i=i, off=off, size=size, csize=csize, type=t, name=nm,
                         sha=hashlib.sha256(d[off:off+size]).hexdigest()))
    return d, hdr, ents


A, ha, ea = parse(sys.argv[1])
B, hb, eb = parse(sys.argv[2])
print(f'A: {sys.argv[1]}  {len(A):,} B')
print(f'B: {sys.argv[2]}  {len(B):,} B')
print(f'  A ver={ha["ver"]} id={ha["bundle_id"]} deps={ha["deps"]} rc={ha["rc"]} flags={ha["flags"]}')
print(f'  B ver={hb["ver"]} id={hb["bundle_id"]} deps={hb["deps"]} rc={hb["rc"]} flags={hb["flags"]}')
print(f'\n{"i":>3} {"name":<34} {"A off":>10} {"A size":>11} {"B off":>10} {"B size":>11}  verdict')
bad = 0
for x, y in zip(ea, eb):
    assert x['name'] == y['name'], (x, y)
    if x['sha'] == y['sha'] and x['off'] == y['off'] and x['size'] == y['size']:
        v = 'same'
    else:
        same = x['sha'] == y['sha']
        v = ('IDENTICAL CONTENT, relocated' if same else 'CHANGED CONTENT')
        if not same:
            bad += 1
    print(f"{x['i']:>3} {x['name']:<34} 0x{x['off']:08x} {x['size']:>11,} "
          f"0x{y['off']:08x} {y['size']:>11,}  {v}")
print(f'\n条目总数 {len(ea)}/{len(eb)}，内容变化 {bad} 个')
