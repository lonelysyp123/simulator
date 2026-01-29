# 储能仿真系统方案设计

## 项目目标
- 仿真范围：搭建储能系统（ESS）端到端仿真，包括两套电池堆（Rack）、两台 PCS、主断路器、变压器以及计划负载；提供 Modbus/IEC61850 等接口以便上位机/调度系统联调。
- 可扩展性：通过点位映射（CSV）与模型参数化，支持多簇电池与多设备拓扑，便于后续扩展 EM/BMS/PCS 告警与控制策略。

## 架构总览
- 模拟层（EssDeviceSimModel/*）：设备模型与状态机
  - BatteryRackSimulator：电池簇/包/单体建模、SOC/电压/温度演化。
  - PCSSimulator：有/无功指令爬坡、并网/离网模式、直流/交流侧参数计算、故障检验。
  - Breaker：过流保护、合/分闸逻辑。
  - TransformerSimulator：一二次侧电压/损耗/阻抗、功率因数耦合。
  - ScheduledLoadSimulator：时段化负载（有/无功）与电流计算。
  - EnergyStorageSystem：线程驱动的系统编排，耦合各设备、推进仿真时钟与交互。
- 接口层：协议适配与数据服务
  - ModbusSimServer：基于 NModbus 的从站模拟；按 CSV 点位映射进行读写；worker 线程生成数据、control 线程处理外部写入。
  - IedSimServer：IEC61850 服务端模拟（GOOSE/报告等）。
  - EssSimModelApi/*：BMS/PCS/EM 的数据生成与服务（如 BmsDataServer、PcsDataServer、EmDataService）。
- 工具与基础设施：
  - CSVUtil：点位表解析。
  - TCPCommunicator：通讯抽象。
  - ObjectsCollect/ObjectPathResolver：变量路径解析与对象聚合。
  - log4net.config：日志配置。
- 展示层（Display/*）：简易控制台/GUI 输出（ConsoleLogDisplay、GuiMain、Cmd）。

## 数据流与线程模型
- 仿真主循环（EnergyStorageSystem）：固定步长（默认 200ms）推进时钟
  - 采样两台 PCS 的交流电流，叠加计划负载电流，驱动断路器；合闸时给变压器一次侧电压并将 PCS 并网；分闸时置待机并电网不可用。
  - 用电池堆总电压更新 PCS 直流侧；用 PCS 直流电流更新电池堆状态。
- Modbus 数据生成：
  - 每种 ModelType 建一个 worker 线程，按 PriorityQueue(nextChange) 定时生成/抓取值，合并变化写入从站。
  - shadowData 避免重复写；nextChange/rackNextChange 控制调度。
- 控制通道：
  - StartControlThread 周期读取控制点（FC=6），解析变化后通过 SimServer.SetExtIfVariableVal 写入模拟器，影子缓存记录 shadowControl。
- PCS 指令爬坡：
  - 有/无功各一后台线程，按 ComputePowerRampStages 生成阶段目标与延时，遇到新设定值立即打断重算；支持线性曲线，预留二次/平方根。
- 并发安全：
  - 采用 _stateLock 保证共享状态一致；线程优先级 BelowNormal；分段睡眠规避 busy-wait。

## 接口与配置
- 点位映射 (CSV)：
  - 字段包含 Address/FunctionCode/ParamName/Scale/ModelSim/Type/Size。
  - ModelSim 以 key=value 串描述模型类型与参数（Arg1..Arg4）。
  - Bank/Rack 表对应不同设备与从站，Rack 表按 rackId 生成多从站写入。
- Modbus 约定：
  - 数据点：FC=3/4；控制点：FC=6。
  - 写入分组到不同从站（主从站及各 rack 从站）。
- IEC61850：
  - IedSimServer 暴露对象模型、报告与事件；点位与模拟器变量通过对象路径映射。
- 日志与配置：
  - log4net.config 控制输出级别与目标。
  - 运行脚本：run_instance.sh、killall.sh、run1.sh/.bat；发布配置在 Properties/PublishProfiles。

## 错误处理与健壮性
- 防抖与回退：影子缓存只在变化时写；负载电压缺失时回退额定值。
- 容错：线程 catch+日志；Modbus 写失败分从站隔离；PCS 过载/电压/温度/孤岛保护置故障。
- 校验：指令限幅（MaxPower、视在功率 ≤ 额定×1.1）；并网前检查电网可用。

## 测试与运行
- 构建：
  - dotnet build -c Debug
- 快速运行（示例）：
  - bin/net8.0/run_instance.sh 或 dotnet run（如有入口）。
- 单元测试建议：
  - 电池模型：SOC 演化、温度/电压响应。
  - PCS：爬坡阶段生成、模式切换、故障检验。
  - Modbus：CSV 解析、ModelType 调度、读写与影子缓存。
- 集成联调：
  - 启动 Modbus 从站后，用第三方 Modbus 客户端验证数据区与控制区的映射与写入生效。
  - IEC61850 客户端订阅报告与事件，校验对象路径与值更新。

## 性能与调优
- 线程调度：优先队列按到期时间驱动；合理设置 nextChange 间隔与批写大小。
- 爬坡参数：_defaultRampDurationMs 与 _rampSteps 控制响应速度与平滑性；或使用 slope/interval/delay 模式。
- 资源占用：分段睡眠与变化写降低 CPU；日志级别在联调时降噪。

## 安全与部署
- 端口管理：限制 Modbus/IEC61850 端口暴露范围；本地开发仅监听 0.0.0.0 时需防火墙策略。
- 输入校验：控制点写入类型与量纲校验；CSV 映射加载失败快速失败。
- 发布：使用 FolderProfile.pubxml 发布；按目标环境复制 Config/、点位表与日志配置。

## 路线图
- 模型增强：完成二次/平方根爬坡；PCS 控制策略（恒功率/恒电流/下垂）细化。
- 多簇支持：扩展 rack 点位表与从站编号策略，支持多簇并行。
- 告警体系：将 PCS/BMS/EM 告警按表定义成位域，统一上报与复位机制。
- EM 接口：完善 EmDataService 与电表映射，增加负荷侧数据统计。
- 可视化：Display/GuiMain 增强设备状态与告警面板。
- 配置中心：引入统一 YAML/JSON 配置，替代分散的 CSV 细节。

## 风险与缓解
- 并发一致性：多线程读写共享结构需严格使用锁；建议引入并发集合或 actor 模式。
- 映射偏差：CSV 与模型变量路径不一致导致写入失败；增加启动期自检与缺失提示。
- 长线程运行：增加取消令牌与健康监控，避免无终止的后台线程。
- 过载参数：错误的功率/电压参数可能使故障逻辑长时间触发；提供安全限值与调试开关。
