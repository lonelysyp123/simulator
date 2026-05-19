# 储能仿真系统操作手册

## 1. 文档目的

本文档用于说明 `EssSimulator` 项目的构建、启动、配置、联调和常用操作方式，面向开发、测试与系统联调人员。

本项目是一个基于 `.NET 8` 的储能系统仿真程序，核心能力包括：

- 仿真多套电池舱、多台 PCS、主变/单元变、断路器和负载（数量由 `Simulator.Devices` 推导）。
- 通过 Modbus TCP 对外提供 BMS、PCS/EMU、电表数据。
- 支持 EMS 主控有功/无功、PCS 启停、单元高压断路器、**孤岛电压百分比**、**黑启动**等控制点。
- 离网 V/f 建压、黑启动有功内环、变压器励磁涌流等物理行为仿真。
- 提供控制台 GUI 和命令行交互命令，便于调试和联调。
- 使用 CSV 点表将外部点位映射到内部对象路径。

---

## 2. 项目概览

### 2.1 启动入口

程序入口位于 `Program.cs`，启动后会完成以下工作：

1. 加载 `log4net.config` 日志配置。
2. 从 `appsettings.json` 读取仿真配置。
3. 初始化核心模型 `EnergyStorageSystem`。
4. 启动以下后台服务：
	- `BmsDataService`
	- `PcsDataServer`
	- `EmDataService`
	- `ModbusHostedService`
5. 注册黑启动安全联锁 `BlackStartSafety`（主断与单元高压均合闸时禁止黑启动）。
6. 默认启动控制台 GUI（当 `Simulator.Runtime.NoGui=false` 时）。

### 2.2 核心模块

| 模块 | 路径 | 说明 |
|---|---|---|
| 启动入口 | `Program.cs` | 程序启动、依赖注入、托管服务注册 |
| 配置模型 | `Configuration/SimulatorConfig.cs` | 定义仿真参数、PCS、变压器、负载配置 |
| 系统模型 | `EssDeviceSimModel/` | 电池、PCS、变压器、断路器、负载等仿真逻辑 |
| 数据服务 | `EssSimModelApi/` | BMS、PCS、EM 数据同步与映射；`BlackStartSafety` 联锁 |
| 协议服务 | `Protocol/` | Modbus 服务、对象路径访问、`ModbusDataSync` 控制点同步 |
| 交互界面 | `Display/` | GUI 菜单、命令输入、日志视图 |
| 点表文件 | `bms_bank.csv`、`bms_rack.csv`、`emu.csv`、`em.csv` | 定义 Modbus 点位与模型映射 |

---

## 3. 运行环境

### 3.1 环境要求

- `dotnet SDK 8.0` 或更高兼容版本
- macOS / Linux / Windows 均可运行
- 终端建议支持 UTF-8

### 3.2 依赖包

项目依赖在 `EssSimulator.csproj` 中定义，主要包括：

- `Microsoft.Extensions.Hosting`
- `log4net`
- `NModbus`
- `CsvHelper`
- `Spectre.Console`

---

## 4. 构建与启动

### 4.1 为什么不能直接执行 `dotnet build`

项目根目录下同时存在多个解决方案/项目文件，例如：

- `EssSimulator.sln`
- `IEC61850-simulatorServer2.sln`
- `EssSimulator.csproj`

因此直接执行：

```zsh
dotnet build
```

会触发 `MSB1011`，因为 .NET CLI 无法自动判断要构建哪个目标。

### 4.2 正确的构建方式

请显式指定解决方案或项目文件。

推荐使用：

```zsh
cd /Users/songyinpei/jobs/trina/EssSimulator
dotnet build ./EssSimulator.csproj
```

也可以构建解决方案：

```zsh
cd /Users/songyinpei/jobs/trina/EssSimulator
dotnet build ./EssSimulator.sln
```

### 4.3 启动方式

推荐直接运行项目：

