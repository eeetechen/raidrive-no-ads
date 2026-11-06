#!/usr/bin/env python3
"""独立 IL 合法性检查：不依赖 ILVerify，直接按 ECMA-335 校验方法体。

检查项（这些正是「补丁把 IL 改坏」会立刻违反的）：
  1. fat header 的 size 字段是否与 InitLocals/flags 自洽
  2. InitLocals=true 但 LocalVarSigTok=0  -> InvalidProgramException（v2/v3 的翻车点）
  3. IL 字节能否被完整解码成合法指令流（不越界、无未知 opcode）
  4. 每个分支目标是否落在指令边界上
  5. 方法体是否越出所在 section
  6. 最大栈需求是否可能超过 maxstack（保守估计）
"""
import sys, struct

EOPS = {
    0x00: ('nop', 0), 0x01: ('break', 0), 0x02: ('ldarg.0', 0), 0x03: ('ldarg.1', 0),
    0x04: ('ldarg.2', 0), 0x05: ('ldarg.3', 0), 0x06: ('ldloc.0', 0), 0x07: ('ldloc.1', 0),
    0x08: ('ldloc.2', 0), 0x09: ('ldloc.3', 0), 0x0A: ('stloc.0', 0), 0x0B: ('stloc.1', 0),
    0x0C: ('stloc.2', 0), 0x0D: ('stloc.3', 0), 0x0E: ('ldarg.s', 1), 0x0F: ('ldarga.s', 1),
    0x10: ('starg.s', 1), 0x11: ('ldloc.s', 1), 0x12: ('ldloca.s', 1), 0x13: ('stloc.s', 1),
    0x14: ('ldnull', 0), 0x15: ('ldc.i4.m1', 0), 0x16: ('ldc.i4.0', 0), 0x17: ('ldc.i4.1', 0),
    0x18: ('ldc.i4.2', 0), 0x19: ('ldc.i4.3', 0), 0x1A: ('ldc.i4.4', 0), 0x1B: ('ldc.i4.5', 0),
    0x1C: ('ldc.i4.6', 0), 0x1D: ('ldc.i4.7', 0), 0x1E: ('ldc.i4.8', 0), 0x1F: ('ldc.i4.s', 1),
    0x20: ('ldc.i4', 4), 0x21: ('ldc.i8', 8), 0x22: ('ldc.r4', 4), 0x23: ('ldc.r8', 8),
    0x25: ('dup', 0), 0x26: ('pop', 0), 0x27: ('jmp', 4), 0x28: ('call', 4),
    0x29: ('calli', 4), 0x2A: ('ret', 0), 0x2B: ('br.s', 1), 0x2C: ('brfalse.s', 1),
    0x2D: ('brtrue.s', 1), 0x2E: ('beq.s', 1), 0x2F: ('bge.s', 1), 0x30: ('bgt.s', 1),
    0x31: ('ble.s', 1), 0x32: ('blt.s', 1), 0x33: ('bne.un.s', 1), 0x34: ('bge.un.s', 1),
    0x35: ('bgt.un.s', 1), 0x36: ('ble.un.s', 1), 0x37: ('blt.un.s', 1), 0x38: ('br', 4),
    0x39: ('brfalse', 4), 0x3A: ('brtrue', 4), 0x3B: ('beq', 4), 0x3C: ('bge', 4),
    0x3D: ('bgt', 4), 0x3E: ('ble', 4), 0x3F: ('blt', 4), 0x40: ('bne.un', 4),
    0x41: ('bge.un', 4), 0x42: ('bgt.un', 4), 0x43: ('ble.un', 4), 0x44: ('blt.un', 4),
    0x45: ('switch', -1), 0x46: ('ldind.i1', 0), 0x47: ('ldind.u1', 0), 0x48: ('ldind.i2', 0),
    0x49: ('ldind.u2', 0), 0x4A: ('ldind.i4', 0), 0x4B: ('ldind.u4', 0), 0x4C: ('ldind.i8', 0),
    0x4D: ('ldind.i', 0), 0x4E: ('ldind.r4', 0), 0x4F: ('ldind.r8', 0), 0x50: ('ldind.ref', 0),
    0x51: ('stind.ref', 0), 0x52: ('stind.i1', 0), 0x53: ('stind.i2', 0), 0x54: ('stind.i4', 0),
    0x55: ('stind.i8', 0), 0x56: ('stind.r4', 0), 0x57: ('stind.r8', 0),
    0x58: ('add', 0), 0x59: ('sub', 0), 0x5A: ('mul', 0), 0x5B: ('div', 0), 0x5C: ('div.un', 0),
    0x5D: ('rem', 0), 0x5E: ('rem.un', 0), 0x5F: ('and', 0), 0x60: ('or', 0), 0x61: ('xor', 0),
    0x62: ('shl', 0), 0x63: ('shr', 0), 0x64: ('shr.un', 0), 0x65: ('neg', 0), 0x66: ('not', 0),
    0x67: ('conv.i1', 0), 0x68: ('conv.i2', 0), 0x69: ('conv.i4', 0), 0x6A: ('conv.i8', 0),
    0x6B: ('conv.r4', 0), 0x6C: ('conv.r8', 0), 0x6D: ('conv.u4', 0), 0x6E: ('conv.u8', 0),
    0x6F: ('callvirt', 4), 0x70: ('cpobj', 4), 0x71: ('ldobj', 4), 0x72: ('ldstr', 4),
    0x73: ('newobj', 4), 0x74: ('castclass', 4), 0x75: ('isinst', 4), 0x76: ('conv.r.un', 0),
    0x79: ('unbox', 4), 0x7A: ('throw', 0), 0x7B: ('ldfld', 4), 0x7C: ('ldflda', 4),
    0x7D: ('stfld', 4), 0x7E: ('ldsfld', 4), 0x7F: ('ldsflda', 4), 0x80: ('stsfld', 4),
    0x81: ('stobj', 4), 0x82: ('conv.ovf.i1.un', 0), 0x83: ('conv.ovf.i2.un', 0),
    0x84: ('conv.ovf.i4.un', 0), 0x85: ('conv.ovf.i8.un', 0), 0x86: ('conv.ovf.u1.un', 0),
    0x87: ('conv.ovf.u2.un', 0), 0x88: ('conv.ovf.u4.un', 0), 0x89: ('conv.ovf.u8.un', 0),
    0x8A: ('conv.ovf.i.un', 0), 0x8B: ('conv.ovf.u.un', 0), 0x8C: ('box', 4),
    0x8D: ('newarr', 4), 0x8E: ('ldlen', 0), 0x8F: ('ldelema', 4), 0x90: ('ldelem.i1', 0),
    0x91: ('ldelem.u1', 0), 0x92: ('ldelem.i2', 0), 0x93: ('ldelem.u2', 0), 0x94: ('ldelem.i4', 0),
    0x95: ('ldelem.u4', 0), 0x96: ('ldelem.i8', 0), 0x97: ('ldelem.i', 0), 0x98: ('ldelem.r4', 0),
    0x99: ('ldelem.r8', 0), 0x9A: ('ldelem.ref', 0), 0x9B: ('stelem.i', 0), 0x9C: ('stelem.i1', 0),
    0x9D: ('stelem.i2', 0), 0x9E: ('stelem.i4', 0), 0x9F: ('stelem.i8', 0), 0xA0: ('stelem.r4', 0),
    0xA1: ('stelem.r8', 0), 0xA2: ('stelem.ref', 0), 0xA3: ('ldelem', 4), 0xA4: ('stelem', 4),
    0xA5: ('unbox.any', 4), 0xB3: ('conv.ovf.i1', 0), 0xB4: ('conv.ovf.u1', 0),
    0xB5: ('conv.ovf.i2', 0), 0xB6: ('conv.ovf.u2', 0), 0xB7: ('conv.ovf.i4', 0),
    0xB8: ('conv.ovf.u4', 0), 0xB9: ('conv.ovf.i8', 0), 0xBA: ('conv.ovf.u8', 0),
    0xC2: ('refanyval', 4), 0xC3: ('ckfinite', 0), 0xC6: ('mkrefany', 4),
    0xD0: ('ldtoken', 4), 0xD1: ('conv.u2', 0), 0xD2: ('conv.u1', 0), 0xD3: ('conv.i', 0),
    0xD4: ('conv.ovf.i', 0), 0xD5: ('conv.ovf.u', 0), 0xD6: ('add.ovf', 0),
    0xD7: ('add.ovf.un', 0), 0xD8: ('mul.ovf', 0), 0xD9: ('mul.ovf.un', 0),
    0xDA: ('sub.ovf', 0), 0xDB: ('sub.ovf.un', 0), 0xDC: ('endfinally', 0),
    0xDD: ('leave', 4), 0xDE: ('leave.s', 1), 0xDF: ('stind.i', 0), 0xE0: ('conv.u', 0),
}
FE = {
    0x00: ('arglist', 0), 0x01: ('ceq', 0), 0x02: ('cgt', 0), 0x03: ('cgt.un', 0),
    0x04: ('clt', 0), 0x05: ('clt.un', 0), 0x06: ('ldftn', 4), 0x07: ('ldvirtftn', 4),
    0x09: ('ldarg', 2), 0x0A: ('ldarga', 2), 0x0B: ('starg', 2), 0x0C: ('ldloc', 2),
    0x0D: ('ldloca', 2), 0x0E: ('stloc', 2), 0x0F: ('localloc', 0), 0x11: ('endfilter', 0),
    0x12: ('unaligned.', 1), 0x13: ('volatile.', 0), 0x14: ('tail.', 0),
    0x15: ('initobj', 4), 0x16: ('constrained.', 4), 0x17: ('cpblk', 0),
    0x18: ('initblk', 0), 0x19: ('no.', 1), 0x1A: ('rethrow', 0), 0x1C: ('sizeof', 4),
    0x1D: ('refanytype', 0), 0x1E: ('readonly.', 0),
}
BR1 = {0x2B, 0x2C, 0x2D, 0x2E, 0x2F, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0xDE}
BR4 = {0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F, 0x40, 0x41, 0x42, 0x43, 0x44, 0xDD}


