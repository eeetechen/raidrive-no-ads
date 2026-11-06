#Requires -Version 5.1
<#
.SYNOPSIS
    一键安装：备份 → 停进程 → 校验 → 覆盖 → 复验。失败自动回滚。

.DESCRIPTION
    取代 README 里那 5 条手工命令。关键差异：

      * 覆盖前**强制**用 verify-patch.ps1 校验源文件，校验不过直接中止
      * 覆盖前把当前 exe 备份成 RaiDrive.Mount.exe.bak.<时间戳>（不覆盖原有 .bak）
      * 覆盖后**重新读回**目标文件算 sha256，跟期望值比对，不一致自动回滚
      * 需要管理员权限；没有就明确报错，不做半套

.PARAMETER Path
    要安装的 exe。默认 .\bin\RaiDrive.Mount.noads.v4.exe

.PARAMETER AppDir
    RaiDrive 安装目录。

.PARAMETER Force
    跳过交互确认。

.EXAMPLE
    .\install.ps1
#>
[CmdletBinding()]
param(
    [string]$Path,
    [string]$AppDir = 'C:\Program Files\OpenBoxLab\RaiDrive Mount',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $here
if (-not $Path) { $Path = Join-Path $root 'bin\RaiDrive.Mount.noads.v4.exe' }
$expected = '0acccd3ee5f914d1aaba87d8f6bc38e19374379f296a55505937ad06a1a259fa'

function Head($t) { Write-Host ''; Write-Host "=== $t ===" -ForegroundColor Cyan }

# ---------------------------------------------------------------- 权限
$pr = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $pr.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host '需要管理员权限。请用「以管理员身份运行」的 PowerShell 再执行。' -ForegroundColor Red
    exit 1
}

# ---------------------------------------------------------------- 源文件
Head '1. 校验源文件'
$Path = (Resolve-Path -LiteralPath $Path).Path
$srcHash = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLower()
Write-Host "  $Path"
Write-Host "  sha256 = $srcHash"
if ($srcHash -ne $expected) {
    Write-Host "  期望 $expected" -ForegroundColor Red
    Write-Host '  与交付的 v4 不一致，拒绝安装。' -ForegroundColor Red
    exit 1
}
& (Join-Path $here 'verify-patch.ps1') -Path $Path
if ($LASTEXITCODE -ne 0) {
    Write-Host '  verify-patch.ps1 未通过，拒绝安装。' -ForegroundColor Red
    exit 1
}

# ---------------------------------------------------------------- 目标
Head '2. 目标目录与备份'
$target = Join-Path $AppDir 'RaiDrive.Mount.exe'
if (-not (Test-Path -LiteralPath $target)) { throw "目标不存在：$target" }
$beforeHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToLower()
Write-Host "  当前 exe sha256 = $beforeHash"
if ($beforeHash -eq $expected) {
    Write-Host '  已经是 v4，无需重复安装。' -ForegroundColor Yellow
    exit 0
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backup = Join-Path $AppDir "RaiDrive.Mount.exe.bak.$stamp"
Copy-Item -LiteralPath $target -Destination $backup -Force
Write-Host "  已备份 -> $backup"

if (-not $Force) {
    $ans = Read-Host '确认覆盖？(y/N)'
    if ($ans -notmatch '^[yY]') { Write-Host '已取消。'; exit 1 }
}

# ---------------------------------------------------------------- 停进程
Head '3. 停止进程与服务'
Get-Process -Name 'RaiDrive*' -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host ("  停止 PID={0} {1}" -f $_.Id, $_.ProcessName)
    try { $_ | Stop-Process -Force -ErrorAction Stop } catch { Write-Host "    失败：$($_.Exception.Message)" -ForegroundColor Yellow }
}
foreach ($sn in 'RaiDriveMountService') {
    $s = Get-Service -Name $sn -ErrorAction SilentlyContinue
    if ($s -and $s.Status -eq 'Running') {
        Stop-Service -Name $sn -Force -ErrorAction SilentlyContinue
        Write-Host "  已停止服务 $sn"
    }
}
Start-Sleep -Seconds 2
$still = @(Get-Process -Name 'RaiDrive*' -ErrorAction SilentlyContinue)
if ($still.Count) {
    Write-Host '  仍有进程在跑，覆盖会失败。请手动退出托盘图标后重试。' -ForegroundColor Red
    exit 1
}

# ---------------------------------------------------------------- 覆盖
Head '4. 覆盖并复验'
Copy-Item -LiteralPath $Path -Destination $target -Force
$afterHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToLower()
Write-Host "  覆盖后 sha256 = $afterHash"
if ($afterHash -ne $expected) {
    Write-Host '  校验不一致，开始回滚…' -ForegroundColor Red
    Copy-Item -LiteralPath $backup -Destination $target -Force
    $rb = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToLower()
    Write-Host ("  已回滚，当前 sha256 = {0}（应等于 {1}）" -f $rb, $beforeHash)
    exit 1
}
Write-Host '  校验一致 ✔' -ForegroundColor Green

Head '5. 下一步'
Write-Host '  1) 启动：& "' + $target + '"'
Write-Host '  2) 另开窗口看日志：'
Write-Host ('     Get-Content "$env:ProgramData\OpenBoxLab\RaiDrive Mount\log\center-$env:USERNAME.log" -Wait -Tail 40')
Write-Host '  3) 判定：无 Ads 行 / 添加连接不灰 / granted 括号里是 Standard'
Write-Host ''
Write-Host ("  回滚：Copy-Item `"{0}`" `"{1}`" -Force" -f $backup, $target)
