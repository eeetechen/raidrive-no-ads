#Requires -Version 5.1
<#
.SYNOPSIS
    一键体检：在动 RaiDrive 之前，先确认这台机器「能不能装」。

.DESCRIPTION
    交付包原来的 README 默认了三件并不成立的事，这个脚本把它们逐条测出来：

      1. 脚本能跑       —— 文件是否带 UTF-8 BOM、ExecutionPolicy 是否拦
      2. 有管理员权限   —— 没有就写不进 Program Files
      3. 装的是哪一版   —— 原版 / v2 / v3 / v4 / 未知，以及 .bak 是什么

    只读检查，不修改任何东西。退出码 0 = 可以装；1 = 有阻断项。

.EXAMPLE
    .\check-env.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Continue'
$fail = 0
$app = 'C:\Program Files\OpenBoxLab\RaiDrive Mount'
$KNOWN = @{
    'ec4ae608fb0943de94d213e76c4e726e3b2bf3b790772a0a9735b1db210b3263' = '原版（未修改）'
    '0acccd3ee5f914d1aaba87d8f6bc38e19374379f296a55505937ad06a1a259fa' = 'v4 去广告版'
    '6dc0252397b74654deb3d239c96ac520b6317357f93d342ab45eff3f05f42d7e' = 'v3（有问题：打断授权）'
    '41cf79a674245b5d82f2a8b27a33215c1326cc7f5a87fbf97e1bf0b3538b2987' = 'v2（有问题：列表不可用）'
    '3512a00c43c4b6092152d997ce5b7511a4d63c381ec88633f88642b0b75fbcbf' = '原版（本机安装器落地的那一份，代码与原版逐字节相同，仅签名时间戳不同）'
}

$script:fail = 0

function Head($t) { Write-Host ''; Write-Host "=== $t ===" -ForegroundColor Cyan }
function Ok($t) { Write-Host "  [OK]   $t" -ForegroundColor Green }
function Warn($t) { Write-Host "  [警告] $t" -ForegroundColor Yellow }
function Bad($t) { Write-Host "  [阻断] $t" -ForegroundColor Red; $script:fail++ }

Head '1. 运行时'
Write-Host ("  PowerShell {0}（{1} 位）" -f $PSVersionTable.PSVersion, [Environment]::Is64BitProcess)
$isWinPS = $PSVersionTable.PSVersion.Major -lt 6
if ($isWinPS) { Warn '这是 Windows PowerShell 5.1：无 BOM 的 UTF-8 脚本里含中文会解析失败（本包脚本已补 BOM）' }

$pol = Get-ExecutionPolicy -Scope Process
if ($pol -eq 'Restricted' -or $pol -eq 'AllSigned') {
    Bad ("ExecutionPolicy={0}，本脚本能跑到这里说明已绕过；但子脚本可能仍被拦。" -f $pol)
    Write-Host '         先执行：Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass'
} else {
    Ok "ExecutionPolicy(Process)=$pol"
}

Head '2. 写入权限（决定能不能覆盖 Program Files）'
$id = [Security.Principal.WindowsIdentity]::GetCurrent()
$pr = New-Object Security.Principal.WindowsPrincipal($id)
$elevated = $pr.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
Write-Host "  当前用户：$($id.Name)"
if ($elevated) {
    Ok '已提权（管理员）'
} else {
    $probe = Join-Path $app ('.dsh_probe_' + [guid]::NewGuid().ToString('N').Substring(0, 8))
    $writable = $false
    try { [IO.File]::WriteAllText($probe, 'x'); Remove-Item -LiteralPath $probe -Force; $writable = $true } catch { }
    if ($writable) {
        Ok '未提权，但安装目录可写（ACL 被放开过）'
    } else {
        Bad '未提权且安装目录不可写 —— 无法覆盖 RaiDrive.Mount.exe'
        Write-Host '         处理：右键 PowerShell → 以管理员身份运行，然后再跑一次'
        Write-Host '         或者让安装目录对当前用户可写（不推荐，等价于降低安全性）'
    }
}

Head '3. 安装目录与版本判定'
if (-not (Test-Path -LiteralPath $app)) {
    Bad "安装目录不存在：$app（可能装在别处或未安装 2026.10.0）"
} else {
    Ok "安装目录存在"
    foreach ($n in 'RaiDrive.Mount.exe', 'RaiDrive.Mount.exe.bak') {
        $p = Join-Path $app $n
        if (Test-Path -LiteralPath $p) {
            $h = (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLower()
            $tag = if ($KNOWN.ContainsKey($h)) { $KNOWN[$h] } else { '未知（自制 / 其它版本）' }
            $sz = (Get-Item -LiteralPath $p).Length
            Write-Host ("  {0,-24} {1,12:N0} B  {2}" -f $n, $sz, $tag)
            Write-Host ("  {0,-24} {1}" -f '', $h)
        } else {
            Warn "$n 不存在"
        }
    }
}

Head '4. 进程与服务'
$procs = @(Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -like 'RaiDrive*' })
if ($procs.Count) {
    Warn ("有 {0} 个 RaiDrive 进程在跑，覆盖前必须先全部退出：" -f $procs.Count)
    $procs | ForEach-Object { Write-Host ("         PID={0,-7} {1}" -f $_.Id, $_.ProcessName) }
} else {
    Ok '没有 RaiDrive 进程在跑'
}
foreach ($sn in 'RaiDriveMountService', 'WinFsp.Launcher') {
    $s = Get-Service -Name $sn -ErrorAction SilentlyContinue
    if ($s) { Write-Host ("  {0,-24} {1} / {2}" -f $sn, $s.Status, $s.StartType) }
    else { Warn "$sn 服务不存在" }
}

Head '结论'
if ($fail -eq 0) {
    Write-Host '  可以安装。按 docs\WINDOWS-CHECKLIST.md 走，或直接：' -ForegroundColor Green
    Write-Host '    .\scripts\install.ps1 -Path .\bin\RaiDrive.Mount.noads.v4.exe'
    exit 0
} else {
    Write-Host ("  有 {0} 项阻断，先解决再装。" -f $fail) -ForegroundColor Red
    exit 1
}
