# dist 发布目录结构

`dist/` 根目录**仅保留**三个版本文件夹与压缩包，不再使用 `win-x64/`、`community/` 等平级目录。

```
dist/
├── 社区版/
│   ├── win-x64/          # Windows 自包含包
│   └── linux-arm64/      # Linux 自包含包
├── 充值版/
│   ├── win-x64/
│   └── linux-arm64/
├── 定制版/
│   ├── win-x64/
│   └── linux-arm64/
├── EssSimulator-社区版-win-x64.zip
├── EssSimulator-社区版-linux-arm64.tar.gz
├── EssSimulator-充值版-win-x64.zip
├── EssSimulator-充值版-linux-arm64.tar.gz
├── EssSimulator-定制版-win-x64.zip
└── EssSimulator-定制版-linux-arm64.tar.gz
```

## 版本与配置

| 版本 | 配置文件 | 说明 |
|------|----------|------|
| 社区版 | `configs/社区版.appsettings.json` | 2 单元，本地联调/评估 |
| 充值版 | `configs/充值版.appsettings.json` | 2 单元，线上体验部署 |
| 定制版 | `configs/定制版.appsettings.json` | 完整项目拓扑（与仓库根 `appsettings.json` 同步维护） |

## 发布命令

```bash
# 单版本 Windows
EDITION=社区版 ./scripts/commercial/publish-windows.sh

# 单版本 Linux
EDITION=定制版 ./scripts/commercial/publish-linux.sh

# 三版本 × 双平台
./scripts/commercial/publish-all.sh

# 仅同步 CSV/配置/文档（不重新编译）
./scripts/commercial/sync-runtime.sh
./scripts/commercial/sync-runtime.sh 社区版 win-x64
```

Windows 本机：`$env:EDITION="社区版"; .\scripts\commercial\publish-windows.ps1`

**开发联调（非商业）** 使用根目录脚本，输出 `dist/win-x64`、`dist/linux-arm64`：

```bash
./scripts/publish-windows.sh
./scripts/publish-linux.sh
```

详见 [`scripts/commercial/README.md`](../scripts/commercial/README.md) 与 [`scripts/README.md`](../scripts/README.md)。

## 迁移旧目录

若存在旧的 `dist/win-x64`、`dist/linux-arm64`、`dist/community`，可手动迁入对应版本后删除：

```bash
mkdir -p dist/社区版 dist/充值版 dist/定制版
# 示例：旧 win-x64 若含完整 8 单元配置 → 定制版
mv dist/win-x64 dist/定制版/ 2>/dev/null || true
mv dist/linux-arm64 dist/社区版/ 2>/dev/null || true
rm -rf dist/community
```

压缩包命名已变更，旧包 `EssSimulator-win-x64.zip` 等可删除。
