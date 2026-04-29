# appsettings.json 字段说明（EssSimulator）

本文档用于解释 `appsettings.json` 中每个字段的含义与影响范围。`appsettings.json` 本身保持为**纯 JSON**（不包含注释），便于与第三方工具/格式化器兼容。

## 总体单位与约定

- **功率**：kW（有功）、kvar（无功）
- **电压**：V（一次侧/二次侧均使用 V；界面可能会换算成 kV 显示）
- **电流**：A
- **频率**：Hz
- **SOC**：0~1（配置里）、界面/对外展示可能换算为 %

## 顶层结构

`appsettings.json` 的顶层包含以下配置段：

- `Simulator`：仿真运行参数、协议端口、设备清单（决定通道数）
- `Pcs`：PCS 物理侧参数（会影响仿真行为与并网侧计算）
- `Transformer`：变压器模型参数（影响一次/二次电压、电流、损耗等）
- `UnitTransformer`：单元变参数（每个 Unit 一台 35kV/690V）
- `Load`：站内负载计划（决定系统功率平衡与潮流方向）

## 当前配置快照（按当前 appsettings.json）

- `Simulator.Runtime`
  - `SimStepMs = 200`
  - `Speedup = 1.0`
  - `NoGui = false`
- `Simulator.Protocol`
  - `BaseBmsModbusPort = 1501`
  - `BmsPortStep = 1`
  - `BaseEmuModbusPort = 1601`
  - `EmuPortStep = 1`
  - `EmModbusPort = 1500`
- `Simulator.Devices`
  - 共 `5` 个 Unit（每个 Unit 固定 2 路 PCS + 2 路 BMS）
  - 所有 BMS 拓扑一致：`ClusterCount=12`、`PackCount=4`、`CellSeriesCount=104`、`CellParallelCount=1`
  - 所有 BMS 单体参数一致：`CellNominalVoltage=3.2V`、`CellNominalCapacity=314Ah`
  - 初始 SOC 配置：
    - Unit-1: `0.55/0.50`（随机扰动 `0.03`）
    - Unit-2~5: `0.60/0.45`（随机扰动 `0.02`）
- `Pcs`
  - `RatedPower=2508`、`MaxPower=2508`、`Efficiency=0.99`
  - `DcVoltageRangeMin=1000`、`DcVoltageRangeMax=1500`
  - `AcVoltageNominal=690`、`FrequencyNominal=50`
  - `MaxCurrent=2200`、`GridLossCoefficient=0.11`
- `Transformer`
  - `RatedPower=2500`、`PrimaryVoltage=220000`、`SecondaryVoltage=35000`
  - `NoLoadLoss=50`、`LoadLoss=200`、`ImpedancePercent=4`
  - `ReactiveVoltageInfluenceCoefficient=1.0`、`NoLoadCurrentPercent=2`
- `UnitTransformer`
  - `RatedPower=2500`、`PrimaryVoltage=35000`、`SecondaryVoltage=690`
  - `NoLoadLoss=50`、`LoadLoss=200`、`ImpedancePercent=4`
  - `ReactiveVoltageInfluenceCoefficient=1.0`、`NoLoadCurrentPercent=2`
- `Load`
  - `ActivePowerPlan=-500`
  - `ReactivePowerPlan=0`

---

## Simulator

### Simulator.Runtime

- `Simulator.Runtime.SimStepMs`（int，ms）
  - **作用**：仿真主循环的真实休眠间隔。
  - **影响**：刷新/更新频率与 CPU 占用。越小刷新越快、占用越高。

- `Simulator.Runtime.Speedup`（double，无量纲）
  - **作用**：仿真加速倍率。
  - **影响**：仿真内部使用的时间步长 \(dt\) 约为 `SimStepMs * Speedup`。增大可在相同真实时间里推进更多仿真时间。

- `Simulator.Runtime.NoGui`（bool）
  - **作用**：是否禁用控制台可视化界面。
  - **影响**：`true` 时不启动 GUI，但后台服务（仿真、点表/协议服务等）仍正常运行。

### Simulator.Protocol

- `Simulator.Protocol.BaseBmsModbusPort`（int）
  - **作用**：BMS Modbus TCP 起始端口。
  - **影响**：多路 BMS 从站端口会从该端口起按步长递增。

- `Simulator.Protocol.BmsPortStep`（int）
  - **作用**：BMS 端口步长。

- `Simulator.Protocol.BaseEmuModbusPort`（int）
  - **作用**：EMU Modbus TCP 起始端口（每个 Unit 一个 EMU 从站）。

- `Simulator.Protocol.EmuPortStep`（int）
  - **作用**：EMU 端口步长。

- `Simulator.Protocol.EmModbusPort`（int）
  - **作用**：并网电表（`simEm`）Modbus TCP 端口。

### Simulator.Devices（数组）

该数组用于定义“单元（Unit）”数量与每个单元内部的 PCS/BMS 配置。当前系统约定：

- **每个 Unit 固定 2 路 PCS + 2 路 BMS**
- Unit 数量会决定 EMU 从站数量与通道数（PCS/BMS 通道随之扩展）

#### Simulator.Devices[i].Name

- **作用**：单元名称（当前版本仅配置占位，不参与仿真计算与点位映射）。

#### Simulator.Devices[i].Pcs（数组，固定 2 项）

每个 PCS 子项支持：

- `Name`：PCS 名称（当前版本仅配置占位，不参与仿真计算与点位映射）
- `PcsRamp`：功率爬坡参数（见下）

##### Simulator.Devices[i].Pcs[j].PcsRamp

用于覆盖该 PCS 的功率爬坡行为：

- `Slope`（double）
  - **作用**：爬坡斜率（越大变化越快）。
  - **影响**：影响功率设定值逼近目标值的速度。

