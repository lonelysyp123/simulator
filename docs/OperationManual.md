# 仿真模拟器 — 使用说明

面向 EMS/BMS/PCS 联调与测试人员。本文说明如何安装、启动、查看界面、下发命令及通过 Modbus 联调，不涉及程序内部实现。

---

## 1. 产品简介

仿真模拟器在本地模拟电池、变流器、变压器、断路器及并网点电气行为，通过 **Modbus TCP** 对外提供与现场相近的遥测与控制接口，便于在无真机或真机不足时完成主控开发联调。

- 对外名称：**仿真模拟器**
- 工程名称：EssSimulator
- 发布包为自包含程序，**无需单独安装 .NET 运行时**

---

## 2. 系统要求

| 平台 | 要求 |
|------|------|
| Windows | Windows 10/11 或 Server 2019+，64 位 (x64) |
| Linux | 与发布包架构一致（如 ARM64），见包内说明 |
| 内存 | 建议 4 GB 及以上 |
| 网络 | 联调时确保 Modbus 端口未被占用 |

---

## 3. 快速启动

### Windows

1. 解压整个文件夹（保持 exe、配置、点表在同一目录）
2. 双击 `start.bat`，或运行 `EssSimulator.exe`
3. 等待控制台出现 **协议服务就绪**，进入主菜单

### Linux

```bash
chmod +x EssSimulator start.sh
./start.sh
# 或：./EssSimulator
```

首次启动约需数秒至一分钟（视单元数量而定）。

### 开发环境（可选）

已安装 .NET 8 SDK 时，可在源码目录执行：

```bash
dotnet run --project EssSimulator.csproj
```

---

## 4. 目录与配置文件

| 文件 | 说明 |
|------|------|
| `EssSimulator.exe` / `EssSimulator` | 主程序 |
| `appsettings.json` | 单元数量、端口、电气参数 |
| `emu.csv` / `em.csv` | PCS·EMU、并网点电表点表 |
| `bms_bank.csv` / `bms_rack.csv` | BMS 堆、簇点表 |
| `log4net.config` | 日志配置 |
| `start.bat` / `start.sh` | 启动脚本 |

- 修改 `appsettings.json` 后需 **重启** 程序
- 更换 BMS 方案时，将目标 CSV **改名为** `bms_bank.csv`
- 单元数、端口以当前 `appsettings.json` 为准

---

## 5. Modbus TCP 端口

可在 `appsettings.json` → `Simulator.Protocol` 中修改。下表为常见默认：

| 设备 | 服务名 | 默认端口 | 点表 |
|------|--------|----------|------|
| 并网点电表 | simEm | 1500 | em.csv |
| BMS 第 N 路 | simBmsN | 1500+N | bms_bank.csv |
| EMU 第 U 单元 | simEmuU | 1600+U | emu.csv |

示例（8 个储能单元）：电表 1500，BMS 1501–1508，EMU 1601–1608。

- Modbus 从站地址（Unit ID）一般为 **1**
- BMS 簇表从站 ID 通常为 **1 + 簇序号**（见 `bms_rack.csv`）
- 默认监听 **127.0.0.1**（仅本机）。EMS 在其它机器联调时，需改绑定地址并放行防火墙（见 §10）

---

## 6. GUI 主菜单

| 菜单 | 作用 |
|------|------|
| 主电气接线 | 查看 220 kV / 35 kV / 690 V 拓扑、PCS/BMS 状态、P设/P实、Q设/Q实 |
| 电池堆簇信息 | 按 BMS 路号查看 SOC、电压、电流等 |
| 电池单体信息 | 查看选定簇的单体电压 |
| 命令输入 | 执行调试命令（见 §8） |
| 连接信息 | Modbus 监听端口与客户端连接 |
| 日志信息 | 运行日志 |

**根菜单快捷键**

| 按键 | 功能 |
|------|------|
| ↑ / ↓ | 选择菜单 |
| Enter | 进入 |
| Esc | 返回；在根菜单下退出程序 |

**主电气接线页**

| 按键 | 功能 |
|------|------|
| ↑ / ↓ / ← / → | 翻页（多单元时） |
| Tab | 表格视图 / ASCII 图切换 |
| **:** 或 **C** | 打开 **临时命令**（全屏 `cmd>`，执行一条后按任意键返回） |
| Esc | 返回上一级菜单 |

**临时命令**

- 进入：主接线页按 `:`（Shift+/）或 `C`
- Enter：执行当前命令
- 执行后按任意键返回主接线
- ↑ / ↓：翻阅历史命令
- ← / →：移动光标编辑
- Esc：清空当前输入

---

## 7. EMS 联调常用点（单元 1 · 端口 1601）

点名以 `emu.csv` 为准。mbpoll 建议加 **-0**，地址与 CSV 中 **Address** 列一致。

**Scale 说明**：写寄存器时填 **原始值**；物理量 = 原始值 ÷ Scale。  
例：`yt0` Scale=10，要写 100 kW → 写 **1000**；`dpc` 同样写原始值 **1000**。

| 功能 | 点名 | 地址 | FC | 说明 |
|------|------|------|-----|------|
| 高压断路器 | yx0 | 1000 | 5 | 0=分 1=合 |
| PCS1 启停 | yx3 | 1003 | 5 | 0=停 1=运行 |
| PCS1 有功设定 | yt0 | 40000 | 6 | kW，Scale 10 |
| PCS1 无功设定 | yt1 | 40001 | 6 | kvar，Scale 10 |
| PCS1 有功反馈 | yc27 | 10028 | 4 | int32，Scale 10 |
| PCS1 运行状态 | yc44 | 10053 | 4 | 见点表说明 |

