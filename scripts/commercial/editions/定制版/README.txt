仿真模拟器（EssSimulator）定制版 — 使用说明
============================================

本包为私有化定制部署版本，拓扑与点表按项目配置（当前 appsettings.json
与 CSV 点表即为交付内容）。启动方式：

  - Windows：双击 start.bat 或运行 EssSimulator.exe
  - Linux：./start.sh

Modbus 端口以 appsettings.json 中 Simulator.Protocol 为准；单元数由
Devices / EssUnits 配置决定。替换点表时保持文件名不变（emu.csv /
bms_bank.csv 等），重启进程生效。

交付边界（含/不含项）见 docs/产品分档与交付边界.md。
详细联调步骤、命令与故障排查见 docs/用户手册.md、docs/指令详细说明.md。
