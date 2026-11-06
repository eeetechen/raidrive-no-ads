#!/usr/bin/env python3
"""从 .NET single-file bundle 里抽出 RaiDrive.Mount.dll，供 mdscan 之类的工具检查。"""
import struct, sys, os

src = sys.argv[1]
out = sys.argv[2]
d = open(src, 'rb').read()
manifest = struct.unpack_from('<q', d, 0x24f38)[0]

def rstr(p):
    v = 0; s = 0
    while True:
        b = d[p]; p += 1
        v |= (b & 0x7f) << s
        if not b & 0x80: break
        s += 7
    return d[p:p + v].decode('utf8', 'replace'), p + v

print(f'{os.path.basename(src)}: {len(d):,} bytes, manifest @0x{manifest:x}')
p = manifest + 12
bid, p = rstr(p)
print(f'bundle_id={bid}')
p += 40
n = 0
while n < 49:
    off, size, csize = struct.unpack_from('<qqq', d, p); p += 24
    t = d[p]; p += 1
    name, p = rstr(p)
    n += 1
    if name == 'RaiDrive.Mount.dll':
        print(f'  [{n-1}] {name} @0x{off:x}  {size:,} bytes  type={t}')
        open(out, 'wb').write(d[off:off+size])
        print(f'  -> wrote {out}')
        break
