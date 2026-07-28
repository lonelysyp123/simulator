# 商业版发布脚本

面向社区版 / 充值版 / 定制版。**完整说明见 [docs/项目编译说明.md](../../docs/项目编译说明.md)。**  
分档边界见 [docs/产品分档与交付边界.md](../../docs/产品分档与交付边界.md)。  
充值接入见 [docs/充值版授权网关约定.md](../../docs/充值版授权网关约定.md)。

## 常用命令

```bash
./scripts/commercial/check-edition-drift.sh      # 发布前档位差异检查
./scripts/commercial/sync-custom-from-root.sh    # 根 appsettings → 定制版模板（可 --dry-run）
EDITION=社区版 ./scripts/commercial/publish-windows.sh
EDITION=定制版 ./scripts/commercial/publish-linux.sh
./scripts/commercial/publish-all.sh
./scripts/commercial/sync-runtime.sh
```

Windows PowerShell：`$env:EDITION="社区版"; .\scripts\commercial\publish-windows.ps1`

## 本目录文件

| 文件 | 作用 |
|------|------|
| `check-edition-drift.sh` | 社区/充值绑定与 NoGui 差异、定制版单元数漂移检查 |
| `sync-custom-from-root.sh` | 将根 `appsettings.json` 同步为定制版模板（叠加交付 GUI） |
| `publish-common.sh` | 版本解析、拷贝运行时、打 zip/tar |
| `publish-windows.sh` / `publish-linux.sh` | 单版本单平台 |
| `publish-all.sh` | 批量发布 |
| `sync-runtime.sh` | 不编译，仅同步配置/点位表（**固定 common**）/文档 |
| `editions/*/README.txt` | 各版本随包说明 |