```zsh
cd /Users/songyinpei/jobs/trina/EssSimulator
dotnet run --project ./EssSimulator.csproj
```

也可以运行编译产物：

```zsh
cd /Users/songyinpei/jobs/trina/EssSimulator/bin/Debug/net8.0
dotnet EssSimulator.dll
```

### 4.4 GUI 启动说明

程序默认会启动控制台 GUI。

如果希望使用无界面模式，请修改 `appsettings.json`：

```json
"Simulator": {
  "Runtime": {
    "NoGui": true
  }
}
```

当前代码中没有单独提供命令行参数切换 GUI，因此是否启用 GUI 由配置文件控制。

---

## 5. 配置说明

主配置文件为 `appsettings.json`。

### 5.1 `Simulator` 配置项（按当前结构）

| 配置项 | 当前 `appsettings.json` 示例 | 说明 |
|---|---:|---|
| `Simulator.Runtime.SimStepMs` | `200` | 仿真主循环步长（ms） |
| `Simulator.Runtime.Speedup` | `1.0` | 仿真加速倍率（影响 PCS 爬坡线程真实等待时间） |
| `Simulator.Runtime.NoGui` | `false` | 是否禁用 GUI（无头模式） |
| `Simulator.Runtime.PcsRamp` | 类默认 `Slope=1` 等 | 全局 PCS 爬坡默认；可被 `Devices[].Pcs[].PcsRamp` 覆盖 |
| `Simulator.Protocol.BaseBmsModbusPort` | `1501` | BMS Modbus 基础端口 |
| `Simulator.Protocol.BmsPortStep` | `1` | BMS 端口步长 |
| `Simulator.Protocol.BaseEmuModbusPort` | `1601` | EMU Modbus 基础端口（每储能单元一个） |
| `Simulator.Protocol.EmuPortStep` | `1` | EMU 端口步长 |
| `Simulator.Protocol.EmModbusPort` | `1500` | 电表 Modbus 端口 |
| `Simulator.Devices[*].Pcs[*].Name` | `PCS` | PCS 名称 |
| `Simulator.Devices[*].Bms[*].ClusterCount` | `12` | 簇数量 |
| `Simulator.Devices[*].Bms[*].PackCount` | `4` | 每簇 Pack 数 |
| `Simulator.Devices[*].Bms[*].CellSeriesCount` | `104` | 单包串联单体数 |
| `Simulator.Devices[*].Bms[*].CellNominalVoltage` | `3.2` | 电芯额定电压 |
| `Simulator.Devices[*].Bms[*].CellNominalCapacity` | `314` | 电芯额定容量 |
| `Simulator.Devices[*].Bms[*].CellInitialSoc` | `0.5` | 电芯初始 SOC 基准值（0-1） |
| `Simulator.Devices[*].Bms[*].CellInitialSocRandomRange` | `0.05` | 初始 SOC 随机扰动范围（±） |

### 5.2 其他配置节

#### `Pcs`（`PcsPhysicalConfig`）

| 配置项 | 说明 |
|---|---|
| `RatedPower` / `MaxPower` | 额定/最大功率（kW） |
| `AcVoltageNominal` / `FrequencyNominal` | 交流额定电压（V）、频率（Hz） |
| `GridLossCoefficient` | 并网线损折算系数 |
| `IslandVfSlewRatePercentPerSecond` | 孤岛电压**有效值**向设定值爬坡速率（%/s） |
| `IslandVoltageStepFaultThresholdPercent` | 单次设定阶跃过大故障阈值（百分点） |
| `IslandVoltageGridConflictThresholdPercent` | 并网时 VF 百分比过高冲突阈值 |
| `BlackStartActivePowerGainKwPerPercent` | 黑启动：目标与有效电压差 1% 对应有功增量（kW） |
| `BlackStartMaxActivePowerKw` | 黑启动自动有功上限（kW） |
| `BlackStartMagnetizingPowerFraction` | 黑启动励磁附加有功（占额定功率比例） |

