<#
.SYNOPSIS
    把 v4 去广告补丁打到「未修改」的 RaiDrive.Mount.exe 上（纯 PowerShell，不需要 Python）。

.DESCRIPTION
    与 tools\repatch.py 做的事完全一样，产物 sha256 也应当相同：

        0acccd3ee5f914d1aaba87d8f6bc38e19374379f296a55505937ad06a1a259fa

    只改 3 个方法（全部属于 RaiDrive.Center.ViewModels.AdvertisingViewModel）：

        row 2747  bool DDCNKJOP...()  ->  ldc.i4.0 ; ret     （恒假，广告开关）
        row 2748  void ADBKADLC...()  ->  ret                （显示横幅 -> 空操作）
        row 2749  void MGHDEPFH...()  ->  ret                （加载广告页 -> 空操作）

    绝不动 row 3554（驱动器列表高度）与 row 7121（功能授权门）——
    这两处是 v2/v3 翻车的地方，脚本会先断言它们仍是原版 4 字节桩。

    用途：等 RaiDrive 出新版本时，用新装出来的原版 exe 重新打一次补丁。

.EXAMPLE
    .\apply-patch.ps1 -Source .\RaiDrive.Mount.orig.exe -Out .\RaiDrive.Mount.patched.exe
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Source,
    [Parameter(Mandatory = $true)][string]$Out
)

$ErrorActionPreference = 'Stop'

Add-Type -TypeDefinition @'
using System;
public static class RdPatchHelper {
    // 在字节数组里找一段模式，返回首次出现的下标，找不到返回 -1
    public static long FindBytes(byte[] hay, byte[] pat, long start) {
        long last = hay.LongLength - pat.LongLength;
        for (long i = start; i <= last; i++) {
            bool ok = true;
            for (int j = 0; j < pat.Length; j++) {
                if (hay[i + j] != pat[j]) { ok = false; break; }
            }
            if (ok) return i;
        }
        return -1;
    }
    // PE 校验和
    public static uint Checksum(byte[] b, long checksumPos) {
        ulong total = 0;
        long n = b.LongLength & ~1L;
        for (long i = 0; i < n; i += 2) {
            if (i == checksumPos || i == checksumPos + 1) continue;
            total += (ulong)(b[i] | (b[i + 1] << 8));
            total = (total & 0xFFFF) + (total >> 16);
        }
        total = (total & 0xFFFF) + (total >> 16);
        return (uint)(total + (ulong)b.LongLength);
    }
}
'@

function Get-U16([byte[]]$b, [long]$o) { [BitConverter]::ToUInt16($b, [int]$o) }
function Get-U32([byte[]]$b, [long]$o) { [BitConverter]::ToUInt32($b, [int]$o) }
function Get-I64([byte[]]$b, [long]$o) { [BitConverter]::ToInt64($b, [int]$o) }

function Read-Leb([byte[]]$b, [ref]$p) {
    [long]$v = 0; [int]$s = 0
    while ($true) {
        [int]$x = $b[$p.Value]; $p.Value = $p.Value + 1
        $v = $v -bor ([long]($x -band 0x7f) -shl $s)
        if (($x -band 0x80) -eq 0) { return $v }
        $s = $s + 7
    }
}
function Read-Str([byte[]]$b, [ref]$p) {
    [int]$n = [int](Read-Leb $b $p)
    [string]$s = [Text.Encoding]::UTF8.GetString($b, $p.Value, $n)
    $p.Value = $p.Value + $n
    return $s
}

# ------------------------------------------------------------------ 读原版
$srcFull = (Resolve-Path -LiteralPath $Source).Path
$orig = [IO.File]::ReadAllBytes($srcFull)
Write-Host ("输入 : {0}  {1:N0} 字节" -f $srcFull, $orig.Length)
Write-Host ("sha256: {0}" -f (Get-FileHash -LiteralPath $srcFull -Algorithm SHA256).Hash.ToLower())

$PTR_POS = 0x24f38
$SIG_POS = 0x24f40
$NENT = 49
$ASM_NAME = 'RaiDrive.Mount.dll'

$manifest = Get-I64 $orig $PTR_POS
if ($manifest -le 0 -or $manifest -ge $orig.Length) {
    throw "bundle 指针无效（0x$('{0:x}' -f $manifest)）"
}

