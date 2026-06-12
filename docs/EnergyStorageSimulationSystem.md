# 储能仿真系统方案设计

## 项目目标

- 仿真范围：储能系统（ESS）端到端仿真，包括多路电池堆（Rack）、多台 PCS、主/单元断路器、主变/单元变、计划负载；通过 Modbus TCP 对外提供 BMS、EMU、电表等接口，供 EMS/主站联调。
- 可扩展性：点位映射（CSV）+ `appsettings.json` 参数化；`DataExchange` 点目录驱动遥测/控制；支持多单元拓扑扩展。

## 架构总览

```
┌─────────────────────────────────────────────────────────────┐
│  协议层：ModbusSimServer / DataExchangeSession / ModbusDataSync │
│  映射层：EssSimModelApi (PcsMapper, BmsMapper, EmMapper)      │
└───────────────────────────┬─────────────────────────────────┘
                            │ 对象路径 / ControlEffect
┌───────────────────────────▼─────────────────────────────────┐
│  编排层：EnergyStorageSystem（固定步长主循环）                  │
│  求解层：ElectricalNetwork + NetworkSolver（AC 潮流主路径）     │
│  桥接层：NetworkControlBridge / NetworkStepOrchestrator         │
└───────────────────────────┬─────────────────────────────────┘
                            │ 共用设备实例
┌───────────────────────────▼─────────────────────────────────┐
│  设备层 EssDeviceSimModel/Devices/*                           │
│  PcsDevice · TransformerDevice · BreakerSimulator             │
│  GridSimulator · LoadDevice · MeterSimulator · BmsRackDevice  │
└─────────────────────────────────────────────────────────────┘
```

### 设备模型（`EssDeviceSimModel/Devices/`）

| 设备 | 说明 |
|------|------|
| `PcsDevice` | 有功/无功爬坡（步进内）、并网/离网、黑启动 V/f、直流/交流换算、保护 |
| `TransformerDevice` | 一二次电压/电流、铁损铜损、励磁涌流、无功励磁支路 |
| `BreakerSimulator` | 主断/单元断合分，网络控制面 SSOT |
| `GridSimulator` | PCC 无功—电压反馈 |
| `LoadDevice` | 35kV 负载计划功率 |
| `BmsRackDevice` | BMS Rack 与 DC 端口桥接 |

电池细粒度模型仍在 `EssDeviceSimModel/Battery/`（`BatteryRackSimulator` 等），由 ESS 主循环与 `BmsRackDevice` 耦合。

### 关键设计原则（迁移后）

- **单一实例**：`ess._pcsList`、`ess._mainTransformer`、`ess._unitTransformers` 与 `ElectricalNetwork` 内对应设备为同一对象，经 `NetworkTopologyBuilder` 注入。
- **Solver 主路径**：每 tick 由 `NetworkSolver.Step` 驱动 AC 段；PCS 物理在 `PcsDevice.Update`（DC 耦合）完成。
- **控制面**：断路器、负载计划、BMS 链路等经 `NetworkControlBridge` 写入网络设备。

## 数据流与线程模型

### 仿真主循环（`EnergyStorageSystem.ExecuteAsync`）

固定步长（默认 200ms × `Speedup`）：

1. `NetworkStepOrchestrator.SolverPrimaryStep` — 同步控制意图 → `NetworkSolver.Step` → 回写电网电压/PCS 网侧
2. `Update` — PCS DC 更新、BMS 电流积分
3. `SyncUnitTransformerAfterPcsUpdate` — 离网/黑启动场景校正单元变与站用电分摊
4. `RefreshAllUnitBlackStartBusContexts` — 刷新黑启动母线上下文
5. `NetworkControlBridge.SyncBmsLinksFromRacks` — 刷新 DC 链路供下一周期 Solver 使用

### Modbus 数据交换

- **simEmu\*** / **simBms\*** / **simEm**：`DataExchangeSession`（点目录 CSV + `DataExchange` 配置）
  - 遥测管道：周期读模型写寄存器
  - 控制管道：周期读 FC5/6 写模型；`ControlEffect` 驱动启停、黑启动、断路器等
  - 反馈管道：将模型状态回写控制线圈
- **simLc\*** 等：仍走 `ModbusDataSync`（legacy 路径）