#### `Transformer` / `UnitTransformer`

主变（220kV/35kV）与单元变（35kV/690V）参数，除额定容量、损耗、阻抗外，还支持：

| 配置项 | 说明 |
|---|---|
| `MagnetizingInrushEnabled` | 是否启用励磁涌流 |
| `MagnetizingInrushDvDtThresholdPuPerSec` | 电压变化率阈值（pu/s） |
| `MagnetizingInrushPeakExtraMultipleOfRatedPrimary` | 涌流峰值相对额定一次电流倍数 |
| `MagnetizingInrushDecayTimeConstantSec` | 涌流衰减时间常数（s） |
| `MagnetizingInrushMaxExtraMultipleOfRatedPrimary` | 涌流峰值上限倍数 |

当前示例：主变 `RatedPower=31500`，单元变 `RatedPower=6300`（见 `appsettings.json`）。

#### `Load`

- `ActivePowerPlan` / `ReactivePowerPlan`：计划负荷；`esscmd setLoad` 会覆盖自动计划。

---

## 6. 对外服务与端口

程序启动后会自动启动多个 Modbus TCP 服务。

### 6.1 端口生成规则（以配置为准）

| 服务名 | 设备名 | 端口公式（当前配置） | 点表 |
|---|---|---|---|
| 电表 | `simEm` | `1500` | `em.csv` |
| PCS/EMU 单元 u | `simEmu{u}` | `1601 + (u-1)` | `emu.csv` |
| BMS 单元 i | `simBms{i}` | `1501 + (i-1)` | `bms_bank.csv` + `bms_rack.csv` |

说明：端口由 `Simulator.Protocol.*` 决定；上表对应当前 `appsettings.json`。BMS rack 从站 ID 为 `主站 slaveId + rack 序号`（见 `ModbusTCPSlave`）。

**`dpc` 设备名**须与注册名一致，例如 `simEmu1`（不是 `simEmu`）。

**注意**：当实际 PCS/EMU 通道数超过 `emu.csv` 当前映射范围时，需为 `emu.PcsList[n]` 补充对应点位，否则多出的 PCS 数据不会通过 Modbus 暴露。

### 6.2 监听信息查看

程序运行后，可在 GUI 的“连接信息”页面查看服务监听情况。

---

## 7. 点位映射机制

### 7.1 点表文件

项目使用以下 CSV 文件定义 Modbus 点位：

- `bms_bank.csv`
- `bms_rack.csv`
- `emu.csv`
- `em.csv`

### 7.2 CSV 关键字段

| 字段 | 说明 |
|---|---|
| `FunctionCode` | 点表功能码，决定仿真器数据区与 Modbus 读写方式（见下表） |
| `Address` | PDU 地址（配合 mbpoll `-0` 使用） |
| `Type` | 数据类型，如 `u16`、`int16`、`int32` |
| `Size` | 位宽 |
| `ParamName` | 点位名称，供 `dpc` 命令使用 |
| `Scale` | 缩放倍数 |
| `Description` | 点位说明 |
| `ModelSim` | 内部对象路径映射配置 |

### 7.3 点表 `FunctionCode` 与数据区

点表中的 `FunctionCode` 与标准 Modbus 功能码一致，仿真器内部映射如下：

| FunctionCode | 含义 | 仿真器存储区 | 典型用途 |
|---:|---|---|---|
| `1` | 线圈读/写 | `CoilDiscretes` | 少见 |
| `2` | 离散输入 | — | 只读（当前较少使用） |
| `3` | 保持寄存器 | `HoldingRegisters` | 读/写寄存器 |
| `4` | 输入寄存器 | `InputRegisters` | 遥测只读 |
| `5` | 写单线圈 | `CoilDiscretes` | 启停、黑启动、断路器等 `bool` 控制 |
| `6` | 写单寄存器 | `HoldingRegisters` | 有功/无功设定、孤岛电压百分比等 |