$p = [ref]([long]($manifest + 12))
$null = Read-Str $orig $p
$hdrPos = $p.Value                     # deps.json offset 在这
$p.Value = $p.Value + 40               # 4 个 qword + flags8

$ents = @()
for ($i = 0; $i -lt $NENT; $i++) {
    $offPos = $p.Value
    $off = Get-I64 $orig $offPos
    $size = Get-I64 $orig ($offPos + 8)
    $p.Value = $p.Value + 24
    $typ = $orig[$p.Value]; $p.Value = $p.Value + 1
    $name = Read-Str $orig $p
    $ents += [pscustomobject]@{ OffPos = $offPos; Off = $off; Size = $size; Name = $name }
}

$ent = $ents | Where-Object { $_.Name -eq $ASM_NAME } | Select-Object -First 1
if (-not $ent) { throw "bundle 里找不到 $ASM_NAME" }
$idx = [array]::IndexOf($ents, $ent)

$oldOff = $ent.Off
$oldSize = $ent.Size
$oldEnd = $oldOff + $oldSize
$asm = New-Object byte[] $oldSize
[Array]::Copy($orig, $oldOff, $asm, 0, $oldSize)
Write-Host ("assembly: @0x{0:x}  {1:N0} 字节" -f $oldOff, $oldSize)

# ------------------------------------------------------------------ PE 结构
$pe = Get-U32 $asm 0x3c
$nsec = Get-U16 $asm ($pe + 6)
$optsz = Get-U16 $asm ($pe + 20)
$opt = $pe + 24
$magic = Get-U16 $asm $opt
if ($magic -eq 0x20b) { $dd = $opt + 112 } else { $dd = $opt + 96 }
$fileAlign = Get-U32 $asm ($opt + 36)
$sectAlign = Get-U32 $asm ($opt + 32)
$secBase = $opt + $optsz

$secs = @()
for ($i = 0; $i -lt $nsec; $i++) {
    $o = $secBase + $i * 40
    $nm = [Text.Encoding]::ASCII.GetString($asm, $o, 8).TrimEnd([char]0)
    $secs += [pscustomobject]@{
        Name = $nm; Hdr = $o
        Vs = Get-U32 $asm ($o + 8); Va = Get-U32 $asm ($o + 12)
        Rs = Get-U32 $asm ($o + 16); Rp = Get-U32 $asm ($o + 20)
    }
}
function Convert-RvaToOff([uint32]$rva) {
    foreach ($s in $secs) {
        $span = [Math]::Max($s.Vs, $s.Rs)
        if ($rva -ge $s.Va -and $rva -lt ($s.Va + $span)) { return [long]($s.Rp + ($rva - $s.Va)) }
    }
    return [long]-1
}

# ------------------------------------------------- MethodDef 表位置与行宽
$clrOff = Convert-RvaToOff (Get-U32 $asm ($dd + 14 * 8))
$mdOff  = Convert-RvaToOff (Get-U32 $asm ($clrOff + 8))
$verLen = Get-U32 $asm ($mdOff + 12)
$q = [long]($mdOff + 16 + $verLen)
$nstreams = Get-U16 $asm ($q + 2)
$q = $q + 4
$tablesOff = [long]-1
for ($i = 0; $i -lt $nstreams; $i++) {
    $sOff = Get-U32 $asm $q
    $q = $q + 8
    $z = $q
    while ($asm[$z] -ne 0) { $z = $z + 1 }
    $nm = [Text.Encoding]::ASCII.GetString($asm, [int]$q, [int]($z - $q))
    $q = [long](($z + 1 + 3) -band (-bnot 3))
    if ($nm -eq '#~') { $tablesOff = $mdOff + $sOff }
}
if ($tablesOff -lt 0) { throw '找不到 #~ 表流' }

$heapSizes = $asm[$tablesOff + 6]
$valid = [BitConverter]::ToUInt64($asm, [int]($tablesOff + 8))
$t = [long]($tablesOff + 24)
$rows = New-Object uint32[] 64
for ($i = 0; $i -lt 64; $i++) {
    if (($valid -band ([uint64]1 -shl $i)) -ne 0) { $rows[$i] = Get-U32 $asm $t; $t = $t + 4 }
}
$strIdx  = if ($heapSizes -band 1) { 4 } else { 2 }
$blobIdx = if ($heapSizes -band 4) { 4 } else { 2 }
$guidIdx = if ($heapSizes -band 2) { 4 } else { 2 }
function Idx([int]$tb) { if ($rows[$tb] -ge 0x10000) { 4 } else { 2 } }
function CodedSize([int[]]$tbls, [int]$bits) {
    $m = 0; foreach ($x in $tbls) { if ($rows[$x] -gt $m) { $m = [int]$rows[$x] } }
    if ($m -ge (1 -shl (16 - $bits))) { 4 } else { 2 }
}