### PCS 爬坡

- 原独立 Thread 已移除；在 `PcsDevice.AdvancePowerRamps` 中按仿真步推进。

## 接口与配置

- 点位映射 CSV：`Address` / `FunctionCode` / `ParamName` / `Scale` / `ModelSim` / `Type`
- `appsettings.json`：`Simulator`、`Pcs`、`Pcc`、`Transformer`、`UnitTransformer`、`Load`、`DataExchange`
- 设备工厂：`PcsDeviceFactory`、`TransformerDeviceFactory` 将配置段映射为 `Model/*DeviceConfig`

## 测试与运行

```bash
dotnet build ./EssSimulator.csproj
dotnet test
```

单元测试覆盖：网络求解器、断路器控制桥、DataExchange 点目录与控制反馈管道等（`EssSimulator.Tests/`）。

## 路线图（已完成 / 进行中 / 待办）

### 已完成

- [x] 协议层封口：`simEmu`/`simBms`/`simEm` → `DataExchangeSession`
- [x] AC Solver 主路径：`ElectricalNetwork` + `NetworkSolver`
- [x] 控制面迁移：断路器、负载、BMS 链路 → `NetworkControlBridge`
- [x] PCS 迁移：`PcsDevice` 单一实例，删除 `PcsModel*`
- [x] 变压器迁移：`TransformerDevice` 单一实例，删除 `transformModel` / `TransformerSimulator`

### 已完成（阶段 5）

- [x] Legacy 收尾：删除双跑/shadow、废弃 Solver 开关、PCS 双写同步
- [x] `NetworkStepOrchestrator` 收敛为单一路径（网络控制面 + `ProjectBreakersToLegacy`）

### 已完成（阶段 6）

- [x] `BmsRackDevice`：`UpdatePhysics`、`SyncTelemetryAndProtection`、并网链路 SSOT
- [x] ESS / Solver / `BmsLinkEngine` / `BmsDataService` 改走 `_bmsRackDevices`

### 已完成（阶段 7）

- [x] `LoadDevice` 吸收时段计划、`SetLoadCharacteristic`、失电逻辑；删除 `ScheduledLoadSimulator`
- [x] ESS `_loadDevice` 与 `ElectricalNetwork.Load` 单实例；保留 `_loadSimulator` 属性供 GUI 路径
- [x] `EmMapper` 改读 `PccMeter.Telemetry`（P/Q/S/I/电能由 Solver S8 采样）
- [x] `NetworkControlBridge.SyncLoadPlan` 按仿真时间刷新计划

### 已完成（阶段 8）

- [x] `BlackStartInterlock`（设备层纯逻辑）+ `EnergyStorageSystem.TrySetPcsBlackStart`
- [x] `BlackStartSafety` 收敛为违规告警/退出；`PcsMapper` 经 ESS 写入黑启动
- [x] `UnitTransformerIslandSync` 抽取离网单元变同步
- [x] 主循环步序：`Solver` → `Update(PCS)` → `SyncUnitTransformer` → `RefreshBlackStartBus` → `SyncBmsLinks`

### 已完成（BMS 保护模块化）

- [x] `BmsRackProtection`：簇级三级告警状态机（`UpdateUnder`/`UpdateOver`/`EvaluateAllClusters`）
- [x] `BmsMapper.MapClusters` 仅保留遥测映射，保护评估委托设备层
- [x] `BmsDataServer` 兼容 API 改指向 `BmsRackProtection`

### 已完成（PCS 黑启动 A+B）

- [x] `BlackStartPhase` 四段：Preparing → SoftStarting → VoltageRegulating → Synchronized
- [x] 闭环建压（母线反馈 + 软启动 V/s + 频率 47→50Hz）
- [x] 建压期电流限幅与变压器涌流 P/Q 尖峰联动

### 待办

## 风险与缓解

- **离网/黑启动时序**：单元变在 PCS.Update 后同 tick 同步（`UnitTransformerIslandSync`），联调时关注建压爬升是否平滑。
- **映射偏差**：CSV `ModelSim` 路径与模型字段不一致会导致 DataExchange 读写失败；启动期日志需关注。
- **并发**：DataExchange 多线程读写模型，共享状态变更处应保持既有锁/原子约定。