控制点（FC `5`/`6`）由 `ModbusDataSync` 控制线程轮询并调用 `PcsMapper.ApplyEmuCommands`；数据点（FC `4`）由模型周期刷新。

### 7.4 EMU 主要控制点（`emu.csv`，Unit-1 / `simEmu1`）

| ParamName | FC | 地址 | Scale | 说明 |
|---|---|---:|---:|---|
| `highvoltagebreakeronoff` | 5 | 4 | 1 | 单元高压断路器（`emu.Emu.PowerOnOff`，0 分 / 1 合） |
| `param55` | 6 | 5300 | 10 | PCS1 有功设定（kW），寄存器值 = kW×10 |
| `param56` | 6 | 5301 | 10 | PCS1 无功设定（kvar） |
| `param64` | 6 | 5304 | 1 | PCS1 孤岛电压百分比设定（0–100） |
| `pcs1_blackstart_enable` | 5 | 5305 | 1 | PCS1 黑启动（0/1） |
| `pcs1_startstop` | 5 | 5303 | 1 | PCS1 启停（0/1） |
| `param62` | 4 | 25336 | 1 | PCS1 孤岛电压百分比反馈（有效值） |
| `param59`~`pcs2_*` | — | 5600 段 | — | PCS2 对应点位（地址 +300） |

Unit-N 使用 `simEmu{N}`，端口 `1601 + N - 1`。

### 7.5 对象路径

CSV 中 `ModelSim` 的 `arg1` 会映射到程序内部对象路径，例如：

- `em.PhaseACurrent`
- `ess._breaker.IsClosed`
- `bms1.BatteryStacks[0].SOC`

运行期通过以下接口读写：

- `SimServer.GetExtIfVariableVal(...)`
- `SimServer.SetExtIfVariableVal(...)`

---

## 8. GUI 使用说明

默认 GUI 菜单位于 `Display/GuiMain.cs`，主要包含以下页面：

| 菜单项 | 说明 |
|---|---|
| 主电气接线 | 显示断路器、变压器、PCS、电池舱、负载、电表数据 |
| 电池堆簇信息 | 查看电池堆及簇级状态 |
| 电池单体信息 | 查看单体电压等详细信息 |
| 命令输入 | 输入命令调试设备（`esscmd`/`breaker`/`dpc`/`dpctest`/`help`） |
| 连接信息 | 查看服务监听与客户端连接状态 |
| 日志信息 | 查看日志输出 |

GUI 快捷键：

- `↑` / `↓`：上下选择菜单
- `Enter`：进入页面
- `Esc`：退出程序

主电气接线页面内快捷键（新增）：

- `Tab`：在实时表格视图与 ASCII 视图之间切换
- `:` 或 `C`：直接打开“内联命令输入”，执行后自动返回主接线页面
- `Esc`：返回上一级菜单

---

## 9. 控制台命令说明（当前 GUI 接入）

命令定义在 `Display/Cmd.cs`。

### 9.1 `dpc`

用于按点位名称直接读取或写入数据。

#### 语法

```text
dpc <device>.<datapoint> get
dpc <device>.<datapoint> set <value>
```

#### 说明

- `<device>` 必须是已注册设备名，如 `simBms1`、`simEmu1`、`simEm`。
- `<datapoint>` 必须来自对应 CSV 文件中的 `ParamName`。
- 如果该点位由模型周期刷新，则 `set` 的值可能会在下一次轮询时被覆盖。

#### 示例

```text
dpc simBms1.yc11 get
dpc simBms1.yc10 set 125.5
dpc simEm.yc12 get
```

#### 常见错误

- 设备名错误：会提示“找不到对应的设备模型”。
- 点位名错误：会提示“指定设备找不到对应数据点”。

### 9.2 `esscmd`

用于调节负载参数，或控制 PCS/BMS Modbus 协议对外是否可用。

#### 语法