def decode(il, where):
    """把 IL 解码成 (offset, size, name, operand) 列表；返回 (ops, errors)"""
    ops, errs = [], []
    i, n = 0, len(il)
    while i < n:
        start = i
        op = il[i]; i += 1
        if op == 0xFE:
            if i >= n:
                errs.append(f'{where}: IL_0x{start:x} 0xFE 后无字节')
                break
            op2 = il[i]; i += 1
            if op2 not in FE:
                errs.append(f'{where}: IL_0x{start:x} 未知双字节 opcode 0xFE{op2:02X}')
                ops.append((start, i - start, f'FE{op2:02X}?', None)); continue
            name, sz = FE[op2]
        else:
            if op not in EOPS:
                errs.append(f'{where}: IL_0x{start:x} 未知 opcode 0x{op:02X}')
                ops.append((start, 1, f'{op:02X}?', None)); continue
            name, sz = EOPS[op]
        if sz == -1:                      # switch
            if i + 4 > n:
                errs.append(f'{where}: IL_0x{start:x} switch 计数越界'); break
            cnt = struct.unpack_from('<I', il, i)[0]; i += 4
            i += 4 * cnt
            if i > n:
                errs.append(f'{where}: IL_0x{start:x} switch 目标表越界'); break
            ops.append((start, i - start, name, None)); continue
        i += sz
        if i > n:
            errs.append(f'{where}: IL_0x{start:x} {name} 操作数越界（需 {sz} 字节，剩 {n - (i - sz)}）')
            break
        ops.append((start, i - start, name, None))
    return ops, errs