- `IntervalMs`（int，ms）
  - **作用**：爬坡线程每级更新间隔。

- `DelayMs`（int，ms）
  - **作用**：新设定值生效前的初始延时。

#### Simulator.Devices[i].Bms（数组，固定 2 项）

每个 BMS 子项主要决定电池拓扑、容量与初始状态：

- `Name`：BMS 名称（当前版本仅配置占位，不参与仿真计算与点位映射）
- `ClusterCount`：簇数量
- `PackCount`：每簇 Pack 数
- `CellSeriesCount`：单体串联数
- `CellParallelCount`：单体并联数
- `CellNominalVoltage`：单体标称电压（V）
- `CellNominalCapacity`：单体标称容量（Ah）
- `CellInitialSoc`：初始 SOC（0~1，会传递到电芯初始化）
- `CellInitialSocRandomRange`：初始 SOC 随机扰动范围（0~1，会传递到电芯初始化）
- `PackInternalResistance`：Pack 等效内阻（简化模型用）
- `ClusterInternalResistance`：Cluster 等效内阻
- `RackInternalResistance`：Rack 等效内阻

---

## Pcs（PCS 物理参数，直接影响仿真行为）

该段绑定到 `PcsPhysicalConfig`，并用于初始化 `PCSSimulator` 的核心配置与并网侧折算。

- `Pcs.RatedPower`（double）
  - **作用**：额定容量标尺（在代码中用于视在功率检查阈值）。

- `Pcs.MaxPower`（double，kW）
  - **作用**：有功/无功指令限幅上限。
  - **影响**：任何下发的 P/Q 会被裁剪到 \([-MaxPower, +MaxPower]\)。

- `Pcs.Efficiency`（double，0~1）
  - **作用**：PCS 交流侧与直流侧功率换算效率。
  - **影响**：影响电池侧功率/电流与损耗。

- `Pcs.DcVoltageRangeMin` / `Pcs.DcVoltageRangeMax`（double，V）
  - **作用**：直流侧电压允许范围。
  - **影响**：越限会触发 PCS 的越限/故障路径（具体动作取决于 PCS 模型实现）。

- `Pcs.AcVoltageNominal`（double，V）
  - **作用**：交流额定线电压基准。

- `Pcs.FrequencyNominal`（double，Hz）
  - **作用**：交流额定频率基准。

- `Pcs.MaxCurrent`（double，A）
  - **作用**：最大交流电流门槛。
  - **影响**：过流会触发 PCS 的保护/故障路径。

- `Pcs.GridLossCoefficient`（double，0~1）
  - **作用**：并网侧线损/压降简化系数。
  - **影响**：
    - 网侧功率折算（放电折减、充电反推）
    - 电压折算（PCS 内部对“电网电压”的等效处理）

---

## Transformer（主变 220kV/35kV）

该段绑定到 `TransformerConfig`，用于初始化主变 `TransformerSimulator`（220kV/35kV），其二次侧电压作为 **35kV 母线（PCC）** 测点，供负载与并网反馈使用。

- `Transformer.RatedPower`（double，kVA）
  - **作用**：变压器额定容量，用于负载率计算与无功电压反馈等。

- `Transformer.PrimaryVoltage`（double，V）
  - **作用**：主变一次侧额定线电压（例如 220kV 写 220000）。

- `Transformer.SecondaryVoltage`（double，V）
  - **作用**：主变二次侧额定线电压（例如 35kV 写 35000）。

- `Transformer.NoLoadLoss`（double，W）
  - **作用**：空载损耗（铁损）基准。

- `Transformer.LoadLoss`（double，W）
  - **作用**：负载损耗（铜损）基准。

- `Transformer.ImpedancePercent`（double，%）
  - **作用**：短路阻抗百分比，用于二次侧电压随负载/无功变化的近似计算。

- `Transformer.ReactiveVoltageInfluenceCoefficient`（double）
  - **作用**：无功对 PCC 电压影响系数（简化模型的调节因子）。

- `Transformer.NoLoadCurrentPercent`（double，%）
  - **作用**：空载电流百分比，用于一次侧电流估算。

---

## UnitTransformer（单元变 35kV/690V）

该段绑定到 `UnitTransformerConfig`，用于为每个储能单元创建一台单元变 `TransformerSimulator`（35kV/690V）。

- `UnitTransformer.RatedPower`（double，kVA）
  - **作用**：单元变额定容量，用于负载率与电压反馈等计算。

- `UnitTransformer.PrimaryVoltage`（double，V）
  - **作用**：单元变一次侧额定线电压（通常为 35kV → 35000）。

- `UnitTransformer.SecondaryVoltage`（double，V）
  - **作用**：单元变二次侧额定线电压（通常为 690V → 690）。

- `UnitTransformer.NoLoadLoss` / `UnitTransformer.LoadLoss`（double，W）
  - **作用**：单元变损耗参数（铁损/铜损），影响效率与电流估算。

- `UnitTransformer.ImpedancePercent`（double，%）
  - **作用**：阻抗百分比，影响二次侧电压随无功/负载的变化幅度。

- `UnitTransformer.ReactiveVoltageInfluenceCoefficient`（double）
  - **作用**：无功对电压影响的调节系数。

- `UnitTransformer.NoLoadCurrentPercent`（double，%）
  - **作用**：空载电流百分比。

---

## Load（负载计划）

该段绑定到 `LoadConfig`，并用于负载仿真器生成计划功率。

- `Load.ActivePowerPlan`（double，kW）
  - **作用**：负载有功计划。
  - **约定**：负值表示“用电/从电网取电”，正值表示“发电/向外送电”。

- `Load.ReactivePowerPlan`（double，kvar）
  - **作用**：负载无功计划。

---