$tblSize = @{}
$tblSize[0x00] = 2 + $strIdx + 3 * $guidIdx
$tblSize[0x01] = (CodedSize @(0x00, 0x1A, 0x23, 0x01) 2) + 2 * $strIdx
$tblSize[0x02] = 4 + 2 * $strIdx + (CodedSize @(0x02, 0x01, 0x1B) 2) + (Idx 0x04) + (Idx 0x06)
$tblSize[0x03] = (Idx 0x04)
$tblSize[0x04] = 2 + $strIdx + $blobIdx
$tblSize[0x05] = (Idx 0x06)

$mdTable = $t
foreach ($tb in 0x00, 0x01, 0x02, 0x03, 0x04, 0x05) {
    if (($valid -band ([uint64]1 -shl $tb)) -ne 0) { $mdTable += [long]$rows[$tb] * $tblSize[$tb] }
}
$rowSize = 4 + 2 + 2 + $strIdx + $blobIdx + (Idx 0x08)
Write-Host ("MethodDef @0x{0:x}  rows={1}  rowsize={2}" -f $mdTable, $rows[6], $rowSize)

function Get-Body([int]$row) {
    $rva = Get-U32 $asm ($mdTable + ($row - 1) * $rowSize)
    $o = Convert-RvaToOff $rva
    if ($o -lt 0) { throw "row $row 的 RVA 不在任何段内" }
    $u = Get-U16 $asm $o
    $hs = (($u -shr 12) -band 0xF) * 4
    if ($hs -eq 0) { $n = ($u -shr 2) -band 0x3F; $start = $o + 1 }
    else { $n = Get-U32 $asm ($o + 4); $start = $o + $hs }
    $code = New-Object byte[] $n
    [Array]::Copy($asm, $start, $code, 0, $n)
    return [pscustomobject]@{ Rva = $rva; Off = $o; Size = $n; Il = $code }
}

function Hex([byte[]]$b) { ($b | ForEach-Object { $_.ToString('x2') }) -join '' }

# ------------------------------------------------------------------ 前置断言
foreach ($row in 2747, 2748, 2749, 3554, 7121) {
    $b = Get-Body $row
    if (-not (($b.Size -eq 4) -and ($b.Il[3] -eq 0x2A))) {
        throw ("row {0} 不是预期的 4 字节桩（codesize={1} il={2}）—— 输入可能已经打过补丁，或版本不匹配" -f $row, $b.Size, (Hex $b.Il))
    }
}
Write-Host '前置检查通过：5 个相关方法均为原版 4 字节桩'

# --------------------------------------------------------- 追加三个方法体
# 注意：不要用 @{ 2747 = ... } 这种整数键的哈希表——PowerShell 会把 $TINY[2747]
# 当【位置索引】而不是键索引，取到 $null。用显式数组最稳。
$TINY = @(
    [pscustomobject]@{ Row = 2747; Il = [byte[]](0x16, 0x2A) }   # ldc.i4.0 ; ret
    [pscustomobject]@{ Row = 2748; Il = [byte[]](0x2A) }         # ret
    [pscustomobject]@{ Row = 2749; Il = [byte[]](0x2A) }         # ret
)

$last = $secs | Sort-Object { $_.Rp + $_.Rs } | Select-Object -Last 1
$blobStart = [long](($last.Rp + $last.Rs + $fileAlign - 1) -band (-bnot ($fileAlign - 1)))
$blobRva = [long]($last.Va + ($blobStart - $last.Rp))
Write-Host ("新方法体 blob: file 0x{0:x}  rva 0x{1:x}  （追加进 {2}）" -f $blobStart, $blobRva, $last.Name)

