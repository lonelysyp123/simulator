仿真模拟器（EssSimulator）社区版 — 使用说明
============================================

一、产品简介
------------
仿真模拟器用于大储 EMS/BMS/PCS 联调与测试。程序在本地模拟电池、变流器、
变压器、断路器及并网点电气行为，通过 Modbus TCP 对外提供与现场相近的
遥测与控制接口，便于在无真机或真机不足时完成主控开发联调。

本包为自包含发布，无需单独安装 .NET 运行时。


二、系统要求
------------
Windows 版：
  - Windows 10 / 11 / Windows Server 2019 及以上，64 位 (x64)

Linux 版：
  - Linux ARM64 (aarch64) 或对应架构发布包说明

内存：建议 4 GB 及以上；联调时请确保 Modbus 端口未被占用。


三、快速启动
------------
【Windows】
  1. 解压整个文件夹到任意目录（保持 exe、配置、点表在同一目录）
  2. 双击 start.bat，或运行 EssSimulator.exe
  3. 等待控制台出现协议服务就绪后，进入主菜单

【Linux】
  1. 解压 tar.gz 到任意目录
  2. 执行：chmod +x EssSimulator start.sh && ./start.sh
     或直接：./EssSimulator
  3. 等待协议服务就绪后，进入主菜单

首次启动约需数秒至一分钟（视单元数量而定）。


四、目录与配置文件
------------------
  EssSimulator.exe / EssSimulator   主程序
  appsettings.json                  仿真拓扑、端口、电气参数（可按项目修改）
  emu.csv / em.csv                  PCS·EMU、并网点电表 Modbus 点表
  bms_bank.csv / bms_rack.csv       BMS 堆、簇 Modbus 点表
  log4net.config                    日志配置
  start.bat / start.sh              启动脚本（可选）

修改 appsettings.json 后需重启程序生效。
点表文件名需与程序内置一致；更换 BMS 方案时将目标 CSV 改名为 bms_bank.csv。


五、默认 Modbus TCP 端口
------------------------
（可在 appsettings.json → Simulator.Protocol 中修改；下表为常见默认）

  设备              服务名      默认端口        说明
  ------------------------------------------------------------------
  并网点电表        simEm       1500            em.csv
  BMS 第 N 路       simBmsN     1500+N          例：BMS1→1501
  EMU 第 U 单元     simEmuU     1600+U          例：单元1→1601

  单元数由 appsettings.json 中 EssUnits 条目决定。
  当前示例配置为 8 个储能单元时：BMS 1501–1508，EMU 1601–1608，电表 1500。

  Modbus 从站地址（Unit ID）一般为 1。
  BMS 簇表（bms_rack.csv）从站 ID = 1 + 簇序号（以实际点表为准）。

  程序默认监听 127.0.0.1。若 EMS 在其它机器联调，需在配置中改为 0.0.0.0
  并放行防火墙（见下文）。


六、GUI 主菜单
--------------
  主电气接线    查看 220 kV / 35 kV / 690 V 拓扑、PCS/BMS 状态、P/Q 设定与实测
  电池堆簇信息  按 BMS 路号查看 SOC、电压、电流等
  命令输入      执行 esscmd / dpc / breaker 等调试命令
  连接信息      查看 Modbus 服务监听与客户端连接
  日志信息      查看运行日志

  根菜单快捷键：
    ↑ / ↓     选择菜单
    Enter     进入页面
    Esc       返回或退出（根菜单下退出程序）

  主电气接线页：
    ↑/↓/←/→   多单元时翻页
    Tab       表格 / ASCII 视图切换
    : 或 C    打开临时命令（全屏 cmd>，执行一条后按任意键返回）
    Esc       返回上一级菜单

  临时命令：
    Enter     执行命令
    任意键    执行后返回主接线
    ↑ / ↓     翻阅历史命令
    ← / →     移动光标编辑
    Esc       清空当前输入


