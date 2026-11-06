<#
.SYNOPSIS
    校验一个 RaiDrive.Mount.exe 是不是「去广告 v4」补丁版，并说明它到底改了什么。

.DESCRIPTION
    纯 PowerShell 字节解析，不需要 Python、不需要 .NET SDK、不需要反编译工具。

    检查项：
      1. 文件 sha256（识别是不是已知的原版 / 已知的 v4 产物）
      2. 外层 PE：是不是 .NET single-file bundle，manifest 在哪个偏移
      3. bundle 内 RaiDrive.Mount.dll：段表、SizeOfImage、SectionAlignment
      4. MethodDef 表位置（按元数据表逐张累加算出来，不是猜的）
      5. 三个「应该被补丁」的方法：字节必须正好是补丁后的样子
      6. 两个「绝对不能被补丁」的方法：必须是原版 4 字节桩

    退出码：0 通过；1 不通过。

.EXAMPLE
    .\verify-patch.ps1 -Path .\RaiDrive.Mount.exe
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Path
)

$ErrorActionPreference = 'Stop'

$KNOWN = @{
    'ec4ae608fb0943de94d213e76c4e726e3b2bf3b790772a0a9735b1db210b3263' = '这是【原版未修改】的 exe，广告不会被去掉'
    '0acccd3ee5f914d1aaba87d8f6bc38e19374379f296a55505937ad06a1a259fa' = '这是【v4 去广告版】'
    '6dc0252397b74654deb3d239c96ac520b6317357f93d342ab45eff3f05f42d7e' = '这是【v3 有问题的版本】——不要用（会让授权失效）'
    '41cf79a674245b5d82f2a8b27a33215c1326cc7f5a87fbf97e1bf0b3538b2987' = '这是【v2 有问题的版本】——不要用（会让授权失效）'
}

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
function Hex([byte[]]$b) { ($b | ForEach-Object { $_.ToString('x2') }) -join '' }