```text
esscmd setLoad activePower <value>
esscmd setLoad reactivePower <value>

esscmd link pcsN on|off
esscmd link bmsN on|off
esscmd link status [pcsN|bmsN]
```

#### 参数说明

| 参数 | 说明 |
|---|---|
| `setLoad activePower` | 负载有功功率，单位 kW |
| `setLoad reactivePower` | 负载无功功率，单位 kvar |
| `link pcsN on/off` | 开启/关闭第 N 路 PCS 所属 EMU 单元的 Modbus TCP 服务 |
| `link bmsN on/off` | 开启/关闭第 N 路 BMS 的 Modbus TCP 服务 |
| `link status` | 查看全部或指定设备的协议链路状态 |

`on/off` 也支持别名：`online/offline`、`connect/disconnect`。

#### 示例

```text
esscmd setLoad activePower 600
esscmd setLoad reactivePower 80

esscmd link pcs1 off
esscmd link bms2 off
esscmd link status
esscmd link pcs3 on
```

#### 注意事项

- `setLoad` 一旦执行，会停止按时段自动切换负载，改为使用手动设定值。
- PCS 功率请通过 Modbus 写 EMU 寄存器（如 `param55`/`param59`）或 `dpc` 控制，不再通过 `esscmd` 设置。
- `link off` 会关闭 TCP 监听并停止寄存器同步，外部主站/mbpoll 无法连接，用于模拟通信中断。
- `link on` 会重新绑定端口并恢复数据同步。
- 同一储能单元内 `pcs(2n-1)` 与 `pcs(2n)` 共用 `simEmu{n}`，`link pcs1 off` 与 `link pcs2 off` 效果相同，均会断开该单元 EMU 端口。
- BMS 为每路独立端口：`bmsN` 对应 `simBmsN`。

### 9.3 `breaker`

直接设置主断路器状态。

#### 语法

```text
breaker set true
breaker set false
```

#### 含义

- `true`：合闸
- `false`：分闸

### 9.4 `dpctest`

用于执行 `autotest.json` 中定义的自动化脚本（按顺序执行 `dpc` 和 `sleep` 步骤）。

#### 语法

```text
dpctest list
dpctest <testName>
```

#### `autotest.json` 结构

```json
{
  "tests": [
    {
      "name": "xxtest",
      "description": "按阶梯下调 simEm.yc19",
      "steps": [
        "dpc simEm.yc19 set 49.95",
        "sleep(10)",
        "dpc simEm.yc19 set 49.9"
      ]
    }
  ]
}
```

也支持使用 `script` 字段按分号分隔步骤，例如：  
`dpc simEm.yc19 set 49.95;sleep(10);dpc simEm.yc19 set 49.9;`

#### 步骤语法

- `dpc <device>.<datapoint> set <value>`
- `dpc <device>.<datapoint> get`
- `sleep(10)` 或 `sleep 10`

### 9.5 退出命令输入

在“命令输入”页面中输入：

```text
exit
```

可返回 GUI 菜单（不会退出整个进程）。

---

## 10. 典型操作场景

### 10.1 启动并观察系统状态

1. 构建项目。
2. 运行 `EssSimulator`。
3. 进入“主电气接线”页面，观察以下关键量：
	- 断路器状态
	- 变压器一次/二次侧电压与电流
	- PCS1/PCS2 有功、无功
	- 电池舱 SOC、直流电压、直流电流
	- 负载有功、无功
	- 电表三相电流、线电压

说明：当前系统中负载与 PCS 交流电流均按三相线电压口径计算  
`I = S / (sqrt(3) * Uline)`，用于保证功率对消场景下电流口径一致。

### 10.2 读取 BMS 的 SOC

```text
dpc simBms1.yc11 get
```

根据 `bms_bank.csv`，`yc11` 表示系统 SOC。

### 10.3 手动修改负载功率

```text
esscmd setLoad activePower 500
esscmd setLoad reactivePower 50
```

执行后，负载将不再按日程窗口自动变化，而是保持手动值。

