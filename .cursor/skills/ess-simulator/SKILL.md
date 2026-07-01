---
name: ess-simulator
description: >-
  Describes EssSimulator (对外名称「仿真模拟器」): large-scale energy-storage
  simulation for EMS integration testing, architecture, config, Modbus point
  maps, build/run workflow. Apply when the user discusses EssSimulator, 储能仿真,
  仿真模拟器, EMS/BMS/PCS 联调, 大储测试, or this repository.
disable-model-invocation: false
---

# 仿真模拟器（EssSimulator）

## 项目定位

| 项 | 说明 |
|----|------|
| **对外名称** | 仿真模拟器 |
| **行业** | 储能行业，**大储项目**测试联调 |
| **用户** | EMS 开发与测试人员 |
| **与真机** | **软件仿真**：模拟真实物理环境与设备行为，对外 Modbus 反馈与联调预期一致；非硬件在环孪生 |
| **拓扑** | 以 `appsettings.json` 为准，**随项目变化**；回答端口/单元数/电压等级前必须先读当前配置 |

## 回答规范（必须遵守）

1. **默认简体中文**；技术标识（路径、寄存器、类名）保留英文。
2. **先 actionable、后原理**：联调/测试类问题优先给端口、点表、命令；架构细节放后或链到 reference。
3. **Modbus 读写命令** → 使用 [.cursor/skills/ess-mbpoll/SKILL.md](../ess-mbpoll/SKILL.md)，禁止默认 `-t 4`。
4. **每答一问，先想「还缺什么信息」**：若拓扑、点表版本、单元号、现象（读值/写值/日志）未明确，**先简短列出需要用户补充的项**，再给有条件的答案；不要假设与当前 `appsettings.json` / CSV 不一致的配置。
5. **配置即真相**：单元数、端口、PCC/站用电压、`Devices[]` 以仓库内 **`appsettings.json`** 和 **`*.csv`** 为准，勿凭记忆写死数字。
6. **点表切换**：BMS 堆表固定文件名 `bms_bank.csv`；换方案时将目标 CSV **改名为** `bms_bank.csv` 即可。

### 常见需向用户确认的信息

| 场景 | 建议先问 |
|------|----------|
| Modbus 读不到/写无效 | 主机 IP、端口、单元号、点名/地址、用的哪份 CSV、FunctionCode |
| 功率/SOC/电压不对 | 哪一路 BMS/EMU、期望物理量、寄存器原始值、是否已并网/合闸 |
| 改拓扑或加单元 | 目标单元数、是否改 `Devices[]`、端口是否冲突 |
| 发布/部署 | 目标 OS/架构、是否需要 GUI、配置是否与联调环境一致 |
| 行为是否符合真机 | 具体工况（并网/离网/黑启动）、真机侧预期 vs 仿真读数 |

## 30 秒架构

```
EMS/测试工具 ──Modbus TCP──► ModbusSimServer + DataExchangeSession
                                    │ 点表 CSV (ModelSim → 对象路径)
                                    ▼
                         EssSimModelApi (BmsMapper / PcsMapper / EmMapper)
                                    │
                                    ▼
              EnergyStorageSystem 主循环 (PeriodicTimer, ~PropagationIntervalMs)
                                    │
              ElectricalNetwork + NetworkSolver / 潮流传播
                                    │
              PcsDevice · TransformerDevice · Breaker · BmsRackDevice · …
```

- **主循环**：Host 托管 + 固定步长；每步求解电气网络 → 更新 PCS/BMS → Mapper 写 DTO。
- **对外接口**：BMS `simBms{N}`、EMU `simEmu{N}`、电表 `simEm`；点位由 CSV + `Scale` 映射。
- **控制语义**：DataExchange 管道（遥测 / 控制 / 反馈）；PCS 启停 Hold、BMS 并网 Pulse 等见 `PointCatalogLoader`。

## 关键文件速查

| 用途 | 路径 |
|------|------|
| 运行配置 | `appsettings.json` |
| BMS 堆点表 | `bms_bank.csv`（可整表替换） |
| BMS 簇点表 | `bms_rack.csv` |
| PCS/EMU 点表 | `emu.csv` |
| 电表点表 | `em.csv` |
| 主循环 | `EssDeviceSimModel/EnergyStorageSys.cs` |
| 数据交换 | `DataExchange/` |
| Modbus 服务 | `Protocol/ModbusHostedService.cs` |
| 操作手册 | `docs/OperationManual.md` |
| 设计说明 | `docs/EnergyStorageSimulationSystem.md` |
| 发布（商业） | `scripts/commercial/publish-all.sh` → `dist/{社区版,充值版,定制版}/{win-x64,linux-arm64}/` |
| 发布（开发） | `scripts/publish-windows.sh` / `publish-linux.sh` → `dist/win-x64`、`dist/linux-arm64` |

## 默认 Modbus 端口（以配置为准）

读 `Simulator.Protocol`：

- EM：`EmModbusPort`（示例 1500）
- BMS 路 N：`BaseBmsModbusPort + (N-1) * BmsPortStep`（示例 1501 起）
- EMU 单元 N：`BaseEmuModbusPort + (N-1) * EmuPortStep`（示例 1601 起）

## 单元与对象对应（固定规则）

每个 `Devices[]` 项 = **1 个储能单元 = 2 路 PCS + 2 路 BMS**：

- 单元 U 的 EMU：`emuU`，Modbus `simEmuU`
- 该单元 PCS：`emuU.PcsList[0..1]` ↔ `ess._pcsList[...]`
- BMS 通道按全局序号 `bms1…bmsN`，每路独立 `simBms{i}`

## 开发与验证

```bash
dotnet build
dotnet test EssSimulator.Tests/EssSimulator.Tests.csproj
```

- 点表绑定/SOC 等：`EssSimulator.Tests/DataExchange/BmsTelemetryBindingTests.cs`
- 改模型/ Solver / 管道后应跑相关测试

## 详细资料

- 架构与数据流：[architecture.md](architecture.md)
- 构建、运行、发布、联调：[operations.md](operations.md)
- 术语：[glossary.md](glossary.md)
