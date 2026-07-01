# 商业版发布脚本说明

面向 **社区版 / 充值版 / 定制版** 三档产品的打包与分发。输出目录为 `dist/{版本名}/{平台}/`，压缩包位于 `dist/` 根目录。

非商业（开发联调）请使用上级目录的 [`publish-windows.sh`](../publish-windows.sh)、[`publish-linux.sh`](../publish-linux.sh)，输出到 `dist/win-x64`、`dist/linux-arm64`。

完整目录布局见 [`docs/dist-layout.md`](../../docs/dist-layout.md)。

---

## 目录结构

```
scripts/commercial/
├── README.md              # 本说明
├── publish-common.sh      # 公共函数（版本、拷贝、打 zip/tar）
├── publish-windows.sh     # Windows x64 单版本发布
├── publish-linux.sh       # Linux 单版本发布
├── publish-all.sh         # 批量发布（多版本 × 双平台）
├── sync-runtime.sh        # 仅同步配置/点表/文档（不重新编译）
├── publish-windows.ps1    # Windows 本机 PowerShell 入口
└── editions/              # 各版本随包 README
    ├── 社区版/README.txt
    ├── 充值版/README.txt
    └── 定制版/README.txt
```

---

## 版本与配置

| 版本 | 配置文件 | 典型用途 |
|------|----------|----------|
| 社区版 | `configs/社区版.appsettings.json` | 2 单元，本地联调/评估 |
| 充值版 | `configs/充值版.appsettings.json` | 2 单元，线上体验部署 |
| 定制版 | `configs/定制版.appsettings.json` | 完整项目拓扑 |

发布时会将对应 `configs/*.appsettings.json` 复制为包内 `appsettings.json`，并附带点表 CSV、`log4net.config` 及版本 README。

---

## 发布命令

在项目根目录执行（需已安装 .NET 8 SDK）：

```bash
# 单版本 Windows（默认社区版）
./scripts/commercial/publish-windows.sh
EDITION=定制版 ./scripts/commercial/publish-windows.sh

# 单版本 Linux（默认社区版 + linux-arm64 + Release）
./scripts/commercial/publish-linux.sh
EDITION=充值版 RID=linux-arm64 CONFIG=Release ./scripts/commercial/publish-linux.sh

# 三版本 × Windows + Linux
./scripts/commercial/publish-all.sh
./scripts/commercial/publish-all.sh 社区版    # 仅社区版

# 仅更新已发布目录中的 CSV/配置/文档（改点表后常用，无需重新 dotnet publish）
./scripts/commercial/sync-runtime.sh
./scripts/commercial/sync-runtime.sh 社区版 win-x64
```

**Windows 本机（PowerShell）：**

```powershell
$env:EDITION = "社区版"
.\scripts\commercial\publish-windows.ps1
```

---

## 输出产物

```
dist/
├── 社区版/win-x64/、社区版/linux-arm64/
├── 充值版/…
├── 定制版/…
├── EssSimulator-社区版-win-x64.zip
├── EssSimulator-社区版-linux-arm64.tar.gz
└── …（各版本 × 各平台）
```

每个平台目录内含：`EssSimulator` 可执行文件、运行时配置与点表、`start.bat` 或 `start.sh`、版本 `README.txt`。

---

## 与非商业脚本的区别

| 项目 | 非商业（`scripts/publish-*.sh`） | 商业（`scripts/commercial/`） |
|------|----------------------------------|-------------------------------|
| 输出路径 | `dist/win-x64`、`dist/linux-arm64` | `dist/{版本名}/{平台}/` |
| 配置 | 仓库根 `appsettings.json` | `configs/{版本}.appsettings.json` |
| 版本 | 单一 | 社区版 / 充值版 / 定制版 |
| 压缩包名 | `EssSimulator-win-x64.zip` 等 | `EssSimulator-{版本}-{平台}.zip` 等 |
| 随包文档 | 平台 README | 版本 README + 平台 README |

---

## 维护提示

- 修改 `configs/*.appsettings.json` 或点表 CSV 后，对已发布目录可只跑 `sync-runtime.sh`，不必全量编译。
- 新增商业版本：在 `configs/` 增加配置、在 `editions/` 增加 README，并在 `publish-common.sh` 的 `ALL_EDITIONS` 与 `edition_config_file` 中注册。
- 社区版随包说明与 `scripts/README.txt` 内容一致；充值版/定制版见各自 `editions/*/README.txt`。