### 10.4 修改 PCS 响应特性

```text
全局默认:
appsettings.json -> Simulator.Runtime.PcsRamp.Slope
appsettings.json -> Simulator.Runtime.PcsRamp.IntervalMs
appsettings.json -> Simulator.Runtime.PcsRamp.DelayMs

单个 PCS 覆盖:
appsettings.json -> Simulator.Devices[0].Pcs[0].PcsRamp.Slope
appsettings.json -> Simulator.Devices[0].Pcs[0].PcsRamp.IntervalMs
appsettings.json -> Simulator.Devices[0].Pcs[0].PcsRamp.DelayMs
```

若单个 PCS 未配置 `PcsRamp`，将回退到 `Simulator.Runtime.PcsRamp`。修改后重启程序生效。

### 10.5 手动控制断路器

```text
breaker set true
breaker set false
```

### 10.6 EMS 主控有功/无功（并网）

本项目按“EMS 主控”建模：

- EMS/调度通过保持寄存器下发有功、无功设定（如 `param55`/`param56`，地址 `5300`/`5301`）。
- PCS 执行设定值及爬坡；**黑启动激活时忽略** EMS 有功/无功设定。
- PCS 不在本地自动改写无功目标（无 Volt-Var 本地策略）。

### 10.7 离网建压（孤岛电压百分比）

- 设定：`param64` / `param65`（FC6）写入 `IslandVoltagePercentSetting`（0–100）。
- 反馈：`param62` / `param63`（FC4）为内部爬坡后的**有效值** `IslandVoltagePercentEffective`。
- 网侧不可用时，若启停为 1 且设定 > 0（或黑启动开启），PCS 进入 `Normal` + 离网 `Islanded`，按 V/f 建压。
- 有效值向设定值爬坡，速率由 `IslandVfSlewRatePercentPerSecond` 控制。

### 10.8 黑启动

**行为概要：**

- 点位：`pcs1_blackstart_enable` / `pcs2_blackstart_enable`（FC5 线圈）。
- 开启后 PCS 按孤岛电压百分比内环自动调节有功，无功为 0；EMS 有功/无功设定无效。
- 相关参数见 `Pcs` 配置节中 `BlackStart*` 三项。

**推荐操作顺序（mbpoll，Unit-1，端口 1601）：**

1. 单元高压分闸（可选）：`highvoltagebreakeronoff` = 0  
2. 黑启动 = 1（地址 5305）  
3. 孤岛电压百分比设定（地址 5304，如 100）  
4. PCS 启停 = 1（地址 5303）

**联锁（`BlackStartSafety`）：**

- 当**主断路器与所属单元高压断路器均为合闸**时开启黑启动，视为向电网侧建压短路风险，程序记录 Fatal 日志并在约 5 秒后退出。
- 分闸状态下允许黑启动离网建压（`PcsMapper` + `EnergyStorageSys` 已按此逻辑处理）。

### 10.9 使用 mbpoll 联调 EMU

须使用 **PDU 地址模式**（`-0`），并根据点表 `FunctionCode` 选择 `mbpoll -t`：

| 点表 FC | mbpoll 读/写 `-t` |
|---:|---|
| 5 | `-t 0`（线圈） |
| 4 | `-t 3`（输入寄存器；32 位用 `-t 3:int -c 2`） |
| 6 | `-t 4`（保持寄存器） |

**示例（PCS1 有功 100 kW，Scale=10）：**

```bash
mbpoll -0 -t 4 -a 1 -r 5300 -p 1601 -1 127.0.0.1 1000
```

**示例（PCS1 启停合闸）：**

```bash
mbpoll -0 -t 0 -a 1 -r 5303 -p 1601 -1 127.0.0.1 1
```

项目内可参考 `.cursor/skills/ess-mbpoll/SKILL.md` 自动生成命令（按点表查 FC 与 Scale）。

