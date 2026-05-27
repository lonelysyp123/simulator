EssSimulator - Windows 运行说明
================================

系统要求
--------
- Windows 10 / Windows 11 / Windows Server 2019 及以上
- 64 位 (x64)
- 无需单独安装 .NET（已自包含运行时）

快速启动
--------
1. 解压整个 win-x64 文件夹到任意目录（保持文件在同一文件夹内）
2. 双击 start.bat，或直接运行 EssSimulator.exe
3. 首次启动会加载 Modbus 服务，请等待控制台出现“协议服务”就绪信息

配置文件
--------
- appsettings.json   仿真与端口配置（可按需修改）
- *.csv              Modbus 点表
- log4net.config     日志配置

默认 Modbus 端口（可在 appsettings.json 修改）
----------------------------------------------
- 电表 EM:     1500
- BMS 路 1:    1501（后续通道递增）
- EMU 单元 1:  1601（后续单元递增）

防火墙
------
若外部 mbpoll/主站无法连接，请在 Windows 防火墙中允许 EssSimulator.exe，
或放行上述 TCP 端口。

无界面模式
----------
在 appsettings.json 中设置:
  "Simulator": { "Runtime": { "NoGui": true } }

控制台命令
----------
启动后可用: esscmd / breaker / dpc / help

详细说明见项目 docs/OperationManual.md
