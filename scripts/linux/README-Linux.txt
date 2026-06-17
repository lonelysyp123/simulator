EssSimulator - Linux 运行说明
================================

系统要求
--------
- Linux ARM64 (aarch64)，例如树莓派 4/5、ARM 服务器、Apple Silicon 上通过 Linux VM 等
- 无需单独安装 .NET（已自包含运行时）

快速启动
--------
1. 解压 EssSimulator-linux-arm64.tar.gz 到任意目录
2. 执行: chmod +x EssSimulator start.sh && ./start.sh
   或直接: ./EssSimulator
3. 首次启动会加载 Modbus 服务，请等待控制台出现协议服务就绪信息

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
若外部 mbpoll/主站无法连接，请放行上述 TCP 端口（如 firewalld/iptables）。

无界面模式
----------
在 appsettings.json 中设置:
  "Simulator": { "Runtime": { "NoGui": true } }

控制台命令
----------
启动后可用: esscmd / breaker / dpc / help

详细说明见项目 docs/OperationManual.md
