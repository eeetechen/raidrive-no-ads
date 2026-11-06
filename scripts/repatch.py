#!/usr/bin/env python3
"""把 v4 去广告补丁打到「未修改」的 RaiDrive.Mount.exe 上。

    python3 repatch.py <原版 RaiDrive.Mount.exe> <输出 RaiDrive.Mount.exe>

只改 3 个方法，全部属于 RaiDrive.Center.ViewModels.AdvertisingViewModel：

    row 2747  bool DDCNKJOP...()  ->  ldc.i4.0 ; ret     (恒假，广告开关)
    row 2748  void ADBKADLC...()  ->  ret                (显示横幅 -> 空操作)
    row 2749  void MGHDEPFH...()  ->  ret                (加载广告页 -> 空操作)

做法（与 v4 完全一致，纯字节操作，不需要 Wine、不需要 .NET）：

  1. 从 exe 尾部 overlay 里的 .NET single-file bundle 取出 RaiDrive.Mount.dll
  2. 把 3 个极小方法体追加到 .rsrc 段末尾（扩展段，不新增 section header）
  3. 改这 3 个 MethodDef 的 RVA 指向新方法体
  4. 把改好的 assembly 塞回 bundle，修正 49 个条目偏移、manifest 指针、
     证书表偏移、PE 校验和，清掉已失效的 release 签名

不碰的：row 3554（驱动器列表高度）、row 7121（功能授权门）。
这两处是 v2/v3 翻车的地方，脚本最后会断言它们仍是原版 4 字节桩。

验证：对原版跑一遍，输出 sha256 应为
    0acccd3ee5f914d1aaba87d8f6bc38e19374379f296a55505937ad06a1a259fa
"""
import hashlib
import struct
import sys

if len(sys.argv) != 3:
    sys.exit(__doc__)

SRC, OUT = sys.argv[1], sys.argv[2]

# ---------------------------------------------------------------- bundle 解析
PTR_POS = 0x24f38            # .data 段里指向 bundle manifest 的 int64
SIG_POS = 0x24f40            # bundle 的 32 字节 release 签名
NENT = 49                    # manifest 条目数（2026.10.0）
ASM_NAME = 'RaiDrive.Mount.dll'


def read_leb(buf, p):
    v = 0
    s = 0
    while True:
        b = buf[p]
        p += 1
        v |= (b & 0x7f) << s
        if not b & 0x80:
            return v, p
        s += 7


def read_str(buf, p):
    n, p = read_leb(buf, p)
    return buf[p:p + n].decode('utf8', 'replace'), p + n


orig = open(SRC, 'rb').read()
manifest = struct.unpack_from('<q', orig, PTR_POS)[0]

p = manifest + 12                       # skip version(4+4) + count(4)
_bundle_id, p = read_str(orig, p)
p += 32                                 # deps.json off/size, runtimeconfig off/size
p += 8                                  # flags
ents = []
for _ in range(NENT):
    off, size, csize = struct.unpack_from('<qqq', orig, p)
    p += 24
    typ = orig[p]
    p += 1
    name, p = read_str(orig, p)
    ents.append(dict(off=off, size=size, csize=csize, type=typ, name=name))

idx = next(i for i, e in enumerate(ents) if e['name'] == ASM_NAME)
old_off, old_size = ents[idx]['off'], ents[idx]['size']
old_end = old_off + old_size
asm = bytearray(orig[old_off:old_end])

# ----------------------------------------------------------- assembly 内的 PE
pe = struct.unpack_from('<I', asm, 0x3c)[0]
nsec = struct.unpack_from('<H', asm, pe + 6)[0]
optsz = struct.unpack_from('<H', asm, pe + 20)[0]
opt = pe + 24
magic = struct.unpack_from('<H', asm, opt)[0]
dd = opt + (112 if magic == 0x20b else 96)
sect_align = struct.unpack_from('<I', asm, opt + 32)[0]
file_align = struct.unpack_from('<I', asm, opt + 36)[0]
sec_base = opt + optsz

secs = []
for i in range(nsec):
    o = sec_base + i * 40
    name = bytes(asm[o:o + 8]).rstrip(b'\0').decode()
    vs, va, rs, rp = struct.unpack_from('<IIII', asm, o + 8)
    secs.append(dict(name=name, vs=vs, va=va, rs=rs, rp=rp, hdr=o))


def r2o(rva):
    for s in secs:
        if s['va'] <= rva < s['va'] + max(s['vs'], s['rs']):
            return s['rp'] + (rva - s['va'])
    return None


