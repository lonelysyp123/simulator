using EssSimulator.Configuration;
using EssSimulator.Core;
using EssSimulator.Display;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssSimModelApi;
using log4net;
using log4net.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EssSimulator
{
    class Program
    {
        private static void PrintProtocolCreateInfo(SimulatorConfig cfg)
        {
            Console.WriteLine("==== 协议模拟器创建 ====");
            Console.WriteLine($"电表   simEm  : Modbus TCP 端口 {cfg.Protocol.EmModbusPort}");
            int unitCount = Math.Max(1, cfg.Devices?.Count ?? 1);
            for (int i = 0; i < unitCount; i++)
            {
                int emuPort = cfg.Protocol.BaseEmuModbusPort + i * cfg.Protocol.EmuPortStep;
                Console.WriteLine($"EMU 单元{i + 1} simEmu{i + 1}: Modbus TCP 端口 {emuPort}（承载 emu{i + 1}.PcsList[0..1]）");
            }
            for (int i = 0; i < Math.Max(1, cfg.UnitCount); i++)
            {
                int port = cfg.Protocol.BaseBmsModbusPort + i * cfg.Protocol.BmsPortStep;
                Console.WriteLine($"BMS 路{i + 1} simBms{i + 1}: Modbus TCP 端口 {port}");
            }
            Console.WriteLine("=======================");
            Console.WriteLine();
        }

        static async Task Main(string[] args)
        {
            // 配置 log4net
            try
            {
                var logCfgPath = Path.Combine(AppContext.BaseDirectory, "log4net.config");
                if (File.Exists(logCfgPath))
                    XmlConfigurator.Configure(new FileInfo(logCfgPath));
                else if (File.Exists("log4net.config"))
                    XmlConfigurator.Configure(new FileInfo("log4net.config"));
            }
            catch (Exception ex)
            {
                BasicConfigurator.Configure();
                Console.WriteLine($"[log4net] 配置加载失败：{ex.Message}，已启用基础配置");
            }
            LogManager.GetLogger(typeof(Program)).Info("[Program] 应用启动");

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((ctx, services) =>
                {
                    // 绑定配置节到强类型选项
                    services.Configure<SimulatorConfig>(ctx.Configuration.GetSection(SimulatorConfig.Section));
                    services.Configure<PcsPhysicalConfig>(ctx.Configuration.GetSection(PcsPhysicalConfig.Section));
                    services.Configure<TransformerConfig>(ctx.Configuration.GetSection(TransformerConfig.Section));
                    services.Configure<PcsDefaultConfig>(ctx.Configuration.GetSection(PcsDefaultConfig.Section));
                    services.Configure<LoadConfig>(ctx.Configuration.GetSection(LoadConfig.Section));

                    // 核心仿真模型（单例 + 托管服务，由 Host 管理生命周期和仿真主循环）
                    services.AddSingleton<EnergyStorageSystem>(sp =>
                    {
                        var simCfg   = sp.GetRequiredService<IOptions<SimulatorConfig>>().Value;
                        var pcsCfg   = sp.GetRequiredService<IOptions<PcsPhysicalConfig>>().Value;
                        var transCfg = sp.GetRequiredService<IOptions<TransformerConfig>>().Value;
                        var loadCfg  = sp.GetRequiredService<IOptions<LoadConfig>>().Value;
                        var ess = new EnergyStorageSystem(simCfg, pcsCfg, transCfg, loadCfg);
                        SimulatorHost.Instance.Register("ess", ess);
                        return ess;
                    });
                    services.AddHostedService(sp => sp.GetRequiredService<EnergyStorageSystem>());

                    // 数据服务（BackgroundService）
                    services.AddSingleton<BmsDataService>(sp =>
                    {
                        var cfg = sp.GetRequiredService<IOptions<SimulatorConfig>>().Value;
                        return new BmsDataService(cfg);
                    });
                    services.AddHostedService(sp => sp.GetRequiredService<BmsDataService>());

                    services.AddSingleton<PcsDataServer>();
                    services.AddHostedService(sp => sp.GetRequiredService<PcsDataServer>());

                    services.AddSingleton<EmDataService>();
                    services.AddHostedService(sp => sp.GetRequiredService<EmDataService>());

                    // Modbus 协议服务（托管服务）
                    services.AddHostedService<ModbusHostedService>();

                    // GUI（可选，不影响服务启动）
                    services.AddSingleton<GuiMain>();
                })
                .Build();

            var simCfg = host.Services.GetRequiredService<IOptions<SimulatorConfig>>().Value;
            // 先打印协议创建信息，再启动 GUI 覆盖输出，避免混杂
            PrintProtocolCreateInfo(simCfg);

            // 先启动 Host（后台服务会创建并启动 Modbus 从站等）
            await host.StartAsync();

            // 启动控制台 GUI（若未禁用）。GUI 内部会 Clear 覆盖上面的打印。
            if (!simCfg.Runtime.NoGui)
                host.Services.GetRequiredService<GuiMain>();

            await host.WaitForShutdownAsync();
        }
    }
}
