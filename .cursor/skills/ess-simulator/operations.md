# 仿真模拟器 — 构建、运行与联调

## 开发环境

- .NET SDK 8.0+
- 仓库根目录即项目根

```bash
dotnet build
dotnet run --project EssSimulator.csproj
dotnet test EssSimulator.Tests/EssSimulator.Tests.csproj
```

## 配置文件

| 文件 | 作用 |
|------|------|
| `appsettings.json` | 单元拓扑、`Devices[]`、端口、PCC/变压器/负载、DataExchange 周期 |
| `bms_bank.csv` | BMS 堆 Modbus 点（**换表 = 改名为此文件**） |
| `bms_rack.csv` | BMS 簇 Modbus 点 |
| `emu.csv` | PCS/EMU 点 |
| `em.csv` | 站用/PCC 电表点 |
| `log4net.config` | 日志 |

修改拓扑后重启进程；Modbus 端口冲突时需改 `Simulator.Protocol`。

## 启动流程

`Program.cs` → 加载配置 → 创建 `EnergyStorageSystem` → 启动 `BmsDataService` / `PcsDataServer` / `EmDataService` / `ModbusHostedService` →（可选）GUI。

- `Simulator.Runtime.NoGui: true` — 无界面，适合服务器/ARM 部署
- 启动会等待 Modbus 就绪（GUI 模式最多约 60s）

## 控制台命令（运行时）

`help`、`esscmd`、`breaker`、`dpc` 等，详见 `docs/OperationManual.md`。

## Modbus 联调

1. 确认 `appsettings.json` 中端口与防火墙
2. 确认使用的点表文件（尤其 `bms_bank.csv` 版本）
3. 生成 mbpoll → **ess-mbpoll** skill（按 CSV `FunctionCode` 选 `-t`）
4. BMS rack 点：从站 ID 通常 `1 + rackIndex`（见 `ModbusTCPSlave`）

## 发布

**Linux ARM64（自包含单文件 + 外置配置）：**

```bash
./scripts/publish-linux.sh
# 输出: dist/linux-arm64/ ，归档 dist/EssSimulator-linux-arm64.tar.gz
```

发布包需含：`EssSimulator`、`appsettings.json`、`*.csv`、`log4net.config`、`start.sh`。

**Windows x64：** `./scripts/publish-windows.sh` → `dist/win-x64/`

## 联调排错清单

- [ ] 仿真进程已启动且 `serverListenInfo` 显示端口
- [ ] mbpoll/主站 IP、端口、Slave ID 正确
- [ ] 点表 `Address` + mbpoll `-0` 与 CSV 一致
- [ ] 写控制点：确认 Semantics（Pulse/Hold/Edge）与 `Scale`
- [ ] SOC/功率：BMS 是否已 link、PCS 是否运行、断路器是否合闸

## 文档

- 操作手册：`docs/OperationManual.md`
- 方案设计：`docs/EnergyStorageSimulationSystem.md`
