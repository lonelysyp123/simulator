仿真模拟器（EssSimulator）商业版 — 使用说明
============================================

本包为商业买断版：完整开发级能力（多单元可配置、Web 主接线、命令/脚本、
白盒切片等高级功能）。须放置有效 license.txt 后方可启动。

档位开关（appsettings.json → Simulator.Edition）：
  Name = Commercial
  AllowDroopSlices = true
  AllowMainline3d = true
  AllowTopologyEditor = true
  LockTopology = false

授权（Simulator.License.Required = true）：
  1. 本机运行: ./EssSimulator --machine-id   （或 scripts/license/get-machine-id.*）
  2. 将机器码发给提供方，获取 license.txt
  3. 把 license.txt 放到本目录（与 EssSimulator 同级）后启动

快速启动：
  - Windows：双击 start.bat 或运行 EssSimulator.exe
  - Linux / macOS：./start.sh

Modbus / HTTP 端口以 appsettings.json 为准。
详细说明见 docs/授权说明.md、docs/用户手册.md（若随包提供）。
