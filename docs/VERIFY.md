# 如何独立验证本补丁

不要信任我。以下步骤可以让你自己确认：

1. 这个 exe 确实只改了那 3 个方法
2. 授权和列表相关代码确实没被动

---

## 方法 A：跑校验脚本（最简单）

### Windows

```powershell
.\scripts\verify-patch.ps1 -Path .\RaiDrive.Mount.noads.v4.exe
```

期望输出（最后一行）：

```
RESULT: 补丁正确，且授权/列表方法保持原版
```

如果输出 `RESULT: 有问题（N 项不通过），不要使用！`，说明文件不对。

> 如果提示「无法加载文件，因为在此系统上禁止运行脚本」：
> ```powershell
> Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
> ```
> 这只影响当前窗口，关闭即失效。

### Linux / macOS

```bash
python3 scripts/verify_patch.py RaiDrive.Mount.noads.v4.exe
```

---

## 方法 B：只信任 sha256

如果你连脚本都不想跑：

```powershell
Get-FileHash .\RaiDrive.Mount.noads.v4.exe -Algorithm SHA256
```

| 版本 | sha256 |
|---|---|
| **v4 去广告版（当前）** | `0acccd3ee5f914d1aaba87d8f6bc38e19374379f296a55505937ad06a1a259fa` |
| 原版 2026.10.0 | `ec4ae608fb0943de94d213e76c4e726e3b2bf3b790772a0a9735b1db210b3263` |
| v3（有 bug，勿用） | `6dc0252397b74654deb3d239c96ac520b6317357f93d342ab45eff3f05f42d7e` |
| v2（有 bug，勿用） | `41cf79a674245b5d82f2a8b27a33215c1326cc7f5a87fbf97e1bf0b3538b2987` |

只要 sha256 是 `0acccd3e…`，就说明文件内容和我发布的一模一样，
中间没有人改过。

---

## 方法 C：自己从原版重打一遍

这是最强的验证——如果我从原版重新生成，产物 sha256 应该完全一致。

```powershell
# 需要一个「未修改的原版」 RaiDrive.Mount.exe
# （从官方安装器装一份，或从 RaiDrive 官网下载安装）

.\scripts\apply-patch.ps1 `
    -Source .\RaiDrive.Mount.original.exe `
    -Out    .\my-build.exe

Get-FileHash .\my-build.exe -Algorithm SHA256
```

期望输出：

```
sha256: 0acccd3ee5f914d1aaba87d8f6bc38e19374379f296a55505937ad06a1a259fa
与已交付的 v4 完全一致 ✔
```

脚本内置了结构断言：如果原版结构不对（版本不符、方法已打过补丁），
它会**拒绝执行**而不是产出一个坏文件。

---

## 方法 D：完全手动，不跑任何脚本

### 1. 确认内嵌程序集的结构

`.NET single-file bundle` 的 manifest 指针在 `.data` 段固定偏移 `0x24f38`。

```python
import struct
d = open('RaiDrive.Mount.noads.v4.exe','rb').read()
manifest = struct.unpack_from('<q', d, 0x24f38)[0]
print(hex(manifest))   # v4: 0x1e6835b
```

从 manifest 里解析出 49 个条目，第 0 个是 `RaiDrive.Mount.dll`，
位置 `0x1c2000`，大小 `8,164,352`（原版是 `8,163,840`，差 512 字节）。

### 2. 确认只差 512 字节

把两个 exe 的内嵌 dll 都抽出来做逐字节 diff，
**只应该有 5 处差异 + 尾部 512 字节**：

| 位置 | 内容 |
|---|---|
| `0x1b8..0x1b9` | `.rsrc` 的 `VirtualSize` |
| `0x1c1` | `.rsrc` 的 `SizeOfRawData` |
| `0x264bac`, `0x264bbe`, `0x264bd0` | 3 个 MethodDef 的 RVA |
| `0x7c9200..0x7c93ff` | 新增的 512 字节（3 个方法体） |

用本仓库的工具：

```bash
python3 tools/extract_asm.py RaiDrive.Mount.noads.v4.exe v4.dll
python3 tools/extract_asm.py RaiDrive.Mount.original.exe orig.dll
python3 tools/diff_bytes.py orig.dll v4.dll
```

### 3. 确认方法归属

用 `tools/metadata.py` 可以直接读出每个 MethodDef 行号的声明类型：

```bash
python3 tools/metadata.py v4.dll 2747 2748 2749 3554 7121
```

期望看到：

```
row  2747  声明类型 RaiDrive.Center.ViewModels.AdvertisingViewModel
row  2748  声明类型 RaiDrive.Center.ViewModels.AdvertisingViewModel
row  2749  声明类型 RaiDrive.Center.ViewModels.AdvertisingViewModel
row  3554  声明类型 …MMHNAEHBMHEHEMIIDENJMIDCMLMIIEEMBIJF  （MainWindow）
row  7121  声明类型 …BGEIDHMCKHHBDFKNJEBKKNFCBECKLFOBMHLJ  （订阅策略）
```

### 4. 确认 IL 合法

```bash
python3 tools/ilcheck.py v4.dll
```

对比 v4 与原版的「IL 异常集合」应该完全相同
（.NET Reactor 壳本身有 21 处无法解析的桩，两版一致）：

```bash
python3 tools/ilcheck.py orig.dll v4.dll
```

---

## 方法 E：用 ILSpy 查看改后的代码

```bash
dotnet tool install -g ilspycmd
ilspycmd -t "RaiDrive.Center.ViewModels.AdvertisingViewModel" v4.dll
```

应该看到这三个方法变成了：

```csharp
public bool DDCNKJOPGDFNCNIDOCGIACEHKBFHAEBGGFOO()
{
    return false;
}

public void ADBKADLCEELBLMKIFFOIAFIICGMJIOCCIMME()
{
}

public void MGHDEPFHFHJPOJNECKIJGJHOJNKHAOLEBFJI()
{
}
```

---

## 真机行为怎么确认

装好之后，打开日志：

```powershell
Get-Content "$env:ProgramData\OpenBoxLab\RaiDrive Mount\log\center-$env:USERNAME.log" -Wait -Tail 40
```

**判定标准（三条都要）**：

1. **没有** `[INF] Ads N: 200 728x90 …` 这类行
   （原版会有：`[INF] Ads 2: 200 728x90 completed (False, ConnectionAborted) visible:True https://ads.raidrive.com/...`）
2. 「添加连接」按钮可用（不灰）、已有连接能挂载
3. `your@email.com was granted (Standard)` ← **括号里必须有 Standard**

**不能出现**：

```
[FTL] License Failed: ...
[FTL] License Failed: Common Language Runtime detected an invalid program.
```

> `granted ()` 括号为空 = 授权失败。
> 注意：在 Wine 下括号为空是 TPM 缺失的必然结果，与补丁无关
> （原版在 Wine 下报得一模一样）。真机有 TPM 2.0 才能拿到 `(Standard)`。
