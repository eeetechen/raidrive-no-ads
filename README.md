# RaiDrive 去广告补丁（2026.10.0）

去掉 RaiDrive Mount 顶部和列表里的广告横幅。**只改 3 个方法，共 4 个字节。**

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![Version](https://img.shields.io/badge/RaiDrive-2026.10.0-green.svg)](#)

---

## 给不想折腾的人（30 秒）

> 不需要懂命令，不需要手动开管理员 PowerShell。
> **只需要下载一个文件**——去广告程序会自己拉。

**第 1 步**：在右边 [Releases](../../releases) 页面下载 **`install.bat`**。

**第 2 步**：双击它。

- 会弹出一个「用户账户控制」窗口 → 点 **是**
- 然后全是自动的：下载程序 → 校验 → 关闭 RaiDrive → 备份 → 替换 → 复验
- 看到 `Done! Ad banner removed.` 就成功了

**第 3 步**：打开 RaiDrive，广告横幅应该没有了。

> 建议把 `restore.bat` 也下载到**同一个文件夹**，将来想还原方便。

### 它会自动做什么

| 情况 | 行为 |
|---|---|
| 当前目录已有 `RaiDrive.Mount.noads.v4.exe` 且校验通过 | 直接用，不联网 |
| 有文件但校验不通过 | 丢弃，重新下载 |
| 没有这个文件 | 自动从 GitHub Release 下载（31 MB），**下载后再校验一次** |
| 下载失败（断网 / GitHub 被墙） | 报错并打印下载地址，手动下载后放进同一文件夹再双击 |

自动下载优先用 `curl.exe`（Win10 1803+ 自带），失败则回退 PowerShell。文件小于 1000 字节视为下载中断。

**想手动下载**的话，Release 里的 `RaiDrive.Mount.noads.v4.exe` 也可以直接下，和 `install.bat` 放一起就行——有就用，没有才拉。

### 出问题怎么办

双击 `restore.bat` 就能还原回安装前的版本。安装时的备份在 `backup/` 文件夹里。

### 卸载 / 升级 RaiDrive

| 情况 | 做法 |
|---|---|
| 想还原 | 双击 `restore.bat` |
| RaiDrive 升级了 | 补丁会失效（新版本覆盖了 exe），重新跑一次 `install.bat` 即可 |
| 彻底卸载 RaiDrive | 正常卸载即可，补丁不残留任何东西 |

---

## 到底改了什么

补丁只改 `RaiDrive.Mount.exe` 内嵌的 `RaiDrive.Mount.dll` 里的 **3 个方法**，
全部属于 `RaiDrive.Center.ViewModels.AdvertisingViewModel`。

| MethodDef 行号 | 原方法 | 原行为 | 改后 |
|---|---|---|---|
| **2747** | `DDCNKJOPGDFNCNIDOCGIACEHKBFHAEBGGFOO()` | 读广告服务器 JSON 开关，返回「要不要显示广告」 | `return false`（`16 2A`） |
| **2748** | `ADBKADLCEELBLMKIFFOIAFIICGMJIOCCIMME()` | 显示顶部横幅 | 空方法（`2A`） |
| **2749** | `MGHDEPFHFHJPOJNECKIJGJHOJNKHAOLEBFJI()` | 加载广告 WebView2 | 空方法（`2A`） |

补丁体合计 **4 字节**（2 + 1 + 1）。

### 为什么这三处就够

广告 UI 完全由这三个方法门控：

```csharp
if (AdvertisingViewModel.DDCNKJOP()) {   // ← 2747 恒 false 之后，整块都不执行
    AdvertisingViewModel.ADBKADLCE();    // ← 2748 空操作（双保险）
    FGLJMLKEH();                         // ← 列表高度，未改动
    AdvertisingViewModel.MGHDEPFH();     // ← 2749 空操作（双保险）
}
```

`2747` 恒返回 `false` 后，后面两个方法根本不会被调用。
即使别处调用了，`2748`/`2749` 也是空方法，不会加载 WebView2。

### 没有改动的东西

- **其余 11,176 个方法体，一个字节都没碰**
- `RaiDrive.Mount.Service.x64.exe`（挂载服务）—— 独立的程序，不含广告逻辑
- 任何 XAML / 资源 / BAML —— 广告界面是代码控制的，不需要改资源
- **授权、订阅、连接、挂载相关的所有代码**

其中有两处是**绝对不能碰**的，补丁脚本每次都会断言它们没被改动：

| 行号 | 用途 | 改了的后果 |
|---|---|---|
| **3554** | `xDriveListView.Height`（驱动器列表高度） | 列表看不见 / 点不到 |
| **7121** | 订阅策略判定（广告是否可投放） | 授权失效、按钮变灰 |

---

## 文件说明

```
.
├── install.bat                        ← 双击安装（自动提权 + 自动下载 exe）
├── restore.bat                        ← 双击还原
├── scripts/
│   ├── verify-patch.ps1               校验任一个 exe 是不是正确的补丁版
│   ├── apply-patch.ps1                从原版重新生成补丁（升级后可复用）
│   ├── install.ps1                    PowerShell 版安装（带自动回滚）
│   ├── check-env.ps1                  PowerShell 版环境体检
│   ├── verify_patch.py                Python 版校验（Linux/macOS 可用）
│   └── repatch.py                     Python 版生成
├── tools/                             ← 我自己写的解析工具，不依赖 ILSpy
│   ├── ilcheck.py                     独立 IL 合法性校验器
│   ├── metadata.py                    独立元数据 / 方法归属解析
│   ├── bundle_diff.py                 .NET single-file bundle 逐条比对
│   ├── inspect.py                     PE / 段 / MethodDef 结构 dump
│   ├── extract_asm.py                 从 bundle 抽取程序集
│   └── diff_bytes.py                  字节级 diff
├── src/                               ← 自建 C# 工具源码
│   ├── mdscan/                        PE + CLI 元数据扫描器（9 个模式）
│   ├── dumper/                        运行时 IL dumper（需在目标机运行）
│   └── dec/                           基于 ICSharpCode.Decompiler 的反编译器
├── decompiled/                        ← 反编译产物（仅供研究）
│   ├── RaiDrive.Mount.decompiled.cs   主程序集全量反编译（726,313 行）
│   └── service/                       挂载服务全量反编译（928 个 .cs）
└── docs/
    ├── TECH.md                        技术细节：为什么只能这么改
    ├── VERIFY.md                      如何独立验证本补丁
    └── BUILD.md                       从原版重新构建
```

---

## 技术背景（简版）

真正的难点不在“找到广告代码”，而在于：

1. **`.NET Reactor` 加壳** —— 方法名全被随机化，且大量撞名。
   `DDCNKJOPGDFNCNIDOCGIACEHKBFHAEBGGFOO` 这个名字同时属于两个完全不同的类。
   → **必须按 MethodDef 行号 + 声明类型判定归属，绝不能按名字。**

2. **程序集在单文件 bundle 内部** —— 安装目录里根本没有 `RaiDrive.Mount.dll`。
   往目录里放一个改好的 dll 完全无效（这是最常见的失败做法）。

3. **CoreCLR 的 PE 校验很严** —— 改完的 IL 必须同时满足：
   - 不能新增 section header，只能扩展已有 section
   - `SizeOfImage` 必须按 `SectionAlignment`（0x2000）对齐
   - 加了 section 的 `SizeOfRawData` 必须同步加 `VirtualSize`
   - **有 `InitLocals` 就必须有有效的局部变量签名**
   - 方法体 RVA 必须 = `section.va + (file_off - section.raw_ptr)`

4. **86.1% 的方法是桩** —— 11,179 个方法里 9,305 个是 ≤8 字节的占位桩。
   → 不能“整体反混淆再重建”，那样会大面积绕过壳、破坏功能。
   **只能最小化补丁。**

### 为什么补丁体只有 4 字节而不是“原方法解密后改一下”

`2747` 原来的真实 IL 是 160 字节的带局部变量的状态机。
如果把它解密后写回，方法头里的 `LocalVarSigTok` 是 0（桩头是空的），
就会得到**非法 IL**，运行时直接抛
`Common Language Runtime detected an invalid program.`

> 这正是 v2/v3 两版翻车的原因——不是“改错了方法”，
> 而是“写了带局部变量却不声明局部变量签名的 IL”。

v4 的做法是**不发解密体，只发极小合法 IL**（不引入任何局部变量），
与空 localsig 自洽。

---

## 自己验证（不信任我）

```powershell
# Windows PowerShell / PowerShell 7
.\scripts\verify-patch.ps1 -Path .\RaiDrive.Mount.noads.v4.exe
# 期望最后一行：RESULT: 补丁正确，且授权/列表方法保持原版
```

```bash
# Linux / macOS
python3 scripts/verify_patch.py RaiDrive.Mount.noads.v4.exe
```

校验脚本做的事：

1. 算 sha256，和已知版本比对
2. 解析 `.NET single-file bundle`，找到内嵌的 `RaiDrive.Mount.dll`
3. 解析 PE 段表、`SizeOfImage`、`SectionAlignment`
4. 按 ECMA-335 逐张表累加算出 MethodDef 表位置（不是猜的）
5. **三个应该被补丁的方法，字节必须正好是补丁后的样子**
6. **两个绝对不能动的方法，必须是原版 4 字节桩**

详情见 [docs/VERIFY.md](docs/VERIFY.md)。

---

## 已知限制

| 限制 | 说明 |
|---|---|
| **只支持 2026.10.0** | 其它版本结构不同，`apply-patch.ps1` 会拒绝执行而不是产出坏文件 |
| **升级后失效** | 新版本 exe 会覆盖补丁，需重新打 |
| **签名失效** | 覆盖后原版 Authenticode 签名必然失效，Windows 可能提示“未知发布者”。点「仍要运行」即可 |
| **广告请求仍会发出** | 本补丁只阻止广告**显示**（横幅不渲染、WebView2 不加载）。要连 `ads.raidrive.com` 的 HTTP 请求也掐掉，需要另改网络层，本仓库未做 |
| **杀软可能误报** | 修改过签名的 exe 常被启发式误报。用上面的 sha256 自证即可 |

---

## 免责声明

本项目仅供**技术研究与学习**，用于了解 .NET 单文件打包、.NET Reactor 加壳、
PE/CLI 元数据结构和 IL 校验机制。

- RaiDrive 是 OpenBoxLab Inc. 的商业软件，版权归其所有
- 本项目**不提供**任何绕过授权、订阅或付费验证的手段——
  授权相关代码（`3554`、`7121`）保持原版未动，且校验脚本每次都会检查
- 使用本补丁可能违反 RaiDrive 的最终用户许可协议
- **请在使用前自行确认你所在地区的法律法规，以及你是否拥有该软件的合法授权**
- 使用本补丁造成的一切后果由使用者自行承担

如果你喜欢 RaiDrive，请[购买正版](https://www.raidrive.com/pricing)支持开发者。

---

## 许可

本仓库中**由本项目作者编写**的代码（`scripts/`、`tools/`、`src/`、`docs/`）
采用 [GPL-3.0](LICENSE) 许可。

`decompiled/` 目录下的内容是 RaiDrive 的**反编译产物**，版权归 OpenBoxLab Inc. 所有，
**不适用** GPL-3.0，仅作为研究资料提供，请勿再分发。

详见 [NOTICE](NOTICE)。

---

## 致谢

- [ILSpy](https://github.com/icsharpcode/ILSpy) / ICSharpCode.Decompiler — 反编译
- [WinFsp](https://github.com/winfsp/winfsp) — RaiDrive 依赖的文件系统驱动
