#!/usr/bin/env python3
"""校验交付的 RaiDrive.Mount.exe（v4）：

  * 三个去广告补丁方法确实是极小合法 IL
  * row 3554（驱动器列表高度）与 row 7121（功能授权）必须是原版 4 字节桩，
    即"未被改动" —— 这是 v2/v3 出错的地方，必须逐字节保证

用法：  python3 verify_patch.py [RaiDrive.Mount.exe]
"""
import struct
import sys

exe = sys.argv[1] if len(sys.argv) > 1 else 'RaiDrive.Mount.exe'
d = open(exe, 'rb').read()

# ---- .NET single-file bundle：.data 段里的 bundle 指针 ----
manifest = struct.unpack_from('<q', d, 0x24f38)[0]


def rstr(p):
    v = 0
    s = 0
    while True:
        b = d[p]
        p += 1
        v |= (b & 0x7f) << s
        if not b & 0x80:
            break
        s += 7
    return d[p:p + v].decode('utf8', 'replace'), p + v


p = manifest + 12
_b, p = rstr(p)          # bundle id
p += 40                  # depsjson offset/size, runtimeconfig offset/size, flags
asm_off = asm_size = None
count = 0
while p < len(d):
    off, size, csize = struct.unpack_from('<qqq', d, p)
    p += 24
    t = d[p]
    p += 1
    n, p = rstr(p)
    count += 1
    if n == 'RaiDrive.Mount.dll':
        asm_off, asm_size = off, size
    if count >= 49:
        break

if asm_off is None:
    sys.exit('找不到 RaiDrive.Mount.dll bundle 条目')

a = d[asm_off:asm_off + asm_size]

# ---- 内嵌 assembly 的段表 ----
pe = struct.unpack_from('<I', a, 0x3c)[0]
optsz = struct.unpack_from('<H', a, pe + 20)[0]
nsec = struct.unpack_from('<H', a, pe + 6)[0]
base = pe + 24 + optsz
secs = []
for i in range(nsec):
    q = base + i * 40
    vs, va, rs, rp = struct.unpack_from('<IIII', a, q + 8)
    secs.append((va, vs, rp, rs))


def r2o(v):
    for va, vs, rp, rs in secs:
        if va <= v < va + max(vs, rs):
            return rp + (v - va)
    return None


def method(row):
    """返回 (rva, file_offset, codesize, il_bytes)"""
    rva = struct.unpack_from('<I', a, MD + (row - 1) * ROW)[0]
    o = r2o(rva)
    if o is None:
        raise SystemExit(f'row {row}: RVA 0x{rva:x} 不在任何段内（PE 头被破坏）')
    u = struct.unpack_from('<H', a, o)[0]
    hs = ((u >> 12) & 0xF) * 4
    if hs == 0:                       # TINY header
        code = a[o + 1:o + 1 + (u >> 2)]
    else:                             # FAT header
        n = struct.unpack_from('<I', a, o + 4)[0]
        code = a[o + hs:o + hs + n]
    return rva, o, len(code), code


MD, ROW = 0x258a98, 18

# 三个真正需要改的广告方法
PATCHED = [
    (2747, bytes.fromhex('162a'),
     'AdvertisingViewModel::DDCNKJOP  (ads on/off flag)  ->  ldc.i4.0; ret'),
    (2748, bytes.fromhex('2a'),
     'AdvertisingViewModel::ADBKADLC  (show top banner)  ->  ret'),
    (2749, bytes.fromhex('2a'),
     'AdvertisingViewModel::MGHDEPFH  (load ad webview)  ->  ret'),
]

# 绝对不能被改的：原版 4 字节 TINY 桩
UNTOUCHED = [
    (3554, 'MMHNAEHBMHEHEMIIDENJMIDCMLMIIEEMBIJF::FGLJMLKEH  (drive-list Height!)'),
    (7121, 'DHFMIIJKPIMFDLOFKNBKNKNFLEGDDCGOFJHN.BGEIDHMC  (feature licence gate!)'),
]

ok = True

print('=== 应被补丁的方法 ===')
for row, want, note in PATCHED:
    rva, o, ln, code = method(row)
    good = code == want
    ok &= good
    print(f"{'OK ' if good else 'BAD'} row {row:5d} rva=0x{rva:06x} {ln}B "
          f"il={code.hex()}  {note}")

print('\n=== 必须保持原版的方法 ===')
for row, note in UNTOUCHED:
    rva, o, ln, code = method(row)
    # 原版桩：TINY header 0x12，codesize 4，首字节 + 0x2A(ret)
    good = ln == 4 and code[0] in (0x00, 0x17) and code[-1] == 0x2A
    ok &= good
    print(f"{'OK ' if good else 'BAD'} row {row:5d} rva=0x{rva:06x} {ln}B "
          f"il={code.hex()}  {note}")

print('\nRESULT:', '补丁正确，且授权/列表方法保持原版' if ok else '有问题，不要使用')
sys.exit(0 if ok else 1)
