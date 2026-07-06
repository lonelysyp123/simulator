# 点位表（Point Maps）

运行时程序仍从工作目录读取固定文件名：`emu.csv`、`em.csv`、`bms_bank.csv`、`bms_rack.csv`、`lc.csv`。  
本目录按**版本**管理源文件，发布或本地联调时复制到输出目录。

## 版本一览

| 目录 | 说明 | 主要差异 |
|------|------|----------|
| [common](./common/) | 默认联调 / **商业发布** | develop 命名 `bms_bank`；精简 `bms_rack` |
| [lc](./lc/) | LocalControl | param 命名 `bms_bank` + `lc.csv` |
| [battery](./battery/) | 电芯级遥测 | 全量 `bms_rack`（Pack/电芯数据点） |

各目录含 `version.json`（`id` 与目录名一致）。

## 本地联调

```bash
./scripts/sync-pointmaps-to-root.sh           # 默认 common
./scripts/sync-pointmaps-to-root.sh lc
./scripts/sync-pointmaps-to-root.sh battery
```

## 发布（开发脚本）

```bash
./scripts/publish-linux.sh battery
./scripts/publish-linux.sh lc
./scripts/publish-windows.sh common
```

或环境变量：`POINTMAP_VERSION=lc ./scripts/publish-linux.sh`

**商业发布**（`scripts/commercial/`）固定 `common`，不可覆盖。

可选值：`common`、`lc`、`battery`（与目录名一致）。