$full = (Resolve-Path -LiteralPath $Path).Path
$buf = [IO.File]::ReadAllBytes($full)
$sha = (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLower()
$fail = 0

Write-Host ''
Write-Host '=== 1. 文件 ===' -ForegroundColor Cyan
Write-Host ("文件   : {0}" -f $full)
Write-Host ("大小   : {0:N0} 字节" -f $buf.Length)
Write-Host ("sha256 : {0}" -f $sha)
if ($KNOWN.ContainsKey($sha)) {
    $verdict = $KNOWN[$sha]
    if ($verdict -like '*v4*' -and $verdict -like '*去广告版*') {
        Write-Host ("判据   : {0} ✔" -f $verdict) -ForegroundColor Green
    } else {
        Write-Host ("判据   : {0}" -f $verdict) -ForegroundColor Yellow
        if ($verdict -notlike '*原版*') { $fail++ }
    }
} else {
    Write-Host '判据   : 未知的 sha256（自制或其它版本），继续按结构检查' -ForegroundColor Yellow
}

# ------------------------------------------------------------------ bundle
Write-Host ''
Write-Host '=== 2. .NET single-file bundle ===' -ForegroundColor Cyan
$PTR_POS = 0x24f38
$NENT = 49
$ASM_NAME = 'RaiDrive.Mount.dll'

$manifest = Get-I64 $buf $PTR_POS
if ($manifest -le 0 -or $manifest -ge $buf.Length) {
    Write-Host ("manifest 指针无效（0x{0:x}）—— 不是预期的 RaiDrive 2026.10.0 exe" -f $manifest) -ForegroundColor Red
    exit 1
}
$p = [ref]([long]($manifest + 12))
$bundleId = Read-Str $buf $p
$verMaj = Get-U32 $buf $manifest
$verMin = Get-U32 $buf ($manifest + 4)
$cnt = Get-U32 $buf ($manifest + 8)
Write-Host ("manifest @0x{0:x}  version={1}.{2}  bundle_id={3}  条目 {4} 个" -f $manifest, $verMaj, $verMin, $bundleId, $cnt)

$p.Value = $p.Value + 40   # 4 个 qword + flags8
$ents = @()
for ($i = 0; $i -lt $NENT; $i++) {
    $offPos = $p.Value
    $off = Get-I64 $buf $offPos
    $size = Get-I64 $buf ($offPos + 8)
    $p.Value = $p.Value + 24
    $typ = $buf[$p.Value]; $p.Value = $p.Value + 1
    $name = Read-Str $buf $p
    $ents += [pscustomobject]@{ OffPos = $offPos; Off = $off; Size = $size; Type = $typ; Name = $name }
}

$ent = $ents | Where-Object { $_.Name -eq $ASM_NAME } | Select-Object -First 1
if (-not $ent) {
    Write-Host "$ASM_NAME 不在 bundle 里 —— 结构不对" -ForegroundColor Red
    exit 1
}
Write-Host ("{0} @0x{1:x}  {2:N0} 字节" -f $ASM_NAME, $ent.Off, $ent.Size)

$asm = New-Object byte[] $ent.Size
[Array]::Copy($buf, $ent.Off, $asm, 0, $ent.Size)

# ------------------------------------------------------------------ PE
Write-Host ''
Write-Host '=== 3. 内嵌 assembly 的 PE 结构 ===' -ForegroundColor Cyan
$pe = Get-U32 $asm 0x3c
$nsec = Get-U16 $asm ($pe + 6)
$optsz = Get-U16 $asm ($pe + 20)
$opt = $pe + 24
$magic = Get-U16 $asm $opt
$dd = $opt + $(if ($magic -eq 0x20b) { 112 } else { 96 })
$sectAlign = Get-U32 $asm ($opt + 32)
$fileAlign = Get-U32 $asm ($opt + 36)
$sizeOfImage = Get-U32 $asm ($opt + 56)
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
Write-Host ("SectionAlignment=0x{0:x}  FileAlignment=0x{1:x}  SizeOfImage=0x{2:x}  {3} 个段" -f $sectAlign, $fileAlign, $sizeOfImage, $nsec)
foreach ($s in $secs) {
    Write-Host ("  {0,-9} va=0x{1:x6} vs=0x{2:x6} rp=0x{3:x6} rs=0x{4:x6} end=0x{5:x6}" -f $s.Name, $s.Va, $s.Vs, $s.Rp, $s.Rs, ($s.Va + [Math]::Max($s.Vs, $s.Rs)))
}
if ($sectAlign -ne 0x2000) {
    Write-Host ("SectionAlignment 应为 0x2000，实际 0x{0:x} —— CoreCLR 会拒绝加载" -f $sectAlign) -ForegroundColor Red
    $fail++
}
if (($sizeOfImage % $sectAlign) -ne 0) {
    Write-Host 'SizeOfImage 未按 SectionAlignment 对齐 —— CoreCLR 会拒绝加载' -ForegroundColor Red
    $fail++
}

function Convert-RvaToOff([uint32]$rva) {
    foreach ($s in $secs) {
        $span = [Math]::Max($s.Vs, $s.Rs)
        if ($rva -ge $s.Va -and $rva -lt ($s.Va + $span)) { return [long]($s.Rp + ($rva - $s.Va)) }
    }
    return [long]-1
}

# ---------------------------------------------------------- MethodDef 表
Write-Host ''
Write-Host '=== 4. MethodDef 表 ===' -ForegroundColor Cyan
$clrOff = Convert-RvaToOff (Get-U32 $asm ($dd + 14 * 8))
if ($clrOff -lt 0) { Write-Host '找不到 CLI 头' -ForegroundColor Red; exit 1 }
$mdOff = Convert-RvaToOff (Get-U32 $asm ($clrOff + 8))
if ($mdOff -lt 0) { Write-Host '找不到元数据根' -ForegroundColor Red; exit 1 }
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
if ($tablesOff -lt 0) { Write-Host '找不到 #~ 表流' -ForegroundColor Red; exit 1 }

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
    if ($o -lt 0) { return $null }
    $u = Get-U16 $asm $o
    $hs = (($u -shr 12) -band 0xF) * 4
    if ($hs -eq 0) { $n = ($u -shr 2) -band 0x3F; $start = $o + 1 }
    else { $n = Get-U32 $asm ($o + 4); $start = $o + $hs }
    $code = New-Object byte[] $n
    [Array]::Copy($asm, $start, $code, 0, $n)
    return [pscustomobject]@{ Rva = $rva; Off = $o; Size = $n; Il = $code; Hdr = $u }
}

