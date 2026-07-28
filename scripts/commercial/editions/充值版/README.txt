仿真模拟器（EssSimulator）充值版 — 使用说明
============================================

本包为线上托管/充值体验模板：拓扑规模与社区版相同（2 储能单元），
默认无头运行，并监听 0.0.0.0，便于反向代理或网关接入。

与社区版的关键差异：
  - NoGui = true（适合服务器部署）
  - Modbus 默认绑定 0.0.0.0；Web HttpBaseUrl = http://0.0.0.0
  - Simulator.Web.ApiKeyEnabled = true（密钥用环境变量注入，勿写入文件）
  - StaticFiles = false（默认不托管前端静态页）
  - 充值、时长、用户鉴权仍由服务端授权模块或网关控制
    （本包内 appsettings.json 只定义仿真拓扑与监听，不实现计费）

启动前请设置：
  export Simulator__Web__ApiKey='your-secret'
  Windows PowerShell: $env:Simulator__Web__ApiKey='your-secret'

快速启动：
  - Windows：运行 EssSimulator.exe（建议作为服务或计划任务托管）
  - Linux：./start.sh 或 ./EssSimulator

默认端口（2 单元）：
  - 电表 EM：1500
  - BMS：1501、1502
  - EMU：1601、1602
  - HTTP API：5050（/api/health 免 Key；其余 /api 需 X-Api-Key）

安全提示：公网暴露前必须置于授权网关之后，并限制写寄存器权限。
产品分档见 docs/产品分档与交付边界.md；网关协作见 docs/充值版授权网关约定.md。