写入 `pcs1_startstop` / `pcs2_startstop` 后，控制线程会**立即**调用 `ApplyEmuCommands`（无需等待 100ms `PcsDataServer` 周期）。联锁停机后可通过 `PublishControlToSlave` 将启停线圈清 0，便于再次写 1 触发边沿。

### 10.10 无功-电压反馈逻辑详解（EMS 主控前提）

本节对应代码：

- `EssDeviceSimModel/GridFeedbackConventions.cs`
- `EssDeviceSimModel/ScheduledLoadSimulator.cs`
- `EssDeviceSimModel/EnergyStorageSys.cs`
- `EssDeviceSimModel/PcsModel.cs`

#### 10.10.1 控制边界

- **谁给 Q 目标**：EMS（外部控制）  
- **谁执行 Q 目标**：PCS（爬坡、限幅、状态机）  
- **谁体现 Q 对 V 的影响**：网络/负载/变压器耦合模型

即：控制权属于 EMS，物理反馈属于电网模型。

#### 10.10.2 符号与测点

- legacy 无功符号：`Qlegacy > 0` 表示感性吸收（通常拉低电压）
- PCC 测点：线电压（变压器二次侧，用于电网反馈/PCS并网判据）
- 并网电表测点：线电压/电流（断路器与变压器之间的一次侧，用于 `em.*`）

#### 10.10.3 反馈链路

当前系统的无功-电压闭环为：

1. EMS 下发 PCS 无功设定值  
2. PCS 输出有功/无功到交流侧  
3. 系统汇总总有功/总无功与总电流  
4. 变压器按负载率和功率因数计算二次侧电压  
5. 负载模型按电压更新电流，反馈到下一周期

因此，虽然 PCS 不做本地自动无功控制，但系统仍具备“无功影响电压”的动态反馈。

#### 10.10.4 为什么这样设计

- 保持控制职责清晰：EMS 做调度，PCS 做执行。  
- 便于联调：Modbus 写入的无功设定不会被本地控制器覆盖。  
- 更贴合你的仿真目标：验证 EMS 策略下的电网响应，而不是引入 PCS 自主控制策略。

---

## 11. 联调建议

### 11.1 Modbus 联调

建议使用 Modbus TCP 客户端（如 `mbpoll`）连接，端口以 `appsettings.json` 为准。当前默认示例：

- `1500`：电表 `simEm`
- `1601`：EMU 单元 1 `simEmu1`（单元 u → `1601 + u - 1`）
- `1501`：BMS 单元 1 `simBms1`（单元 i → `1501 + i - 1`）

联调步骤建议：

1. 先读取 FC4 遥测，确认服务正常。
2. 根据 CSV 核对 `FunctionCode`、地址、`-t` 类型、`Scale`。
3. 对 FC5/FC6 控制点写入，在 GUI「主电气接线」或 `dpc` 观察变化。
4. EMU 控制点用法见 **10.9**；勿对所有点统一使用 `-t 4`。

### 11.2 点值缩放

CSV 中 `Scale` 字段表示缩放倍率。例如：

- 若 `Scale=10`，寄存器值 `1234` 可能代表实际值 `123.4`
- 若 `Scale=1000`，寄存器值 `875` 可能代表实际值 `0.875`

具体解释需结合客户端解析策略与点位定义。

---

## 12. 常见问题

### 12.1 直接 `dotnet build` 失败

原因：当前目录下有多个 `.sln` 或 `.csproj`。

解决：

```zsh
dotnet build ./EssSimulator.csproj
```

### 12.2 程序启动后没有 GUI

可能原因：

- `appsettings.json` 中 `Simulator.Runtime.NoGui=true`
- 当前运行环境不适合控制台交互

### 12.3 `dpc` 设置后值又变回去了

原因：

- 该点位是由模型周期性刷新生成的遥测（FC4）
- `dpc set` 为临时写入；`ModbusDataSync.InvalidateDataShadow` 可强制下一轮回写模型值（`ModbusSimServer.SetDataStoreByMesurePointName` 内部调用）