单元 U 的 EMU 端口 = **1600 + U**。每单元含 2 路 PCS（PCS1 对应 `PcsList[0]`，点名见 `emu.csv`）。

**mbpoll 示例**

```bash
# 读 PCS1 有功反馈（FC4）
mbpoll -0 -t 3:int -a 1 -r 10028 -c 2 -p 1601 127.0.0.1

# 写 PCS1 启停 = 运行（FC5）
mbpoll -0 -t 0 -a 1 -r 1003 -p 1601 -1 127.0.0.1 1

# 写 PCS1 有功 100 kW（Scale 10 → 写 1000）
mbpoll -0 -t 3:int -a 1 -r 40000 -c 1 -p 1601 127.0.0.1 1000
```

完整点表见包内 `emu.csv`、`em.csv`、`bms_bank.csv`。

---

## 8. 内置控制台命令

在 **命令输入** 菜单，或主接线 **临时命令**（`:` / `C`）中使用。

### esscmd

```text
esscmd help

esscmd setLoad activePower <kW>
esscmd setLoad reactivePower <kvar>     # 35 kV 站用负荷，负值=从电网取电

esscmd setbms1 power on                 # BMS 直流侧并网（off=离网）

esscmd link pcs1 on|off                 # 模拟 PCS 所属 EMU 通讯中断/恢复
esscmd link bms1 on|off                 # 模拟 BMS 通讯中断/恢复
esscmd link status
```

### dpc（按点名读写）

```text
dpc <设备>.<点名> get
dpc <设备>.<点名> set <值>
```

设备名示例：`simEmu1`、`simBms1`、`simEm`。  
**控制点** `set` 写入 **Modbus 原始寄存器值**（与 mbpoll 一致）；**遥测点** 可能被下一周期刷新。

**PCS1 常用示例（单元 1）**

```text
dpc simEmu1.yx0 set 1          # 高压断合闸
dpc simEmu1.yx3 set 1          # PCS1 启动
dpc simEmu1.yx3 set 0          # PCS1 停机
dpc simEmu1.yt0 set 1000       # 有功 100 kW（Scale 10）
dpc simEmu1.yt1 set 200        # 无功 20 kvar
dpc simEmu1.yc44 get           # 读运行状态
```

### breaker（主断路器）

```text
breaker set true    # 合闸
breaker set false   # 分闸
```

### dpctest（自动化脚本）

```text
dpctest list
dpctest <测试名>
```

步骤定义在 `autotest.json` 中。

### 退出命令输入菜单

输入 `exit` 返回主菜单（不退出程序）。

---

## 9. 典型联调顺序（示例）

```text
dpc simEmu1.yx0 set 1
esscmd setbms1 power on
dpc simEmu1.yx3 set 1
dpc simEmu1.yt0 set 500
```

然后在主接线查看 **P设 / P实**，或用 mbpoll 读反馈点。

---

## 10. 无界面模式

编辑 `appsettings.json`：

```json
"Simulator": {
  "Runtime": {
    "NoGui": true
  }
}
```

无 GUI 时仍提供 Modbus 服务；调试需改回 `NoGui: false` 或使用 EMS/mbpoll。

---

## 11. 防火墙与网络

Modbus 连不上时依次检查：

1. 是否已出现「协议服务就绪」
2. 端口是否与 `appsettings.json` 一致、未被占用
3. Windows：放行 `EssSimulator.exe` 或 TCP 1500–1610 等端口
4. Linux：firewalld / iptables 放行对应端口
5. EMS 连接的 IP 是否与程序绑定地址一致（默认 127.0.0.1 仅本机）

---

## 12. 常见问题

**Q: 启动后 Modbus 连不上？**  
A: 等待协议就绪；检查端口、防火墙、IP 绑定。

**Q: PCS 已发启停仍显示停机？**  
A: 检查高压断（yx0）是否合闸、BMS 是否并网（`esscmd setbms1 power on`）、单元断状态。

**Q: 写了有功设定但 P实 为 0？**  
A: 确认 PCS 已运行、已并网；看主接线 P设/P实；写寄存器注意 Scale（100 kW → 原始值 1000）。

**Q: dpc 写的功率与预期差 10 倍？**  
A: 控制点应写 **原始寄存器值**，不是 kW 物理量（除非 Scale=1）。

**Q: 修改单元数后无法启动？**  
A: 检查 `appsettings.json` 语法、EssUnits 配置、端口是否冲突。

---

## 13. 发布包与版本

社区版/定制版发布目录说明见 `docs/dist-layout.md`（源码仓库内）。

Windows 发布示例（商业版）：

```bash
EDITION=社区版 ./scripts/commercial/publish-windows.sh
```

开发联调可直接使用 `./scripts/publish-windows.sh`（见 `scripts/README.md`）。

---

## 14. 进一步阅读（可选）

| 文档 | 内容 |
|------|------|
| `scripts/README.txt` | 社区版随包简明说明 |
| `docs/appsettings.explained.md` | 配置项说明 |
| `docs/EnergyStorageSimulationSystem.md` | 方案与架构（技术人员） |

日常使用以本文与随包 `README.txt` 为主即可。
