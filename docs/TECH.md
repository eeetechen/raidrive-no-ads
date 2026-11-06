# 技术细节：为什么只能这么改

本文补充 README 里没展开的部分。

---

## 1. 安装包是 Advanced Installer 引导器，payload 加密

最初的 `RaiDrive.Mount_2026.10.0_x64.exe`（36,775,080 字节）里，
真正的应用 payload 被 **AES-256 加密**（整段熵 8.0，MSCF 头齐全但数据全乱）。

→ 离线解密不可行。**必须先用官方安装器装一遍，拿到安装目录里的明文**
（未加密的 .NET 单文件 bundle）。

---

## 2. 程序集在 bundle 内部，不在安装目录

```
RaiDrive.Mount.exe（.NET single-file bundle）
├── bundle manifest @0x1e6815b  （version 6.0，49 个条目）
├── bundle 指针存在 .data @0x24f38
├── 全部内容在 PE overlay（最后一个 section .rsrc 之后）
└── [0] RaiDrive.Mount.dll  @0x1c2000  8,163,840 字节   ← 要改的是这个
```

**安装目录里根本没有这个 dll。**

这解释了最常见的失败做法：把一个改好的 `RaiDrive.Mount.dll`
放进 `C:\Program Files\OpenBoxLab\RaiDrive Mount\`，然后发现毫无效果——
因为程序加载的是 bundle 里的那份，不是目录里的。

---

## 3. .NET Reactor 加壳：86.1% 的方法是桩

| 统计 | 数值 |
|---|---|
| 类型总数 | 1,243 |
| 方法总数 | 11,179 |
| 有方法体 | 10,811 |
| **≤8 字节的占位桩** | **9,305（86.1%）** |
| 真实 IL 的方法 | 1,331 |
| 总 IL 字节 | 193,368 |

真实逻辑在 `RuntimeHelpers.PrepareMethod` 触发的 JIT 过程中才被解密出来。

→ **不能用「整体反混淆再重建」**：那样会大面积绕过壳、破坏功能。
→ **只能最小化补丁**：只改必要的几个方法，其余原样保留。

### 3.1 标识符随机化导致大量撞名

`DDCNKJOPGDFNCNIDOCGIACEHKBFHAEBGGFOO` 这个 34 字符的名字，
**同时属于两个完全不同的类**：

| 声明类型 | 语义 |
|---|---|
| `RaiDrive.Center.ViewModels.AdvertisingViewModel` | 广告开关（**该改**） |
| `DHFMIIJKPIMFDLOFKNBKNKNFLEGDDCGOFJHN.BGEIDHMCKHH…` | 订阅策略判定（**不该改**） |

两者签名完全一样（都是 `instance bool ()`），名字也一样。

> **最重要的方法论教训：绝对不能按方法名判定归属，
> 必须按 MethodDef 行号 + 声明类型。**

反例：有一个版本就是因为按名字匹配，多改了后一个，
导致所有功能判定不可用、按钮变灰、连不上。

---

## 4. CoreCLR 的 PE 校验（最耗时的部分）

以下每条都是「改一处 → 跑一次」实测出来的硬约束：

| # | 约束 | 违反后果 |
|---|---|---|
| 1 | 不能新增 section header，只能扩展已有 section | `BadImageFormatException: Bad IL format` |
| 2 | `SizeOfImage` 必须按 `SectionAlignment`（0x2000）对齐 | 同上 |
| 3 | 扩展 section 时 `SizeOfRawData` 必须**同步**加 `VirtualSize` | 同上 |
| 4 | 方法体 RVA 必须 = `section.va + (file_off - section.raw_ptr)` | 同上 |
| 5 | fat header 第一 u16 = `(dwords << 12) \| flags`，size 字段是 **dword 数**不是字节数 | 同上 |
| 6 | **有 `InitLocals` 就必须有有效的局部变量签名** | `InvalidProgramException` |
| 7 | 文件尾部追加 padding 无害 | — |

第 6 条是 v2/v3 两版翻车的直接原因，详见下节。

---

## 5. 为什么补丁体只有 4 字节

`2747` 原来的真实 IL（JIT 时解密出来的）是 **160 字节**的状态机：

```
2B 05                       br.s +5
28 ABF35648                 call ...
20 01000000                 ldc.i4.1
FE 0E 0000                  ldarg.s 0
FE 0C 0000                  ldloc.s 0
45 03000000                 switch (3 targets)
...
```

它使用了**局部变量**（`ldloc.s 0`）。但磁盘上那个方法的桩头是：

```
fat header: flags=0x3 (Fat, InitLocals=0)
            LocalVarSigTok = 0x00000000   ← 没有局部变量签名
```

如果原样把 160 字节写回去，JIT 会读到 `ldloc` 但找不到局部变量声明，
于是抛：

```
[FTL] License Failed: Common Language Runtime detected an invalid program.
```

**v4 的做法**：不发解密体，直接发**极小合法 IL**，不引入任何局部变量：

| 方法 | 补丁体 | 含义 |
|---|---|---|
| 2747 | `16 2A` | `ldc.i4.0; ret` → `return false` |
| 2748 | `2A` | `ret` → 空方法 |
| 2749 | `2A` | `ret` → 空方法 |

这样与空的 `localsig` 自洽，JIT 不会拒绝。

---

## 6. 广告是否真的不显示了

静态上看，`2747` 恒 `false` 之后，主窗口状态机走 `IL_0172` 分支，
该分支**只**关闭广告可见性：

```csharp
IL_0420:
if (AdvertisingViewModel.DDCNKJOP()) {
    ...
    goto IL_00a6;      // 走广告流程（ADBKADLCE → FGLJMLKEH → MGHDEPFH）
}
goto IL_0172;
IL_0172:
    _stateManager.PDDIHJOPPHINHONEMINADGBMJACGBFLBAONF();  // VisibleAdvertisement = false
    num4 = 19;         // → 2749 空操作 → 循环
```

关键点是：**驱动器列表高度（`3554`）不在这条路径上**，
它由另一条独立路径 `KCGGPLMLMKJFNMPHLFMIECNGJLACJNJPFNCI`
（在登录状态变化时调用）负责，**不受广告开关影响**。

所以 v2 那种「列表看不见」的理论上不会复现。
但这条结论**只在反编译层面验证过**，真机行为仍需实测。

---

## 7. 校验脚本如何防止我把事情搞砸

`verify-patch.ps1` 每次都检查 5 个方法，其中：

**必须有补丁的 3 个**：
- `2747` → 必须正好是 `162a`
- `2748` → 必须正好是 `2a`
- `2749` → 必须正好是 `2a`

**绝对不能动的 2 个**：
- `3554` → 必须还是原版 `0000002a`（4 字节）
- `7121` → 必须还是原版 `0000172a`（4 字节）

任意一条不符，退出码为 1，`install.bat` / `install.ps1` 会拒绝安装。
