# 储能仿真系统操作手册

## 1. 文档目的

本文档用于说明 `EssSimulator` 项目的构建、启动、配置、联调和常用操作方式，面向开发、测试与系统联调人员。

本项目是一个基于 `.NET 8` 的储能系统仿真程序，核心能力包括：

- 仿真多套电池舱、多台 PCS、变压器、断路器和负载（数量由 `Simulator.Devices` 推导）。
- 通过 Modbus TCP 对外提供 BMS、PCS/EMU、电表数据。
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
5. 默认启动控制台 GUI（当 `Simulator:NoGui=false` 时）。

### 2.2 核心模块

| 模块 | 路径 | 说明 |
|---|---|---|
| 启动入口 | `Program.cs` | 程序启动、依赖注入、托管服务注册 |
| 配置模型 | `Configuration/SimulatorConfig.cs` | 定义仿真参数、PCS、变压器、负载配置 |
| 系统模型 | `EssDeviceSimModel/` | 电池、PCS、变压器、断路器、负载等仿真逻辑 |
| 数据服务 | `EssSimModelApi/` | BMS、PCS、EM 数据同步与映射 |
| 协议服务 | `Protocol/` | Modbus 服务、对象路径访问、数据同步 |
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

| 配置项 | 默认值 | 说明 |
|---|---:|---|
| `Simulator.Runtime.SimStepMs` | `200` | 仿真主循环步长（ms） |
| `Simulator.Runtime.Speedup` | `100.0` | 仿真加速倍率 |
| `Simulator.Runtime.NoGui` | `false` | 是否禁用 GUI（无头模式） |
| `Simulator.Protocol.BaseBmsModbusPort` | `1502` | BMS Modbus 基础端口 |
| `Simulator.Protocol.BmsPortStep` | `10` | BMS 端口步长 |
| `Simulator.Protocol.BaseEmuModbusPort` | `1501` | EMU Modbus 基础端口（每储能单元一个） |
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

- `Pcs`：PCS 物理参数，如额定功率、电压范围、电流上限。
- `Transformer`：变压器参数。
- `PcsDefault`：PCS 默认限值与调度相关参数。
- `Load`：默认负载有功、无功计划值。

---

## 6. 对外服务与端口

程序启动后会自动启动多个 Modbus TCP 服务。

### 6.1 端口生成规则（以配置为准）

| 服务名 | 设备名 | 默认端口 | 数据来源 |
|---|---|---:|---|
| 电表 | `simEm` | `1500` | `em.csv` |
| PCS/EMU（单元 u） | `simEmu{u}` | `BaseEmuModbusPort + (u-1) * EmuPortStep` | `emu.csv` |
| BMS 单元 i | `simBms{i}` | `BaseBmsModbusPort + (i-1) * BmsPortStep` | `bms_bank.csv` |

说明：实际端口由 `Simulator.Protocol.*` 决定；文档中的数值仅为默认配置类初始值示例。

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
| `FunctionCode` | Modbus 功能码，常见为 `4`（读输入寄存器）或 `6`（单寄存器写） |
| `Address` | Modbus 寄存器地址 |
| `Type` | 数据类型，如 `u16`、`int16`、`int32` |
| `Size` | 位宽 |
| `ParamName` | 点位名称，供 `dpc` 命令使用 |
| `Scale` | 缩放倍数 |
| `Description` | 点位说明 |
| `ModelSim` | 内部对象路径映射配置 |

### 7.3 对象路径

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
| 命令输入 | 输入命令调试设备（`esscmd`/`breaker`/`dpc`/`dpctest`） |
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

- `<device>` 必须是已注册设备名，如 `simBms1`、`simBms2`、`simEmu`、`simEm`。
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

用于调节 PCS 特性或负载参数。

#### 语法

```text
esscmd setPcs1 slope <value>
esscmd setPcs1 interval <value>
esscmd setPcs1 delay <value>

esscmd setPcs2 slope <value>
esscmd setPcs2 interval <value>
esscmd setPcs2 delay <value>

esscmd setLoad activePower <value>
esscmd setLoad reactivePower <value>
```

#### 参数说明

| 参数 | 说明 |
|---|---|
| `slope` | PCS 功率变化速率 |
| `interval` | 每一级功率变化时间间隔，单位毫秒 |
| `delay` | 新设定值生效前的初始延时，单位毫秒 |
| `activePower` | 负载有功功率，单位 kW |
| `reactivePower` | 负载无功功率，单位 kvar |

#### 示例

```text
esscmd setPcs1 slope 0.5
esscmd setPcs1 interval 500
esscmd setPcs1 delay 100
esscmd setLoad activePower 600
esscmd setLoad reactivePower 80
```

#### 注意事项

