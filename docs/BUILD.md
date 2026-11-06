# 从原版重新构建

当 RaiDrive 发布新版本、补丁失效时，用本文的方法重新打一次。

---

## 前置条件

1. **原版 exe**（未修改过的 `RaiDrive.Mount.exe`）
   - 从官方安装器装一份，或
   - 用 `restore.bat` 还原，或
   - 从别处获取同版本原版
2. 确认版本是 **2026.10.0**

> **重要**：`apply-patch.ps1` 内置了结构断言。
> 如果输入不是预期的原版结构（版本不符、或者已经打过补丁），
> 它会**拒绝执行**，而不是产出坏文件。

---

## 方法 1：PowerShell（推荐，Windows 自带）

```powershell
.\scripts\apply-patch.ps1 `
    -Source "C:\path\to\RaiDrive.Mount.original.exe" `
    -Out    ".\RaiDrive.Mount.noads.exe"
```

正常输出：

```
输入 : ...\RaiDrive.Mount.original.exe  31,897,224 字节
sha256: ec4ae608fb0943de94d213e76c4e726e3b2bf3b790772a0a9735b1db210b3263
assembly: @0x1c2000  8,163,840 字节
MethodDef @0x258a98  rows=11179  rowsize=18
前置检查通过：5 个相关方法均为原版 4 字节桩
新方法体 blob: file 0x7c9200  rva 0x7cc600  （追加进 .rsrc）
三个方法体共 48 字节
  .rsrc: vs 0x19a5c8 -> 0x19c000,  rawsz 0x19a600 -> 0x19a800
  SizeOfImage 0x7ce000 -> 0x7ce000
已改写 3 个 MethodDef RVA
bundle 拼接：assembly 8,163,840 -> 8,164,352  (delta +512)

写出 : ...\RaiDrive.Mount.noads.exe  31,897,736 字节
sha256: 0acccd3ee5f914d1aaba87d8f6bc38e19374379f296a55505937ad06a1a259fa
与已交付的 v4 完全一致 ✔
```

最后一行说「完全一致」就对了。

### 怎么拿到原版 exe

如果你之前用 `install.bat` 装过，原版在 `backup/` 文件夹里：

```
backup\RaiDrive.Mount.exe.20261106-154233.bak
```

直接把它当作 `-Source`。

---

## 方法 2：Python（Linux / macOS）

```bash
python3 scripts/repatch.py RaiDrive.Mount.original.exe RaiDrive.Mount.noads.exe
```

---

## 新版本 RaiDrive 怎么办

如果 RaiDrive 升级到比如 2026.11.0，上面的脚本**会失败**（可能报错拒绝，也可能产出不同的 sha256）。

原因是结构可能变了：

| 可能变化 | 影响 |
|---|---|
| bundle 条目数变了 | `verify-patch.ps1` 里硬编码的 `NENT = 49` 不对 |
| bundle 指针位置变了 | `PTR_POS = 0x24f38` 不对 |
| 方法行号变了 | `2747/2748/2749` 不对 |
| 广告逻辑重构了 | 这 3 个方法可能不再是门控点 |

遇到这种情况需要重新分析。基本流程：

### 1. 确认还是 .NET single-file bundle

```bash
# 找 bundle manifest 指针
python3 tools/inspect.py RaiDrive.Mount.dll
# 或者直接看 exe 的 .data 段
```

### 2. 定位 AdvertisingViewModel

```bash
ilspycmd -t "RaiDrive.Center.ViewModels.AdvertisingViewModel" RaiDrive.Mount.dll
```

注意：**类名不会变**（`RaiDrive.Center.ViewModels.AdvertisingViewModel`，
因为这是命名空间 + 类名，可能没被 Reactor 随机化）。
但**方法名会变**（每次构建都随机）。

### 3. 从行号反查方法

用 `tools/metadata.py` 列出该类型的所有方法行号：

```bash
python3 tools/metadata.py RaiDrive.Mount.dll 2720 2721 2722 ... 2763
```

或者直接列出 TypeDef 里名为 `AdvertisingViewModel` 的类型覆盖的行区间。

### 4. 识别三个目标方法

在反编译输出里找这三个特征：

| 特征 | 对应 v4 的 row |
|---|---|
| `return jObject.Value<bool>(...)` — 读广告 JSON 开关 | 2747 |
| 含 `WebBrowser.Height = 90.0` 和 `IsActivated = true` | 2748 |
| 含 `IsActivated = false` 和 `WebBrowser.Height = 0.0` | 2749 |

**绝对不要**按名字匹配——名字会撞车。

### 5. 改成极小 IL 并回填

修改 `scripts/apply-patch.ps1` 里的这几个常量：

```powershell
$PTR_POS   = 0x24f38        # bundle 指针位置
$NENT      = 49             # bundle 条目数
$ASM_NAME  = 'RaiDrive.Mount.dll'

# 以及前置断言里的行号
foreach ($row in 2747, 2748, 2749, 3554, 7121) { ... }
```

然后重新跑。

---

## 构建产物校验清单

生成之后一定要验：

```powershell
# 1. 结构是否正确
.\scripts\verify-patch.ps1 -Path .\RaiDrive.Mount.noads.exe

# 2. IL 是否合法（对比原版异常集合）
python tools/ilcheck.py orig.dll new.dll

# 3. 是不是只改了 3 个方法
python tools/diff_bytes.py orig.dll new.dll
```

第 3 步期望看到 **5 处差异 + 尾部 512 字节**（见 [VERIFY.md](VERIFY.md)）。

如果差异多于这个，说明改多了，**不要发布**。

---

## 已知的坑（都实际踩过）

| 坑 | 症状 | 正确做法 |
|---|---|---|
| 把改好的 dll 放进安装目录 | 完全无效 | 必须改 bundle 内的程序集 |
| 按方法名匹配 | 改到同名但不同类的方法 → 功能全挂 | 按 MethodDef 行号 + 声明类型 |
| 把解密后的 IL 原样写回 | `License Failed: invalid program` | 发极小合法 IL，不引入局部变量 |
| 新增 section header | `BadImageFormatException` | 只能扩展已有 section |
| 改了 section 大小但没同步 VirtualSize | 同上 | 两个字段一起改 |
| 改 `3554` | 驱动器列表不可见 | 不要碰 |
| 改 `7121` | 授权失效、按钮变灰 | 不要碰 |
| PowerShell 脚本存成无 BOM 的 UTF-8 | PS 5.1 下中文变乱码、语法错误 | 存成 **UTF-8 with BOM** |
| `.bat` 里写中文注释 | cmd 按 GBK 解析，命令被截断 | `.bat` 里只用 ASCII |
