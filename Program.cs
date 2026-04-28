using EssSimulator.Configuration;
using EssSimulator.Core;
using EssSimulator.Display;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssSimModelApi;
using log4net;
using log4net.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace EssSimulator
{
    class Program
    {
        private static Stream? LoadJsonWithCommentsAsStream(string path, bool optional)
        {
            if (!File.Exists(path))
            {
                if (optional) return null;
                throw new FileNotFoundException($"未找到配置文件: {path}", path);
            }

            var json = File.ReadAllText(path, Encoding.UTF8);
            var docOptions = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            using var doc = JsonDocument.Parse(json, docOptions);
            var ms = new MemoryStream();
            using (var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true }))
            {
                doc.RootElement.WriteTo(writer);
            }
            ms.Position = 0;
            return ms;
        }

        private static bool IsSimulatorReady(int expectedServerCount)
        {
            var store = SimulatorHost.Instance;
            if (!store.Contains("ess") ||
                !store.Contains("em") ||
                !store.Contains("bms1") ||
                !store.Contains("emu1") ||
                !store.Contains("simEm") ||
                !store.Contains("simBms1") ||
                !store.Contains("simEmu1"))
            {
                return false;
            }

            if (SimServer.serverListenInfo.Count < expectedServerCount)
            {
                return false;
            }

            try
            {
                var soc = SimServer.GetExtIfVariableVal("bms1.BatteryStacks[0].SOC");
                var totalActivePower = SimServer.GetExtIfVariableVal("em.TotalActivePower");
                var pcsList = SimServer.GetExtIfVariableVal("ess._pcsList") as System.Collections.ICollection;
                return soc != null && totalActivePower != null && pcsList != null && pcsList.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private static async Task WaitForSimulatorReadyAsync(SimulatorConfig cfg, CancellationToken cancellationToken)
        {
            int expectedBmsCount = Math.Max(1, cfg.UnitCount);
            int expectedEmuCount = Math.Max(1, cfg.Devices?.Count ?? 1);
            int expectedServerCount = expectedBmsCount + expectedEmuCount + 1; // BMS + EMU + EM
            var timeout = TimeSpan.FromSeconds(60);
            var start = DateTime.UtcNow;
            var spinner = new[] { '|', '/', '-', '\\' };
            int spinnerIndex = 0;

            while ((DateTime.UtcNow - start) < timeout)
            {
                if (IsSimulatorReady(expectedServerCount))
                {
                    Console.Clear();
                    return;
                }

                var elapsedSeconds = (int)(DateTime.UtcNow - start).TotalSeconds;
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("=== 储能单元模拟器加载中 ===");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("正在初始化内部模拟器与协议服务，请稍候...");
                Console.WriteLine($"协议服务进度: {SimServer.serverListenInfo.Count}/{expectedServerCount}");
                Console.WriteLine($"加载状态: {spinner[spinnerIndex]}  已等待 {elapsedSeconds}s");
                Console.WriteLine();
                Console.WriteLine("加载完成后将自动进入可视化界面。");

                spinnerIndex = (spinnerIndex + 1) % spinner.Length;
                await Task.Delay(200, cancellationToken);
            }

            // 超时兜底：避免无限等待，给出提示后继续进入 GUI。
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("初始化等待超时，仍将进入可视化界面。");
            Console.WriteLine("若 dpc 初始读取异常，请稍后重试。");
            Console.ResetColor();
            await Task.Delay(1200, cancellationToken);
        }

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
                .ConfigureAppConfiguration((ctx, cfg) =>
                {
                    // 改为与 autotest.json 一致的读取方式：支持 JSON 注释与尾随逗号。
                    // 仍使用 IConfiguration/IOptions 绑定，避免影响其余逻辑。
                    cfg.Sources.Clear();

                    string baseDir = Directory.GetCurrentDirectory();
                    string envName = ctx.HostingEnvironment.EnvironmentName;

                    var baseStream = LoadJsonWithCommentsAsStream(Path.Combine(baseDir, "appsettings.json"), optional: false);
                    if (baseStream != null) cfg.AddJsonStream(baseStream);

                    var envStream = LoadJsonWithCommentsAsStream(Path.Combine(baseDir, $"appsettings.{envName}.json"), optional: true);
                    if (envStream != null) cfg.AddJsonStream(envStream);

                    // 保留环境变量与命令行覆盖能力
                    cfg.AddEnvironmentVariables();
                    if (args is { Length: > 0 }) cfg.AddCommandLine(args);
                })
                .ConfigureServices((ctx, services) =>
                {
                    // 绑定配置节到强类型选项
                    services.Configure<SimulatorConfig>(ctx.Configuration.GetSection(SimulatorConfig.Section));
                    services.Configure<PcsPhysicalConfig>(ctx.Configuration.GetSection(PcsPhysicalConfig.Section));
                    services.Configure<TransformerConfig>(ctx.Configuration.GetSection(TransformerConfig.Section));
                    services.Configure<UnitTransformerConfig>(ctx.Configuration.GetSection(UnitTransformerConfig.Section));
                    services.Configure<LoadConfig>(ctx.Configuration.GetSection(LoadConfig.Section));

                    // 核心仿真模型（单例 + 托管服务，由 Host 管理生命周期和仿真主循环）
                    services.AddSingleton<EnergyStorageSystem>(sp =>
                    {
                        var simCfg   = sp.GetRequiredService<IOptions<SimulatorConfig>>().Value;
                        var pcsCfg   = sp.GetRequiredService<IOptions<PcsPhysicalConfig>>().Value;
                        var transCfg = sp.GetRequiredService<IOptions<TransformerConfig>>().Value;
                        var unitTransCfg = sp.GetRequiredService<IOptions<UnitTransformerConfig>>().Value;
                        var loadCfg  = sp.GetRequiredService<IOptions<LoadConfig>>().Value;
                        var ess = new EnergyStorageSystem(simCfg, pcsCfg, transCfg, unitTransCfg, loadCfg);
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

            // 先启动 Host（后台服务会创建并启动 Modbus 从站等）
            await host.StartAsync();

            // 启动控制台 GUI（若未禁用）。GUI 内部会 Clear 覆盖上面的打印。
            if (!simCfg.Runtime.NoGui)
            {
                await WaitForSimulatorReadyAsync(simCfg, CancellationToken.None);
                host.Services.GetRequiredService<GuiMain>();
            }
            else
            {
                PrintProtocolCreateInfo(simCfg);
            }

            await host.WaitForShutdownAsync();
        }
    }
}