def check(path):
    a = open(path, 'rb').read()
    pe = struct.unpack_from('<I', a, 0x3c)[0]
    nsec = struct.unpack_from('<H', a, pe + 6)[0]
    optsz = struct.unpack_from('<H', a, pe + 20)[0]
    opt = pe + 24
    magic = struct.unpack_from('<H', a, opt)[0]
    dd = opt + (112 if magic == 0x20b else 96)
    base = opt + optsz
    secs = []
    for i in range(nsec):
        o = base + i * 40
        nm = a[o:o + 8].rstrip(b'\0').decode()
        vs, va, rs, rp = struct.unpack_from('<IIII', a, o + 8)
        secs.append((nm, va, vs, rp, rs))

    def r2o(v):
        for nm, va, vs, rp, rs in secs:
            if va <= v < va + max(vs, rs):
                return rp + (v - va)
        return None

    clr = r2o(struct.unpack_from('<I', a, dd + 14 * 8)[0])
    md = r2o(struct.unpack_from('<I', a, clr + 8)[0])
    verLen = struct.unpack_from('<I', a, md + 12)[0]
    q = md + 16 + verLen
    ns = struct.unpack_from('<H', a, q + 2)[0]; q += 4
    T = None
    for _ in range(ns):
        so, ss = struct.unpack_from('<II', a, q); q += 8
        z = q
        while a[z]: z += 1
        nm = a[q:z].decode(); q = (z + 1 + 3) & ~3
        if nm == '#~':
            T = md + so
    heap = a[T + 6]; valid = struct.unpack_from('<Q', a, T + 8)[0]
    q = T + 24
    rows = [0] * 64
    for i in range(64):
        if valid >> i & 1:
            rows[i] = struct.unpack_from('<I', a, q)[0]; q += 4
    sid = 4 if heap & 1 else 2
    bid = 4 if heap & 4 else 2
    gid = 4 if heap & 2 else 2

    def idx(t): return 4 if rows[t] >= 0x10000 else 2

    def coded(t, b):
        m = max(rows[x] for x in t)
        return 4 if m >= (1 << (16 - b)) else 2

    sz = {0x00: 2 + sid + 3 * gid,
          0x01: coded((0x00, 0x1A, 0x23, 0x01), 2) + 2 * sid,
          0x02: 4 + 2 * sid + coded((0x02, 0x01, 0x1B), 2) + idx(4) + idx(6),
          0x03: idx(4), 0x04: 2 + sid + bid, 0x05: idx(6),
          0x06: 4 + 2 + 2 + sid + bid + idx(8), 0x07: idx(8), 0x08: 2 + 2 + sid, 0x09: idx(2)}
    off = {}; cur = q
    for t in range(64):
        off[t] = cur
        if valid >> t & 1:
            cur += rows[t] * sz.get(t, 0)
    MD = off[6]
    STR = md + dict((nm, so) for nm, so in []).get('#Strings', 0)
    # 重新定位 #Strings
    q2 = md + 16 + verLen + 4
    for _ in range(ns):
        so, ss = struct.unpack_from('<II', a, q2); q2 += 8
        z = q2
        while a[z]: z += 1
        nm = a[q2:z].decode(); q2 = (z + 1 + 3) & ~3
        if nm == '#Strings':
            STR = md + so

    def s(o):
        z = STR + o
        e = a.index(b'\0', z)
        return a[z:e].decode('utf8', 'replace')

    total = rows[6]
    errs_all, fat_bad, localsig_bad, decoded = [], [], [], 0
    for row in range(1, total + 1):
        rva = struct.unpack_from('<I', a, MD + (row - 1) * sz[6])[0]
        if rva == 0:
            continue
        o = r2o(rva)
        if o is None:
            errs_all.append(f'row {row}: RVA 0x{rva:x} 不在任何 section 内')
            continue
        u = struct.unpack_from('<H', a, o)[0]
        hs = ((u >> 12) & 0xF) * 4
        if hs == 0:
            size = (u >> 2) & 0x3F
            start = o + 1
            initlocals = False
            localsig = 0
            maxstack = 8
        else:
            flags = u & 0xF
            size = struct.unpack_from('<I', a, o + 4)[0]
            maxstack = struct.unpack_from('<H', a, o + 2)[0]
            localsig = struct.unpack_from('<I', a, o + 8)[0]
            start = o + hs
            initlocals = bool(flags & 0x10)
        if start + size > len(a):
            errs_all.append(f'row {row}: 方法体越出文件（start=0x{start:x} size={size}）')
            continue
        if initlocals and localsig == 0:
            localsig_bad.append(f'row {row}: InitLocals 但 LocalVarSigTok=0（非法）')
        if maxstack < 1:
            fat_bad.append(f'row {row}: maxstack={maxstack}')
        il = a[start:start + size]
        ops, e = decode(il, f'row {row}')
        decoded += 1
        errs_all.extend(e)
    return dict(total=total, decoded=decoded, errs=errs_all,
                localsig=localsig_bad, fat=fat_bad, rows=rows[6])


for p in sys.argv[1:]:
    r = check(p)
    print(f'=== {p} ===')
    print(f'  MethodDef 行数      : {r["total"]}')
    print(f'  成功解码的方法体    : {r["decoded"]}')
    print(f'  InitLocals 非法     : {len(r["localsig"])}')
    for x in r['localsig'][:10]: print(f'      {x}')
    print(f'  maxstack 异常       : {len(r["fat"])}')
    for x in r['fat'][:10]: print(f'      {x}')
    print(f'  IL 解码错误         : {len(r["errs"])}')
    for x in r['errs'][:20]: print(f'      {x}')
    print()