- `setLoad` 一旦执行，会停止按时段自动切换负载，改为使用手动设定值。
- `setPcs1` 和 `setPcs2` 只作用于对应 PCS。

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
esscmd setPcs1 slope 1.0
esscmd setPcs1 interval 300
esscmd setPcs1 delay 50
```

该操作会使 PCS1 的有功/无功调整过程更快。

### 10.5 手动控制断路器

```text
breaker set true
breaker set false
```

### 10.6 EMS 主控无功模式（推荐）

本项目当前按“EMS 主控”建模：

- EMS/调度通过控制点下发有功、无功设定（例如 `5300/5301`）。
- PCS 负责执行设定值及爬坡，不在本地自动改写无功目标。
- 电池仅体现能量吸收/释放与电压电流响应。

说明：自动无功（Volt-Var）相关配置已移除，PCS 仅执行 EMS 下发的无功设定值。

### 10.7 无功-电压反馈逻辑详解（EMS 主控前提）

本节对应代码：

- `EssDeviceSimModel/GridFeedbackConventions.cs`
- `EssDeviceSimModel/ScheduledLoadSimulator.cs`
- `EssDeviceSimModel/EnergyStorageSys.cs`
- `EssDeviceSimModel/PcsModel.cs`

#### 10.7.1 控制边界

- **谁给 Q 目标**：EMS（外部控制）  
- **谁执行 Q 目标**：PCS（爬坡、限幅、状态机）  
- **谁体现 Q 对 V 的影响**：网络/负载/变压器耦合模型

即：控制权属于 EMS，物理反馈属于电网模型。

#### 10.7.2 符号与测点

- legacy 无功符号：`Qlegacy > 0` 表示感性吸收（通常拉低电压）
- 并网测点：PCC 线电压（变压器二次侧）

#### 10.7.3 反馈链路

当前系统的无功-电压闭环为：

1. EMS 下发 PCS 无功设定值  
2. PCS 输出有功/无功到交流侧  
3. 系统汇总总有功/总无功与总电流  
4. 变压器按负载率和功率因数计算二次侧电压  
5. 负载模型按电压更新电流，反馈到下一周期

因此，虽然 PCS 不做本地自动无功控制，但系统仍具备“无功影响电压”的动态反馈。

#### 10.7.4 为什么这样设计

- 保持控制职责清晰：EMS 做调度，PCS 做执行。  
- 便于联调：Modbus 写入的无功设定不会被本地控制器覆盖。  
- 更贴合你的仿真目标：验证 EMS 策略下的电网响应，而不是引入 PCS 自主控制策略。

---

## 11. 联调建议

### 11.1 Modbus 联调

建议使用任意 Modbus TCP 客户端工具连接以下端口：

- `1500`：电表 `simEm`
- `1501`：PCS/EMU `simEmu`
- `1502`：BMS 单元 1 `simBms1`
- `1512`：BMS 单元 2 `simBms2`

联调步骤建议：

1. 先读取只读寄存器，确认服务正常。
2. 根据 CSV 核对地址、数据类型、缩放倍率。
3. 对控制点执行写入，再在 GUI 或日志中观察变化。

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

- 该点位是由模型周期性刷新生成的
- `dpc set` 为临时写入；当前实现会在后续轮询中强制回归模型实时值

### 12.4 无法连接 Modbus 端口

排查方式：

```zsh
lsof -i :1500
lsof -i :1501
lsof -i :1502
lsof -i :1512
```

检查：

- 程序是否正常启动
- 端口是否被其他程序占用
- 配置端口是否已修改

### 12.5 命令提示“找不到对应的模型”

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
- `Display/Cmd.cs`
- `Protocol/ModbusHostedService.cs`
- `bms_bank.csv`
- `bms_rack.csv`
- `emu.csv`
- `em.csv`

特别是在以下场景需要更新文档：

- 增加新的设备服务或端口
- 新增命令或修改命令语法
- 新增 CSV 点位或调整点位命名规则
- 改变默认启动方式或配置项

---

## 15. 当前版本说明

本文档基于当前仓库中的以下实现整理：

- `Program.cs`
- `Configuration/SimulatorConfig.cs`
- `Display/Cmd.cs`
- `Display/GuiMain.cs`
- `Protocol/ModbusHostedService.cs`
- `Protocol/Modbus/ModbusDataSync.cs`
- `Protocol/ModbusSimServer.cs`
- `EssDeviceSimModel/PcsModel.cs`
- `EssDeviceSimModel/GridFeedbackConventions.cs`
- `EssDeviceSimModel/ScheduledLoadSimulator.cs`
- `bms_bank.csv`
- `emu.csv`
- `em.csv`

如代码实现与文档不一致，应以代码为准，并及时回写本文档。