# ------------------------------------------------- locate MethodDef + row size
# MethodDef 第一行（<Module> 的 .ctor）的 RVA 是 8264，这个 4 字节序列在文件里
# 唯一，用它定位表头。bundle 里的 assembly 版本变了要重新确认这个值。
TARGET_FIRST_RVA = 8264
pat = struct.pack('<I', TARGET_FIRST_RVA)
md_off = asm.find(pat)
if md_off < 0:
    sys.exit('找不到 MethodDef 表（不是预期的 RaiDrive.Mount.dll？）')

# heap 大小 -> str/blob 索引宽度
clr = r2o(struct.unpack_from('<I', asm, dd + 14 * 8)[0])
md_root = r2o(struct.unpack_from('<I', asm, clr + 8)[0])
verlen = struct.unpack_from('<I', asm, md_root + 12)[0]
q = md_root + 16 + verlen
nstreams = struct.unpack_from('<H', asm, q + 2)[0]
q += 4
tables_off = None
for _ in range(nstreams):
    soff, ssize = struct.unpack_from('<II', asm, q)
    q += 8
    z = asm.index(b'\0', q)
    nm = bytes(asm[q:z]).decode()
    q = (z + 1 + 3) & ~3
    if nm == '#~':
        tables_off = md_root + soff
if tables_off is None:
    sys.exit('找不到 #~ 表流')

heaps = asm[tables_off + 6]
valid = struct.unpack_from('<Q', asm, tables_off + 8)[0]
t = tables_off + 24
rows = [0] * 64
for i in range(64):
    if valid & (1 << i):
        rows[i] = struct.unpack_from('<I', asm, t)[0]
        t += 4

str_idx = 4 if (heaps & 1) else 2
blob_idx = 4 if (heaps & 4) else 2
guid_idx = 4 if (heaps & 2) else 2
tbl_idx = lambda tb: 4 if rows[tb] >= 0x10000 else 2
row_size = 4 + 2 + 2 + str_idx + blob_idx + tbl_idx(0x08)

print(f'assembly {old_size} B  MethodDef @0x{md_off:x} rows={rows[6]} rowsize={row_size}')


def method_body(row):
    """返回 (rva, file_off, codesize, bytes)"""
    rva = struct.unpack_from('<I', asm, md_off + (row - 1) * row_size)[0]
    o = r2o(rva)
    if o is None:
        sys.exit(f'row {row}: RVA 0x{rva:x} 不在任何段内')
    u = struct.unpack_from('<H', asm, o)[0]
    hs = ((u >> 12) & 0xF) * 4
    if hs == 0:                                   # TINY
        return rva, o, (u >> 2) & 0x3F, bytes(asm[o + 1:o + 1 + ((u >> 2) & 0x3F)])
    n = struct.unpack_from('<I', asm, o + 4)[0]   # FAT
    return rva, o, n, bytes(asm[o + hs:o + hs + n])


# ------------------------------------------------------------------ 前置断言
# 三个目标方法必须是 4 字节 TINY 桩，且位于段内
for row in (2747, 2748, 2749, 3554, 7121):
    rva, o, ln, code = method_body(row)
    assert ln == 4 and code[-1] == 0x2A, (
        f'row {row} 不是预期的 4 字节桩（codesize={ln} il={code.hex()}）'
        f'——输入可能已经是打过补丁的，或版本不匹配')
print('前置检查通过：5 个相关方法均为原版 4 字节桩')

# --------------------------------------------------------- 构造三个极小方法体
TINY = {
    2747: bytes([0x16, 0x2A]),   # ldc.i4.0 ; ret
    2748: bytes([0x2A]),         # ret
    2749: bytes([0x2A]),         # ret
}

last = max(secs, key=lambda s: s['rp'] + s['rs'])
blob_start = (last['rp'] + last['rs'] + file_align - 1) & ~(file_align - 1)
blob_rva = last['va'] + (blob_start - last['rp'])
print(f'新方法体 blob: file 0x{blob_start:x} rva 0x{blob_rva:x} (追加进 {last["name"]})')

blob = bytearray()
new_rva = {}
for row in sorted(TINY):
    il = TINY[row]
    codesize = len(il)
    il = il + b'\x00' * ((4 - len(il) % 4) % 4)
    total = ((12 + len(il)) + 3) & ~3
    hdr = bytearray(12)
    # fat header: u16 = 3 dwords (12 字节头) << 12 | flags
    # flags 0x3 = Fat；0x10 = InitLocals —— 这里没有局部变量，必须清掉
    struct.pack_into('<HH', hdr, 0, (3 << 12) | 0x03, 8)
    struct.pack_into('<I', hdr, 4, codesize)      # codesize 是字节数
    struct.pack_into('<I', hdr, 8, 0)             # localsig = 0（无局部变量）
    new_rva[row] = blob_rva + len(blob)
    blob += hdr + il + b'\x00' * (total - 12 - len(il))
