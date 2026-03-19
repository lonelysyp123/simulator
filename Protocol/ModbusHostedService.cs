using EssSimulator.Configuration;
using EssSimulator.Core;
using EssSimulator.EssSimModelApi;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EssSimulator
{
    /// <summary>
    /// 将所有 Modbus 从站的创建与启动封装为 IHostedService，
    /// 替代 Program.cs 中的 for 循环硬编码启动逻辑。
    /// </summary>
    public class ModbusHostedService : IHostedService
    {
        private readonly SimulatorConfig _cfg;
        private readonly List<ModbusSimServer> _servers = new();

        public ModbusHostedService(IOptions<SimulatorConfig> opts)
        {
            _cfg = opts.Value;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // 将所有 Modbus TCP 连接放到后台线程中异步执行，
            // 避免阻塞 Host 的 StartAsync 调用链。
            Task.Run(() =>
            {
                var store = SimulatorHost.Instance;

                // BMS Modbus 服务（每个储能单元一个，端口间隔 10）
                for (int i = 0; i < _cfg.UnitCount; i++)
                {
                    int port    = _cfg.BaseModbusPort + i * 10;
                    string name = $"simBms{i + 1}";
                    var server  = new ModbusSimServer("bms_bank.csv", port, name, _cfg.ClusterCount);
                    store.Register(name, server);
                    server.Start();
                    SimServer.serverListenInfo[name] = $"Modbus TCP 端口 {port}";
                    _servers.Add(server);
                }

                // PCS (EMU) Modbus 服务
                var pcs = new ModbusSimServer("emu.csv", _cfg.PcsModbusPort, "simEmu");
                store.Register("simEmu", pcs);
                pcs.Start();
                SimServer.serverListenInfo["simEmu"] = $"Modbus TCP 端口 {_cfg.PcsModbusPort}";
                _servers.Add(pcs);

                // 电表 Modbus 服务
                var em = new ModbusSimServer("em.csv", _cfg.EmModbusPort, "simEm");
                store.Register("simEm", em);
                em.Start();
                SimServer.serverListenInfo["simEm"] = $"Modbus TCP 端口 {_cfg.EmModbusPort}";
                _servers.Add(em);
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var server in _servers)
            {
                try { server.Stop(); }
                catch { /* 忽略单个服务关闭异常 */ }
            }
            return Task.CompletedTask;
        }
    }
}
