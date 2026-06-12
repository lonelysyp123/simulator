using EssSimulator.Configuration;
using EssSimulator.Core;
using EssSimulator.DataExchange;
using EssSimulator.DataExchange.Catalog;
using EssSimulator.DataExchange.Config;
using EssSimulator.EssSimModelApi;
using log4net;
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
        private static readonly ILog Log = LogManager.GetLogger(typeof(ModbusHostedService));
        private readonly SimulatorConfig _cfg;
        private readonly DataExchangeOptions _dataExchange;
        private readonly List<ModbusSimServer> _servers = new();

        public ModbusHostedService(
            IOptions<SimulatorConfig> opts,
            IOptions<DataExchangeOptions> dataExchangeOpts)
        {
            _cfg = opts.Value;
            _dataExchange = dataExchangeOpts.Value;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // 将所有 Modbus TCP 连接放到后台线程中异步执行，
            // 避免阻塞 Host 的 StartAsync 调用链。
            Task.Run(() =>
            {
                var store = SimulatorHost.Instance;
                var bmsCfg = _cfg.GetBmsDeviceConfigs();

                // BMS Modbus 服务（每个储能单元一个，端口间隔 10）
                for (int i = 0; i < _cfg.UnitCount; i++)
                {
                    int port    = _cfg.Protocol.BaseBmsModbusPort + i * _cfg.Protocol.BmsPortStep;
                    string name = $"simBms{i + 1}";
                    int clusterCount = i < bmsCfg.Count
                        ? bmsCfg[i].ClusterCount
                        : new BmsDeviceConfig().ClusterCount;
                    var server  = new ModbusSimServer("bms_bank.csv", port, name, clusterCount, _dataExchange);
                    store.Register(name, server);
                    server.Start();
                    SimServer.serverListenInfo[name] = $"Modbus TCP 端口 {port}";
                    _servers.Add(server);
                }

                // PCS (EMU) Modbus 服务
                int unitCount = Math.Max(1, _cfg.Devices?.Count ?? 1);
                for (int u = 0; u < unitCount; u++)
                {
                    int port = _cfg.Protocol.BaseEmuModbusPort + u * _cfg.Protocol.EmuPortStep;
                    string name = $"simEmu{u + 1}";
                    var pcs = new ModbusSimServer("emu.csv", port, name, dataExchangeOptions: _dataExchange);
                    store.Register(name, pcs);
                    pcs.Start();
                    SimServer.serverListenInfo[name] = $"Modbus TCP 端口 {port}";
                    _servers.Add(pcs);
                }

                // 电表 Modbus 服务
                var em = new ModbusSimServer("em.csv", _cfg.Protocol.EmModbusPort, "simEm");
                store.Register("simEm", em);
                em.Start();
                SimServer.serverListenInfo["simEm"] = $"Modbus TCP 端口 {_cfg.Protocol.EmModbusPort}";
                _servers.Add(em);
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var server in _servers)
            {
                try { server.Stop(); }
                catch (Exception ex) { Log.Warn("关闭 Modbus 服务时异常", ex); }
            }
            return Task.CompletedTask;
        }
    }
}
