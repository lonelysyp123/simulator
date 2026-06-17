# 仿真模拟器 — 架构说明

## 分层

| 层 | 目录 | 职责 |
|----|------|------|
| 协议层 | `Protocol/` | Modbus TCP 从站、`ModbusHostedService` 启动多路服务 |
| 数据交换 | `DataExchange/` | 点目录编译、遥测/控制/反馈管道、`ControlEffect` |
| 映射层 | `EssSimModelApi/Mappers/` | 物理状态 → BMS/EMU/EM DTO（`BmsMapper`、`PcsMapper`） |
| 编排层 | `EssDeviceSimModel/EnergyStorageSys.cs` | 仿真主循环、BMS/PCS 耦合、黑启动站用电 |
| 求解层 | `EssDeviceSimModel/Solver/`、`Propagation/` | `ElectricalNetwork`、`NetworkSolver`、径向潮流 |
| 设备层 | `EssDeviceSimModel/Devices/` | PCS、变压器、断路器、电网、负载、BMS Rack |
| 电池细模型 | `EssDeviceSimModel/Battery/` | 电芯/簇/堆积分，经 `BmsRackDevice` 接入 |

## 运行模型（相对老版本）

- **老方式**：多线程各自轮询（PCS 爬坡、Modbus 同步等分散）。
- **现方式**：
  - `.NET Host` 注册多个 `IHostedService` / `BackgroundService`
  - **单一仿真时钟**：`EnergyStorageSystem.ExecuteAsync` + `PeriodicTimer`（`PropagationIntervalMs`）
  - 每 tick：电气网络 Step → PCS/BMS 物理更新 → Mapper 刷新 DTO
  - Modbus 侧独立周期读 DTO 写寄存器（`DataExchange` 的 `TelemetryIntervalMs` 等）

## 主循环一步（简化）

1. `RadialPowerSweepEngine.SolveCycle` 或 `NetworkStepOrchestrator.SolverPrimaryStep`
2. `Update`：PCS DC、BMS 电流积分
3. `SyncUnitTransformerAfterPcsUpdate`：离网/黑启动单元变与站用电
4. `RefreshAllUnitBlackStartBusContexts`
5. `NetworkControlBridge.SyncBmsLinksFromRacks`

## 数据交换三管道

| 管道 | 方向 | 说明 |
|------|------|------|
| Telemetry | 仿真 → Modbus | 如 `bms1.BatteryStacks[0].SOC` → `yc11` |
| Control | Modbus → 仿真 | EMS 写寄存器 → `ControlEffect`（启停、并网、断路器） |
| Feedback | 仿真 → Modbus | Hold 类控制点状态回写 |

绑定路径：`CSV ModelSim` → `ModbusPointMap` 替换 `bmsdeviceId`/`emuDeviceId` → `PointCatalogLoader` → `ObjectPathResolver` / `SimServer.GetExtIfVariableVal`。

部分 DTO 字段为**计算属性**（如 `ChargeDischargeStatus`）或由 **Mapper 从物理量写入**（如 `SOC` ← `RackState.MinClusterSOC`）。

## 电气拓扑（配置驱动）

电压等级示例（见 `Pcc`、`StationBus` 配置）：

- PCC：如 220kV 线电压
- 站用/集电：如 35kV
- 单元 AC 母线：690V 级

断路器、主变、单元变、负载、PCS、BMS DC 端口在 `ElectricalTopologyFactory` / `NetworkTopologyBuilder` 中连成一张网；**设备实例唯一**，ESS 与 Network 共用同一 `PcsDevice` 等对象。

## 设计原则

- **配置即拓扑**：单元数、变压器参数、负载计划 → `appsettings.json`
- **点表即协议**：外部可见寄存器仅由 CSV 定义
- **控制面 SSOT**：断路器、BMS 链路等经 `NetworkControlBridge`

## 仿真边界（回答用户时宜说明）

- 面向 **EMS 联调与逻辑验证**，非电芯级标定或保护证书级仿真
- 电池为 aggregated 模型；保护/告警有简化规则
- Modbus 为 **TCP 仿真从站**，非现场真实 BMS/PCS 协议栈复刻