控制点（FC5/FC6）写入模型后由 `PcsDataServer` / 控制线程维持，不会被遥测刷新覆盖。

### 12.4 mbpoll 写启停成功但 PCS 不启动

排查：

1. 是否使用 `-0` 且 FC5 点用 `-t 0`、FC6 点用 `-t 4`。
2. 是否同时满足：启停=1；并网时断路器合闸；离网/黑启动时已设孤岛电压或黑启动。
3. 是否在「主断+单元高压均合闸」时误开黑启动导致进程退出（见 10.8）。
4. 读 `param62` 反馈与 GUI 中 PCS 模式是否变为「黑启动」/「正常」。

### 12.5 无法连接 Modbus 端口

排查方式：

```zsh
lsof -i :1500
lsof -i :1501
lsof -i :1601
```

检查：

- 程序是否正常启动
- 端口是否被其他程序占用
- 配置端口是否已修改

### 12.6 命令提示“找不到对应的模型”

通常表示：

- 程序尚未完成初始化
- 设备名输入错误
- 当前命令对应的对象尚未注册到 `SimulatorHost`

---

## 13. 日志与排障

### 13.1 日志配置

日志配置文件为：

- `log4net.config`

程序启动时会优先从输出目录读取该文件；如果找不到，会尝试读取项目根目录中的同名文件。

### 13.2 建议排查顺序

1. 先看程序是否正常启动。
2. 再看 GUI“连接信息”中各端口是否已监听。
3. 再查看日志页面是否有异常。
4. 最后检查 CSV 点表与对象路径映射是否正确。

---

## 14. 文件维护建议

修改以下文件后，建议同步更新本文档：

- `appsettings.json`
- `Configuration/SimulatorConfig.cs`
- `Display/Cmd.cs`、`Display/GuiMain.cs`
- `Protocol/ModbusHostedService.cs`、`Protocol/Modbus/ModbusDataSync.cs`
- `EssSimModelApi/Mappers/PcsMapper.cs`、`EssSimModelApi/BlackStartSafety.cs`
- `EssDeviceSimModel/PcsModel.cs`、`EssDeviceSimModel/EnergyStorageSys.cs`
- `bms_bank.csv`、`bms_rack.csv`、`emu.csv`、`em.csv`
- `.cursor/skills/ess-mbpoll/`（mbpoll 命令约定）

特别是在以下场景需要更新文档：

- 增加新的设备服务或端口
- 新增命令或修改命令语法
- 新增 CSV 点位、FunctionCode 或调整联锁逻辑
- 黑启动 / 孤岛电压 / 变压器涌流等行为变更
- 改变默认启动方式或配置项

---

## 15. 当前版本说明

本文档基于当前仓库实现整理（含黑启动、孤岛电压、励磁涌流、mbpoll 联调约定），主要参考：

- `Program.cs`、`appsettings.json`
- `Configuration/SimulatorConfig.cs`
- `Display/Cmd.cs`、`Display/GuiMain.cs`
- `Protocol/ModbusHostedService.cs`、`Protocol/Modbus/ModbusDataSync.cs`、`Protocol/ModbusSimServer.cs`、`Protocol/ModbusSlave.cs`
- `EssSimModelApi/PcsDataServer.cs`、`EssSimModelApi/Mappers/PcsMapper.cs`、`EssSimModelApi/BlackStartSafety.cs`
- `EssDeviceSimModel/PcsModel.cs`、`EssDeviceSimModel/EnergyStorageSys.cs`、`EssDeviceSimModel/transformModel.cs`
- `EssDeviceSimModel/GridFeedbackConventions.cs`、`EssDeviceSimModel/ScheduledLoadSimulator.cs`
- `bms_bank.csv`、`bms_rack.csv`、`emu.csv`、`em.csv`

如代码实现与文档不一致，应以代码为准，并及时回写本文档。
