# 储能仿真系统操作说明书

## 概览
- **目标:** 指导在本项目中构建、运行与联调储能仿真系统（电池堆、PCS、断路器、变压器与计划负载），并通过 Modbus/IEC61850 接口进行数据交互与控制。
- **受众:** 开发/测试工程师、系统联调人员。

## 环境准备
- **操作系统:** macOS（已测试）
- **Shell:** zsh
- **依赖:**
  - `dotnet SDK` ≥ `8.0`
  - 项目内置 `NModbus`、`log4net` 配置，无需额外安装
- **代码位置:** 工作区根目录 `IEC61850-simulatorServer2/`

## 目录结构速览
- **设备仿真:** `EssDeviceSimModel/*`（电池堆、PCS、断路器、变压器、负载、系统编排）
- **数据服务:** `EssSimModelApi/*`（BMS/PCS/EM 数据生成与服务）
- **协议层:** `ModbusSimServer.cs`、`IedSimServer.cs`
- **工具与展示:** `CSVUtil.cs`、`Display/*`、`log4net.config`
- **运行与发布:** `bin/**/net8.0/*`、`Properties/PublishProfiles/*`、脚本 `run_instance.sh`、`killall.sh`

## 配置与点位
- **CSV点位表:**
  - 字段包含 `Address/FunctionCode/ParamName/Scale/ModelSim/Type/Size`
  - `FunctionCode`: 数据点 `3/4`，控制点 `6`
  - `ModelSim`: `key=value` 串，描述模型类型与参数（`ModelType`、`Arg1..Arg4`）。
- **Rack映射:** `ModbusSimServer` 会将 bank 表映射到对应 rack 表（按 `rackId`）并写入不同从站。
- **对象路径:** 控制点与数据点最终通过 `Arg1` 路径映射到模拟器内部变量（`SimServer.SetExtIfVariableVal`/`GetExtIfVariableVal`）。

## 构建与运行
- **构建:**
```zsh
cd /Users/songyinpei/jobs/IEC61850-simulatorServer2
dotnet build -c Debug
```
- **运行（示例）:** 项目包含脚本与发布产物，常见方式：
  - 直接运行（如项目提供入口）：
```zsh
dotnet run
```
  - 使用脚本（不同目录可能有 `run_instance.sh` 等）：
```zsh
./run_instance.sh
```
  - 使用已编译产物：
```zsh
cd bin/Debug/net8.0
./run_instance.sh
```

## 模拟系统启动流程
1. **系统编排线程（EnergyStorageSystem）**
   - 固定步长推进，两路 PCS + 负载电流合成，驱动断路器；合闸时并网更新变压器与 PCS 状态，分闸时置待机。
2. **Modbus 从站（ModbusSimServer）**
   - 加载 CSV 映射，按 `ModelType` 分组建 worker 线程，定时生成/抓取值并写入数据区（FC=3/4）。
   - 控制线程周期读取外部写入（FC=6），检测变化后写入模拟器变量（路径来源 `Arg1`）。
3. **IEC61850 服务（IedSimServer）**
   - 暴露对象模型，支持报告/事件订阅（具体对象路径由对象模型与映射决定）。

## 常用操作
- **开/合断路器**
  - 通过控制点（FC=6）对应的断路器对象路径写入命令值（例如 `Close=1/Trip=0`，实际点位以 CSV 映射为准）。
  - 断路器过流保护由系统自动根据合成交流电流判断。
- **设置 PCS 有/无功**
  - 写入有功/无功设定值控制点（单位 kW/kvar），系统将按爬坡曲线平滑逼近目标。
  - 可通过接口调整爬坡参数：总时长与分段数（或 `slope/interval/delay`）。
- **调整负载时段**
  - 修改 `ScheduledLoadSimulator` 的时段配置（Active/Reactive Power），系统将按二次侧电压计算负载电流并参与合成。

## 监控与日志
- **日志配置:** `log4net.config`
- **日志目录:** 通常为 `bin/net8.0/Logs/`
- **监控建议:**
  - 观察断路器状态（合/分闸）、变压器二次电压、PCS 有/无功与交流电流、系统功率因数。
  - 使用 Modbus 客户端读取数据点（FC=3/4），写入控制点（FC=6）验证生效。

## 联调与验收
- **Modbus 联调:**
  - 启动从站后，用第三方客户端按 CSV 点位表读取/写入，核对影子缓存与模拟器变量是否一致。
- **IEC61850 联调:**
  - 客户端订阅报告或事件，触发控制命令后确认对象值与状态变化。

## 故障与保护
- **PCS 故障判据:** 直流电压越界、过流、过温、并网孤岛检测；进入故障模式后功率清零。
- **安全限值:** 指令自动限幅（`MaxPower`、视在功率 ≤ 额定×1.1）；并网需电网可用。

## 常见问题排查
- **点位不更新:** 检查 CSV `ParamName/ModelSim.Arg1` 与对象路径是否一致；确认 `FunctionCode` 正确。
- **控制不生效:** 仅 FC=6 控制点会被控制线程处理；确认影子缓存 `shadowControl` 有变化。
- **端口冲突:** 修改服务端口与从站配置；确保单实例占用。
- **线程资源:** 长时间运行建议引入取消令牌与健康监控。

## 维护与扩展
- **新增点位:** 在 CSV 中添加映射，指定 `ModelType/Arg1..Arg4`；在模拟器中实现对应变量路径。
- **告警体系:** 建议将 PCS/BMS/EM 告警按表定义为位域，统一上报与复位。
- **参数调优:** 根据联调反馈调整爬坡、负载时段与保护阈值，优化响应与稳定性。