# ------------------------------------------------------------------ 判定
Write-Host ''
Write-Host '=== 5. 三个补丁方法（必须是补丁后的样子）===' -ForegroundColor Cyan
$EXPECT = @(
    @{ Row = 2747; Name = 'AdvertisingViewModel::DDCNKJOP...  (广告开关)'; Hex = '162a' }
    @{ Row = 2748; Name = 'AdvertisingViewModel::ADBKADLC...  (显示横幅)'; Hex = '2a' }
    @{ Row = 2749; Name = 'AdvertisingViewModel::MGHDEPFH...  (加载广告页)'; Hex = '2a' }
)
foreach ($e in $EXPECT) {
    $b = Get-Body $e.Row
    if (-not $b) { Write-Host ("BAD row {0}  RVA 不在段内" -f $e.Row) -ForegroundColor Red; $fail++; continue }
    $h = Hex $b.Il
    if ($h -eq $e.Hex -and $b.Size -eq ($e.Hex.Length / 2)) {
        Write-Host ("OK  row {0}  rva=0x{1:x}  {2}B  il={3}   {4}" -f $e.Row, $b.Rva, $b.Size, $h, $e.Name) -ForegroundColor Green
    } else {
        Write-Host ("BAD row {0}  rva=0x{1:x}  {2}B  il={3}  （期望 {4}B il={5}）   {6}" -f $e.Row, $b.Rva, $b.Size, $h, ($e.Hex.Length / 2), $e.Hex, $e.Name) -ForegroundColor Red
        $fail++
    }
}

Write-Host ''
Write-Host '=== 6. 两个绝对不能动的方法（必须是原版 4 字节桩）===' -ForegroundColor Cyan
$PROTECT = @(
    @{ Row = 3554; Name = 'xDriveListView.Height = ...  (驱动器列表高度)'; Expect = '0000002a' }
    @{ Row = 7121; Name = 'BGEIDHMCKHH..::DDCNKJOP...  (功能授权门)';       Expect = '0000172a' }
)
foreach ($e in $PROTECT) {
    $b = Get-Body $e.Row
    if (-not $b) { Write-Host ("BAD row {0}  RVA 不在段内" -f $e.Row) -ForegroundColor Red; $fail++; continue }
    $h = Hex $b.Il
    if ($b.Size -eq 4 -and $h -eq $e.Expect) {
        Write-Host ("OK  row {0}  rva=0x{1:x}  4B  il={2}   {3}" -f $e.Row, $b.Rva, $h, $e.Name) -ForegroundColor Green
    } else {
        Write-Host ("BAD row {0}  rva=0x{1:x}  {2}B  il={3}  （期望 4B il={4}）   {5}" -f $e.Row, $b.Rva, $b.Size, $h, $e.Expect, $e.Name) -ForegroundColor Red
        $fail++
    }
}

Write-Host ''
if ($fail -eq 0) {
    Write-Host 'RESULT: 补丁正确，且授权/列表方法保持原版' -ForegroundColor Green
    Write-Host ''
    Write-Host '放到 RaiDrive 安装目录覆盖 RaiDrive.Mount.exe 即可。原文件先备份。'
    exit 0
} else {
    Write-Host ("RESULT: 有问题（{0} 项不通过），不要使用！" -f $fail) -ForegroundColor Red
    exit 1
}