$blob = New-Object System.Collections.Generic.List[byte]
$newRva = @{}
foreach ($item in $TINY) {
    [byte[]]$il = $item.Il
    $codesize = $il.Length
    $padded = New-Object byte[] ((([int]($codesize / 4)) + 1) * 4)
    [Array]::Copy($il, $padded, $codesize)
    $total = 12 + $padded.Length

    $hdr = New-Object byte[] 12
    # fat header: u16 = 3 个 dword 的头（12 字节）<< 12 | flags
    # flags 0x3 = Fat；0x10 = InitLocals（这里没有局部变量，必须清掉）
    $word = [uint16]((3 -shl 12) -bor 0x03)
    [Array]::Copy([BitConverter]::GetBytes($word), 0, $hdr, 0, 2)
    [Array]::Copy([BitConverter]::GetBytes([uint16]8), 0, $hdr, 2, 2)          # MaxStack = 8
    [Array]::Copy([BitConverter]::GetBytes([uint32]$codesize), 0, $hdr, 4, 4)  # CodeSize（字节数！）
    [Array]::Copy([BitConverter]::GetBytes([uint32]0), 0, $hdr, 8, 4)          # LocalVarSigTok = 0

    $newRva[[int]$item.Row] = [long]($blobRva + $blob.Count)
    $blob.AddRange([byte[]]$hdr)
    $blob.AddRange($padded)
}
Write-Host ("三个方法体共 {0} 字节" -f $blob.Count)

# ------------------------------------------------- 扩展 .rsrc（不新增 section）
$newRawEnd = [long](($blobStart + $blob.Count + $fileAlign - 1) -band (-bnot ($fileAlign - 1)))
$newVs = [long](($last.Va + ($newRawEnd - $last.Rp) + $sectAlign - 1) -band (-bnot ($sectAlign - 1)))
[Array]::Copy([BitConverter]::GetBytes([uint32]($newVs - $last.Va)), 0, $asm, $last.Hdr + 8, 4)
[Array]::Copy([BitConverter]::GetBytes([uint32]($newRawEnd - $last.Rp)), 0, $asm, $last.Hdr + 16, 4)
Write-Host ("  {0}: vs 0x{1:x} -> 0x{2:x},  rawsz 0x{3:x} -> 0x{4:x}" -f $last.Name, $last.Vs, ($newVs - $last.Va), $last.Rs, ($newRawEnd - $last.Rp))

$imgOff = $opt + 56
$oldImg = Get-U32 $asm $imgOff
$newImg = [uint32](($newVs + $sectAlign - 1) -band (-bnot ($sectAlign - 1)))
if ($newImg -lt $oldImg) { $newImg = $oldImg }
[Array]::Copy([BitConverter]::GetBytes($newImg), 0, $asm, $imgOff, 4)
Write-Host ("  SizeOfImage 0x{0:x} -> 0x{1:x}" -f $oldImg, $newImg)

# 把 asm 拼到目标长度再落 blob
$asmLen = [long]$blobStart
if ($asm.Length -lt $asmLen) { $tmp = New-Object byte[] $asmLen; [Array]::Copy($asm, $tmp, $asm.Length); $asm = $tmp }
$asmLen = $blobStart + $blob.Count
if ($asm.Length -lt $asmLen) { $tmp = New-Object byte[] $asmLen; [Array]::Copy($asm, $tmp, $asm.Length); $asm = $tmp }
$blob.CopyTo($asm, [int]$blobStart)
if ($asm.Length -lt $newRawEnd) { $tmp = New-Object byte[] $newRawEnd; [Array]::Copy($asm, $tmp, $asm.Length); $asm = $tmp }

foreach ($row in $newRva.Keys) {
    [Array]::Copy([BitConverter]::GetBytes([uint32]$newRva[$row]), 0, $asm, $mdTable + ($row - 1) * $rowSize, 4)
}
Write-Host ("已改写 {0} 个 MethodDef RVA" -f $newRva.Count)

# --------------------------------------------------------------- 塞回 bundle
$delta = [long]$asm.Length - $oldSize
$buf = New-Object byte[] ($orig.Length + $delta)
[Array]::Copy($orig, 0, $buf, 0, $oldOff)
[Array]::Copy($asm, 0, $buf, $oldOff, $asm.Length)
$tailLen = $orig.Length - $oldEnd
[Array]::Copy($orig, $oldEnd, $buf, $oldOff + $asm.Length, $tailLen)
Write-Host ("bundle 拼接：assembly {0:N0} -> {1:N0}  (delta {2:+#;-#;0})" -f $oldSize, $asm.Length, $delta)

