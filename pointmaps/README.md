# 点位表（Point Maps）

运行时程序从工作目录读取固定文件名：`emu.csv`、`em.csv`、`bms_bank.csv`、`bms_rack.csv`、`lc.csv`。

解析优先级（见 `Protocol/Modbus/PointMapPathResolver.cs`）：

1. **设备型号选型**（本目录 `models/`，运行期可在 Web「系统配置」页切换）
2. 工作目录 / 输出目录根下的同名文件（sync / 发布脚本复制结果）
3. `pointmaps/common/` 及其他版本目录（开发兜底）

## 设备型号（models/）

按 **设备类型 → 设备型号 → 点表文件** 组织，运行期在系统配置界面选型，
持久化到 `configs/topology/device-models.json`，重启后生效：

```
pointmaps/models/
  bms/          type.json + common/ lc/ battery/（bms_bank.csv + bms_rack.csv）
  emu/          type.json + standard/（emu.csv）
  em/           type.json + standard/（em.csv）
  lc/           type.json + standard/（lc.csv）
```

- `type.json`：`{ id, name, description, files }`，`files` 声明该设备类型使用的点表文件名。
- `model.json`：`{ id, name, description }`，型号展示名与说明。
- 新增型号：在对应设备类型目录下新建子目录，放入点表文件与 `model.json` 即可被自动识别，无需改代码。
- PV 点表（`pv_logger.csv` 等）暂未纳入型号体系，仍从根目录读取。

## 版本目录（legacy 兜底）

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
发布脚本会同时将 `pointmaps/models/` 整目录复制到输出目录，供运行期型号选型使用。