七、EMS 联调常用点（单元 1 · simEmu1 · 端口 1601）
--------------------------------------------------
  以下地址配合 mbpoll 时建议加 -0，与点表 Address 列一致。

  【Scale】写寄存器填原始值；物理量 = 原始值 ÷ Scale。
  例：yt0 Scale=10，要写 100 kW → 写 1000；dpc 同样写 1000。

  功能           点名        地址    FC    说明
  ------------------------------------------------------------------
  高压断路器     yx0         1000    5     0=分 1=合
  PCS1 启停      yx3         1003    5     0=停 1=运行
  PCS1 有功设定  yt0         40000   6     kW，Scale 10
  PCS1 无功设定  yt1         40001   6     kvar，Scale 10
  PCS1 有功反馈  yc27        10028   4     int32，Scale 10
  PCS1 运行状态  yc44        10053   4     见点表说明

  单元 U 的 EMU 端口 = 1600 + U。完整点表见 emu.csv、em.csv、bms_bank.csv。

  读 PCS1 有功反馈（mbpoll -t 3）：
    mbpoll -0 -t 3:int -a 1 -r 10028 -c 2 -p 1601 127.0.0.1

  写 PCS1 启停 = 运行（mbpoll -t 0）：
    mbpoll -0 -t 0 -a 1 -r 1003 -p 1601 -1 127.0.0.1 1

  写 PCS1 有功 100 kW（Scale 10 → 写 1000）：
    mbpoll -0 -t 3:int -a 1 -r 40000 -c 1 -p 1601 127.0.0.1 1000


八、内置控制台命令
------------------
  在「命令输入」菜单，或主接线页 :/C 临时命令中使用：

  esscmd help
      查看全部子命令

  esscmd setLoad activePower <kW>
  esscmd setLoad reactivePower <kvar>
      设置 35 kV 站用负荷（负值=从电网取电）

  esscmd setbms1 power on|off
      BMS 直流侧并网/离网

  esscmd link pcs1 on|off
  esscmd link bms1 on|off
      模拟通讯中断/恢复（非电气离网）

  dpc simEmu1.yx0 set 1
      高压断合闸

  dpc simEmu1.yx3 set 1
  dpc simEmu1.yx3 set 0
      PCS1 启停

  dpc simEmu1.yt0 set 1000
      PCS1 有功 100 kW（原始值，Scale 10）

  dpc simEmu1.yt1 set 200
      PCS1 无功 20 kvar

  breaker set true|false
      主断路器合/分

  命令输入菜单输入 exit 返回主菜单。


九、典型联调顺序（示例）
------------------------
  dpc simEmu1.yx0 set 1
  esscmd setbms1 power on
  dpc simEmu1.yx3 set 1
  dpc simEmu1.yt0 set 500


十、无界面模式（服务器/自动化）
--------------------------------
  编辑 appsettings.json：

    "Simulator": {
      "Runtime": {
        "NoGui": true
      }
    }

  无 GUI 时仍提供 Modbus 服务；调试需改回 NoGui:false 或使用 EMS/mbpoll。


十一、防火墙与网络
------------------
  若 EMS 或 mbpoll 无法连接：
    1. 确认端口与 appsettings.json 一致且未被占用
    2. Windows：允许 EssSimulator.exe 或放行 TCP 1500–1610 等端口
    3. Linux：firewalld/iptables 放行对应端口
    4. 确认 EMS 连接的 IP 与程序绑定地址一致（默认 127.0.0.1 仅本机）


十二、常见问题
--------------
  Q: 启动后 Modbus 连不上？
  A: 等待「协议服务就绪」；检查端口、防火墙、IP 绑定。

  Q: PCS 有启停命令但仍显示停机？
  A: 检查高压断 yx0 是否合闸、BMS 是否并网（esscmd setbms1 power on）。

  Q: 写了有功设定但读数为 0？
  A: 确认 PCS 已运行、已并网；主接线看 P设/P实。

  Q: dpc 功率与预期差 10 倍？
  A: 控制点写 Modbus 原始值（100 kW → yt0 写 1000），不是 kW 数。

  Q: 修改单元数量后无法启动？
  A: 检查 appsettings.json 语法、EssUnits 配置及端口是否冲突。


十三、版本与支持
----------------
  社区版供本地联调与评估使用（默认仅本机监听）。
  线上托管体验请使用充值版；项目完整拓扑请使用定制版。
  分档边界见 docs/产品分档与交付边界.md（若随包提供）。

  详细说明见 docs/用户手册.md、docs/指令详细说明.md（若随包提供）。
  对外产品名称：仿真模拟器
  工程名称：EssSimulator