$newManifest = $manifest + $delta

# 关键：manifest 本身位于 assembly 之后，拼接后整体后移了 $delta。
# 所以所有在【原缓冲区】里算出来的位置都要先 +$delta 再写。
# 而 header 里的指针值只在它指向的位置 >= oldEnd 时才平移（deps.json 会动，
# runtimeconfig.json 在 assembly 之前，不动）。
function Shift-IfAfter([long]$v) { if ($v -ge $oldEnd) { return $v + $delta } else { return $v } }

$hdrPosN = $hdrPos + $delta
$newDeps = Shift-IfAfter (Get-I64 $buf $hdrPosN)
[Array]::Copy([BitConverter]::GetBytes([int64]$newDeps), 0, $buf, $hdrPosN, 8)
$rcPos = $hdrPosN + 16
[Array]::Copy([BitConverter]::GetBytes([int64](Shift-IfAfter (Get-I64 $buf $rcPos))), 0, $buf, $rcPos, 8)

# 每个条目：位置后移；越过被替换 assembly 的偏移值也要平移
for ($i = 0; $i -lt $NENT; $i++) {
    $posN = $ents[$i].OffPos + $delta
    $cur = Get-I64 $buf $posN
    [Array]::Copy([BitConverter]::GetBytes([int64](Shift-IfAfter $cur)), 0, $buf, $posN, 8)
    if ($i -eq $idx) {
        [Array]::Copy([BitConverter]::GetBytes([int64]$asm.Length), 0, $buf, ($posN + 8), 8)
    }
}
[Array]::Copy([BitConverter]::GetBytes([int64]$newManifest), 0, $buf, $PTR_POS, 8)

# 证书表在 overlay 之后，跟着平移
$pe2 = Get-U32 $buf 0x3c
$opt2 = $pe2 + 24
$magic2 = Get-U16 $buf $opt2
$dd2 = $opt2 + $(if ($magic2 -eq 0x20b) { 112 } else { 96 })
$certPos = $dd2 + 4 * 8
$certOff = Get-U32 $buf $certPos
if ($certOff -ne 0) {
    [Array]::Copy([BitConverter]::GetBytes([uint32]($certOff + $delta)), 0, $buf, $certPos, 4)
}

# 原 release 签名已对不上，清零
[Array]::Clear($buf, $SIG_POS, 32)

# 重算 PE 校验和
$chkPos = [long]($opt2 + 64)
[Array]::Copy([BitConverter]::GetBytes([uint32]0), 0, $buf, $chkPos, 4)
[Array]::Copy([BitConverter]::GetBytes([RdPatchHelper]::Checksum($buf, $chkPos)), 0, $buf, $chkPos, 4)

# 注意：不能用 [IO.Path]::GetFullPath()——它按 PowerShell 进程的当前目录解析，
# 而不是按当前 PSDrive 位置。用户 `cd` 到别处或用相对路径 -Out 时会写到错误位置
# （实测会抛 DirectoryNotFoundException）。用 Join-Path 走 PowerShell 语义。
$outFull = if ([IO.Path]::IsPathRooted($Out)) { $Out } else { Join-Path (Get-Location).ProviderPath $Out }
$outDir = [IO.Path]::GetDirectoryName($outFull)
if ($outDir -and -not (Test-Path -LiteralPath $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}
[IO.File]::WriteAllBytes($outFull, $buf)
$sha = (Get-FileHash -LiteralPath $outFull -Algorithm SHA256).Hash.ToLower()

Write-Host ''
Write-Host ("写出 : {0}  {1:N0} 字节" -f $outFull, $buf.Length)
Write-Host "sha256: $sha"
if ($sha -eq '0acccd3ee5f914d1aaba87d8f6bc38e19374379f296a55505937ad06a1a259fa') {
    Write-Host '与已交付的 v4 完全一致 ✔' -ForegroundColor Green
} else {
    Write-Host '注意：与已知 v4 的 sha256 不同（换了原版版本？）' -ForegroundColor Yellow
}
Write-Host ''
Write-Host '下一步：.\verify-patch.ps1 -Path ' -NoNewline
Write-Host $outFull