print(f'三个方法体共 {len(blob)} 字节')

# ------------------------------------------------- 扩展 .rsrc（不新增 section）
new_raw_end = (blob_start + len(blob) + file_align - 1) & ~(file_align - 1)
new_vs = (last['va'] + (new_raw_end - last['rp']) + sect_align - 1) & ~(sect_align - 1)
struct.pack_into('<I', asm, last['hdr'] + 8, new_vs - last['va'])          # VirtualSize
struct.pack_into('<I', asm, last['hdr'] + 16, new_raw_end - last['rp'])    # SizeOfRawData
print(f'  {last["name"]}: vs 0x{last["vs"]:x} -> 0x{new_vs-last["va"]:x}, '
      f'rawsz 0x{last["rs"]:x} -> 0x{new_raw_end-last["rp"]:x}')

img_off = opt + 56
old_img = struct.unpack_from('<I', asm, img_off)[0]
new_img = (new_vs + sect_align - 1) & ~(sect_align - 1)
struct.pack_into('<I', asm, img_off, max(old_img, new_img))
print(f'  SizeOfImage 0x{old_img:x} -> 0x{max(old_img,new_img):x}')

# 落盘 blob
if len(asm) < blob_start:
    asm += b'\x00' * (blob_start - len(asm))
asm += blob
if len(asm) < new_raw_end:
    asm += b'\x00' * (new_raw_end - len(asm))

for row, rva in new_rva.items():
    struct.pack_into('<I', asm, md_off + (row - 1) * row_size, rva)
print(f'已改写 {len(new_rva)} 个 MethodDef RVA')

asm = bytes(asm)

# --------------------------------------------------------------- 塞回 bundle
delta = len(asm) - old_size
out = bytearray(orig[:old_off] + asm + orig[old_end:])
print(f'bundle 拼接：assembly {old_size} -> {len(asm)} (delta {delta:+d})')


def shift(v):
    return v + delta if v >= old_end else v


np_ = manifest + delta
h = np_ + 12
_, h = read_str(out, h)
for pos, is_ptr in ((h, True), (h + 8, False), (h + 16, True), (h + 24, False)):
    if is_ptr:
        struct.pack_into('<q', out, pos, shift(struct.unpack_from('<q', out, pos)[0]))
h += 32 + 8
for i in range(NENT):
    cur = struct.unpack_from('<q', out, h)[0]
    struct.pack_into('<q', out, h, shift(cur))
    if i == idx:
        struct.pack_into('<q', out, h + 8, len(asm))
    h += 24 + 1
    _, h = read_str(out, h)

struct.pack_into('<q', out, PTR_POS, np_)

# 证书表在 overlay 之后，跟着平移
pe2 = struct.unpack_from('<I', out, 0x3c)[0]
opt2 = pe2 + 24
magic2 = struct.unpack_from('<H', out, opt2)[0]
dd2 = opt2 + (112 if magic2 == 0x20b else 96)
cert_off = struct.unpack_from('<I', out, dd2 + 4 * 8)[0]
if cert_off:
    struct.pack_into('<I', out, dd2 + 4 * 8, cert_off + delta)

# release 签名已经对不上，清零（按未签名处理）
out[SIG_POS:SIG_POS + 32] = b'\0' * 32


def pe_checksum(buf):
    e = struct.unpack_from('<I', buf, 0x3c)[0]
    chk = e + 24 + 64
    tmp = bytearray(buf)
    struct.pack_into('<I', tmp, chk, 0)
    if len(tmp) % 2:
        tmp += b'\0'
    total = 0
    for i in range(0, len(tmp), 2):
        total += struct.unpack_from('<H', tmp, i)[0]
        total = (total & 0xFFFF) + (total >> 16)
    total = (total & 0xFFFF) + (total >> 16)
    return (total + len(buf)) & 0xFFFFFFFF


struct.pack_into('<I', out, opt2 + 64, pe_checksum(bytes(out)))

open(OUT, 'wb').write(bytes(out))
print(f'\n写出 {OUT}  {len(out)} 字节')
print(f'sha256 {hashlib.sha256(bytes(out)).hexdigest()}')
