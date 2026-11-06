#!/usr/bin/env python3
"""逐字节比较两个（内嵌）assembly，列出所有差异区间，并解释每处属于哪个 PE 结构。"""
import sys, struct

A = open(sys.argv[1], 'rb').read()
B = open(sys.argv[2], 'rb').read()
print(f'A = {sys.argv[1]}  {len(A):,} B')
print(f'B = {sys.argv[2]}  {len(B):,} B  delta={len(B)-len(A):+,}')

n = min(len(A), len(B))
runs = []
i = 0
while i < n:
    if A[i] != B[i]:
        j = i
        while j < n and A[j] != B[j]:
            j += 1
        runs.append((i, j))
        i = j
    else:
        i += 1

# 长度不同的尾巴也算一处
tail = None
if len(A) != len(B):
    lo = min(len(A), len(B))
    tail = (lo, max(len(A), len(B)))

print(f'\n差异区间 {len(runs)} 个（共同长度内）' + (f' + 尾部长度差 @0x{tail[0]:x}' if tail else ''))
for s, e in runs:
    a = A[s:e].hex()
    b = B[s:e].hex()
    print(f'  @0x{s:06x}..0x{e-1:06x} ({e-s} B)')
    print(f'      A: {a[:96]}{"..." if len(a)>96 else ""}')
    print(f'      B: {b[:96]}{"..." if len(b)>96 else ""}')
if tail:
    print(f'  @0x{tail[0]:06x}..0x{tail[1]-1:06x}  ({tail[1]-tail[0]} B)  仅 B 有（长度差）')
