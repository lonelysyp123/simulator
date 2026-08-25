# 点位表（Point Maps）

运行时程序从工作目录读取固定文件名：`emu.csv`、`em.csv`、`bms_bank.csv`、`bms_rack.csv`、`lc.csv`。

解析优先级（见 `Protocol/Modbus/PointMapPathResolver.cs`）：

1. **设备型号选型**（本目录 `models/`，运行期可在 Web「系统配置」页切换）
2. 工作目录 / 输出目录根下的同名文件（sync / 发布脚本复制结果）
3. `pointmaps/common/` 及其他版本目录（开发兜底）

## 设备型号（models/）

按 **设备类型 → 设备型号 → 点表文件** 组织，运行期在系统配置界面选型，
持久化到 `configs/topology/device-models.json`，重启后生效：

EMU 点表另有自动选型规则：组态工程保存/应用时按 PCS 总数自动切换
（2 台 → `standard`，4 台 → `trina_5.5MW`，8 台 → `trina_10MW`，
其余数量保持现有选型，见 `Web/Topology/EmuPointMapAutoSelect.cs`）。

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

## EMU 系统级点位语义（emu.csv SYSTEM 工作表）

EMU 为虚拟聚合模型（每储能单元 1 个 EMU，聚合 N 台 PCS），以下系统级点位
绑定到 `emuN.Emu.*` 后由均分派发/批量语义生效：

| 点位 | 绑定字段 | 语义 |
|------|----------|------|
| `syst4` | `Emu.RemoteControlEnable` | 远程&本地控制使能：0 禁止 / 1 使能 |
| `syst5` | `Emu.RemoteControlMode` | 控制模式：0 本地 / 1 远程（syst4=1 且 syst5=1 时均分生效） |
| `syst6` | `Emu.SystemOperation` | 系统操作（边沿生效）：3 启动 / 4 停止 / 5 待机（目标清零）/ 6 重置（停机清故障锁存） |
| `syst7` | `Emu.BlackStartModeWrite` | 黑启动模式批量写入：0 关闭 / 1 开启（下发所属全部 PCS） |
| `syst1010` | `Emu.TargetActivePower` | EMU 级目标有功（kW，正放负充）；远程使能时按台数均分 |
| `syst1011` | `Emu.TargetReactivePower` | EMU 级目标无功（kvar）；远程使能时按台数均分 |
| `sysyc200~203` | `Emu.TotalPcsCount` 等统计字段 | PCS 总数/运行/告警/故障台数（`MapEmuState` 同步刷新） |

- 单 PCS 直控点位（yt/yx 系列，绑定 `emuN.PcsList[i].*`）在本地模式下照旧生效；
  远程均分生效时以均分值覆盖功率设定。
- standard 版 emu.csv 保持单 PCS 直控点位不变（向后兼容）；trina 型号点表的
  系统级绑定待对应分支合入后手工补入。

## EMU 设备树路径语法（分层构成）

EMU 支持「EMU → group → PCS 支路」分层构成（组态编辑器中用「EMU 分组」模板建模）。
EMU 不参与电气求解，分层仅为协议聚合视图：组内 PCS 与扁平视图共享同一仿真实例，
两条路径读写同一对象。点表 ModelSim 绑定支持以下路径（`N` 从 1 计，`g`/`i`/`k` 从 0 计）：

| 路径 | 目标 | 门控条件 |
|------|------|----------|
| `emuN.PcsList[i].*` | 扁平视图（保持） | i < 单元 PCS 总数（分组时为各组之和） |
| `emuN.Groups[g].PcsList[i].*` | 组内 PCS（与扁平路径同启停/功率控制语义） | g < 分组数且 i < 组内 PCS 数 |
| `emuN.Groups[g].Breaker.*` | 组断路器协议镜像（恒合闸，纯遥测） | g < 分组数且该组绑定断路器 |
| `emuN.Groups[g].Total*` 等聚合字段 | 组聚合遥测（组内 PCS 求和/台数统计） | g < 分组数 |
| `emuN.Transformers[k].*` | 单元变镜像（负载率/功率/油温，纯遥测） | k=0（本期仅 1 台） |
| `emuN.Breaker.*` | EMU 级断路器镜像（开合跟随单元投退） | 机组绑定单元断路器 |

- 无分组的机组：`Groups[*]` 路径在目录编译期自动剔除（未绑定点位维持默认值），
  既有扁平路径不受影响。
- 组级断路器/电表/变压器镜像不产生电气动作，值为合成（断路器恒合闸、
  变压器优先抄电气层单元变真实值，缺失时按组内 PCS 求和兜底）。
- 控制派发范围不变：EMU 级目标仍按全机组 PCS 均分；组级目标派发为后续扩展。

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
