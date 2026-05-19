using EssSimulator.EssDeviceSimModel;
using EssSimulator.Core;
using EssSimulator.EssSimModelApi;
using EssSimulator.EssSimModelApi.BatteryManagementSystem;
using EssSimulator.EssSimModelApi.EnergyManagementSystem.EnergyManagementSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EssSimulator.Display
{
    public interface ICommand
    {
        string Name { get; }
        string Description { get; }
        void Execute(string[] args);
    }

    public class HelpCommand : ICommand
    {
        public string Name => "help";
        public string Description => "显示所有可用命令";

        private readonly IEnumerable<ICommand> _commands;

        public HelpCommand(IEnumerable<ICommand> commands)
        {
            _commands = commands;
        }

        public void Execute(string[] args)
        {
            Console.WriteLine("可用命令:");
            foreach (var cmd in _commands)
            {
                Console.WriteLine($"  {cmd.Name.PadRight(10)} - {cmd.Description}");
            }
        }
    }
    public class ExitCommand : ICommand
    {
        public string Name => "exit";
        public string Description => "退出程序";

        public void Execute(string[] args)
        {
            Environment.Exit(0);
        }
    }

    public class MathCommand : ICommand
    {
        public string Name => "math";
        public string Description => "执行数学运算 (add/sub/mul/div)";

        public void Execute(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("用法: math <operation> <num1> <num2>");
                Console.WriteLine("可用操作: add, sub, mul, div");
                return;
            }

            if (!double.TryParse(args[1], out var num1) || !double.TryParse(args[2], out var num2))
            {
                Console.WriteLine("请输入有效的数字");
                return;
            }

            double result = args[0] switch
            {
                "add" => num1 + num2,
                "sub" => num1 - num2,
                "mul" => num1 * num2,
                "div" when num2 != 0 => num1 / num2,
                "div" => throw new ArgumentException("除数不能为零"),
                _ => throw new ArgumentException("未知操作")
            };

            Console.WriteLine($"结果: {result}");
        }
    }

    public class DataPointChangeCommand() : ICommand
    {
        public string Name => "dpc";
        public string Description => "数据点变位 (set/get)";

        public void Execute(string[] args)
        {
            if (args.Length == 1 && args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                PrintHelp();
                return;
            }

            if (args.Length < 2)
            {
                PrintHelp();
                return;
            }

            if (!TryExecuteDpcOperation(args, out var message))
            {
                Console.WriteLine(message);
                return;
            }

            Console.WriteLine(message);
        }

        public static bool TryExecuteDpcOperation(string[] args, out string message)
        {
            message = string.Empty;
            if (args.Length < 2)
            {
                message = "dpc 参数不足，请使用 dpc help 查看用法";
                return false;
            }

            var dpcname = args[0];
            var op = args[1].ToLowerInvariant();
            var opdata = args.Length > 2 ? string.Join(' ', args.Skip(2)) : string.Empty;

            var dpcnameParts = dpcname.Split('.');
            if (dpcnameParts.Length != 2)
            {
                message = "dpcname 格式错误，应为 <device>.<datapoint>";
                return false;
            }

            var dpcDeviceName = dpcnameParts[0];
            var dpcDeviceDataPoint = dpcnameParts[1];

            var objectsCollect = SimulatorHost.Instance;
            var obj = objectsCollect.Get<object>(dpcDeviceName);
            ModbusSimServer? simServer = obj as ModbusSimServer;
            if (simServer == null)
            {
                message = "找不到对应的设备模型";
                return false;
            }

            bool isControlPoint = simServer.ControlMaps.Any(m => m.ParamName == dpcDeviceDataPoint);
            bool isDataPoint    = simServer.DataMaps.Any(m => m.ParamName == dpcDeviceDataPoint);
            if (!isControlPoint && !isDataPoint)
            {
                message = "指定设备找不到对应数据点";
                return false;
            }

            if (op == "set")
            {
                if (string.IsNullOrWhiteSpace(opdata))
                {
                    message = "set 操作缺少参数值";
                    return false;
                }

                if (isControlPoint)
                {
                    object val = opdata;
                    if (bool.TryParse(opdata, out var bv)) val = bv;
                    else if (int.TryParse(opdata, out var iv)) val = iv;
                    simServer.SetDataObjectByMesurePointName(dpcDeviceDataPoint, val);
                    message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {dpcDeviceName}.{dpcDeviceDataPoint} 控制点设置为 {val}";
                }
                else
                {
                    simServer.SetDataStoreByMesurePointName(dpcDeviceDataPoint, opdata);
                    message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {dpcDeviceName}.{dpcDeviceDataPoint} 设置值为 {opdata} (若 ModelSim 不为 0 将在下一个轮询周期被覆盖)";
                }
                return true;
            }

            if (op == "get")
            {
                object? result = simServer.GetDataObjectByMesurePointName(dpcDeviceDataPoint);
                if (result == null)
                {
                    message = "获取为空，可能原因: 1) 点名错误 2) 点不支持读取";
                }
                else
                {
                    message = $"设备:{dpcDeviceName} 数据点:{dpcDeviceDataPoint} val:{result}";
                }

                return true;
            }

            message = "不支持的操作，请使用 set 或 get，或 dpc help 查看用法";
            return false;
        }

        private void PrintHelp()
        {
            Console.WriteLine("用法: dpc <dpcname> <operation> <data>");
            Console.WriteLine("  dpcname: <device>.<datapoint> 例如 pcs1.ActivePower");
            Console.WriteLine("  operation: set / get");
            Console.WriteLine("  data: set 时填写值，get 时可省略");
            Console.WriteLine("  若 ModelSim 不为 0 ，set指令将在下一个轮询周期被覆盖");
            Console.WriteLine("示例:");
            Console.WriteLine("  dpc ess.yc1 get");
            Console.WriteLine("  dpc ess.yc1 set 123.45");
        }
    }

    public class DpcTestSuite
    {
        public List<DpcTestCase> Tests { get; set; } = [];
    }

    public class DpcTestCase
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Steps { get; set; } = [];
        public string Script { get; set; } = string.Empty;
    }

    public class DpcAutoTestCommand() : ICommand
    {
        public string Name => "dpctest";
        public string Description => "执行自动化 DPC 测试";

        public void Execute(string[] args)
        {
            if (args.Length < 1 || args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                PrintHelp();
                return;
            }

            if (!TryLoadSuite(out var suite, out var loadError))
            {
                Console.WriteLine(loadError);
                return;
            }

            if (args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                PrintTestList(suite);
                return;
            }

            var testName = args[0];
            var testCase = suite.Tests.FirstOrDefault(t => t.Name.Equals(testName, StringComparison.OrdinalIgnoreCase));
            if (testCase == null)
            {
                Console.WriteLine($"未找到自动化测试: {testName}");
                return;
            }

            var steps = BuildSteps(testCase);
            if (steps.Count == 0)
            {
                Console.WriteLine($"测试 [{testCase.Name}] 没有可执行步骤");
                return;
            }

            Console.WriteLine($"开始执行测试 [{testCase.Name}]，步骤数: {steps.Count}");
            if (!string.IsNullOrWhiteSpace(testCase.Description))
                Console.WriteLine($"说明: {testCase.Description}");

            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i].Trim();
                if (string.IsNullOrWhiteSpace(step))
                    continue;

                Console.WriteLine($"[{i + 1}/{steps.Count}] {step}");

                if (TryParseSleepSeconds(step, out var sleepMilliseconds))
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(sleepMilliseconds));
                    Console.WriteLine($"等待完成: {sleepMilliseconds} 毫秒");
                    continue;
                }

                if (step.StartsWith("dpc ", StringComparison.OrdinalIgnoreCase))
                {
                    var dpcArgs = step.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();
                    if (!DataPointChangeCommand.TryExecuteDpcOperation(dpcArgs, out var dpcMessage))
                    {
                        Console.WriteLine($"测试 [{testCase.Name}] 执行失败: {dpcMessage}");
                        return;
                    }

                    Console.WriteLine(dpcMessage);
                    continue;
                }

                Console.WriteLine($"测试 [{testCase.Name}] 执行失败: 不支持的步骤 [{step}]，当前仅支持 dpc 和 sleep");
                return;
            }

            Console.WriteLine($"测试 [{testCase.Name}] 执行完成");
        }

        private static List<string> BuildSteps(DpcTestCase testCase)
        {
            if (testCase.Steps.Count > 0)
                return testCase.Steps.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();

            if (!string.IsNullOrWhiteSpace(testCase.Script))
            {
                return testCase.Script
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }

            return [];
        }

        private static bool TryParseSleepSeconds(string step, out int seconds)
        {
            seconds = 0;
            if (step.StartsWith("sleep(", StringComparison.OrdinalIgnoreCase) && step.EndsWith(")"))
            {
                var body = step["sleep(".Length..^1].Trim();
                return int.TryParse(body, out seconds) && seconds >= 0;
            }

            if (step.StartsWith("sleep ", StringComparison.OrdinalIgnoreCase))
            {
                var body = step["sleep ".Length..].Trim();
                return int.TryParse(body, out seconds) && seconds >= 0;
            }

            return false;
        }

        private static void PrintTestList(DpcTestSuite suite)
        {
            if (suite.Tests.Count == 0)
            {
                Console.WriteLine("autotest.json 中没有定义任何测试");
                return;
            }

            Console.WriteLine("可用自动化测试:");
            foreach (var test in suite.Tests)
            {
                var desc = string.IsNullOrWhiteSpace(test.Description) ? "" : $" - {test.Description}";
                Console.WriteLine($"  {test.Name}{desc}");
            }
        }

        private static bool TryLoadSuite(out DpcTestSuite suite, out string error)
        {
            suite = new DpcTestSuite();
            error = string.Empty;

            var candidatePaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "autotest.json"),
                Path.Combine(AppContext.BaseDirectory, "autotest.json")
            };

            var configPath = candidatePaths.FirstOrDefault(File.Exists);
            if (string.IsNullOrWhiteSpace(configPath))
            {
                error = "未找到 autotest.json，请在程序目录或启动目录下创建该文件";
                return false;
            }

            try
            {
                var json = File.ReadAllText(configPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };
                suite = JsonSerializer.Deserialize<DpcTestSuite>(json, options) ?? new DpcTestSuite();
                return true;
            }
            catch (Exception ex)
            {
                error = $"读取 autotest.json 失败: {ex.Message}";
                return false;
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("用法: dpctest <testName>");
            Console.WriteLine("  dpctest list         列出 autotest.json 中的测试名称");
            Console.WriteLine("  dpctest <testName>   执行指定测试");
            Console.WriteLine("  dpctest help         查看帮助");
            Console.WriteLine("步骤语法支持:");
            Console.WriteLine("  dpc <device.point> set <value>");
            Console.WriteLine("  dpc <device.point> get");
            Console.WriteLine("  sleep(10) 或 sleep 10");
        }
    }

    public class EssCommand(): ICommand
    {
        public string Name => "esscmd";
        public string Description => "Ess 操控命令（负载设置 / 协议链路开关）";

        public void Execute(string[] args)
        {
            if (args.Length == 0 || (args.Length == 1 && args[0].Equals("help", StringComparison.OrdinalIgnoreCase)))
            {
                PrintHelp();
                return;
            }

            var verb = args[0];
            if (verb.Equals("setLoad", StringComparison.OrdinalIgnoreCase))
            {
                ExecuteSetLoad(args);
                return;
            }

            if (verb.Equals("link", StringComparison.OrdinalIgnoreCase))
            {
                ExecuteLink(args);
                return;
            }

            Console.WriteLine("未知子命令，请使用 esscmd help 查看用法");
        }

        private static void PrintHelp()
        {
            Console.WriteLine("esscmd 子命令:");
            Console.WriteLine("  setLoad activePower <kW>       // 手动设定负载有功（-用电, +向电网送电）");
            Console.WriteLine("  setLoad reactivePower <kvar>   // 手动设定负载无功");
            Console.WriteLine("  link pcsN on|off               // 开启/关闭第 N 路 PCS 所属 EMU 单元的 Modbus 对外服务");
            Console.WriteLine("  link bmsN on|off               // 开启/关闭第 N 路 BMS 的 Modbus 对外服务");
            Console.WriteLine("  link status [pcsN|bmsN]        // 查看协议链路状态（省略目标则列出全部）");
            Console.WriteLine();
            Console.WriteLine("说明:");
            Console.WriteLine("  - off：关闭 TCP 监听并停止寄存器同步，外部 mbpoll/主站无法连接");
            Console.WriteLine("  - on：重新绑定端口并恢复数据同步");
            Console.WriteLine("  - 同一储能单元内 pcs(2n-1)/pcs(2n) 共用 simEmu{n}，关闭任一路会影响该单元两路 PCS");
        }

        private static void ExecuteSetLoad(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("用法: esscmd setLoad activePower|reactivePower <数值>");
                return;
            }

            if (args[1] != "activePower" && args[1] != "reactivePower")
            {
                Console.WriteLine("setLoad 仅支持 activePower 或 reactivePower");
                return;
            }

            if (!double.TryParse(args[2], out var num))
            {
                Console.WriteLine("请输入有效的数字");
                return;
            }

            var ess = SimulatorHost.Instance.Get<EnergyStorageSystem>("ess");
            if (ess == null)
            {
                Console.WriteLine("找不到 ess 模型，请确认仿真已启动");
                return;
            }

            ess._loadSimulator.SetLoadCharacteristic(args[1], num);
            Console.WriteLine($"执行成功: 负载 {args[1]} = {num}");
        }

        private static void ExecuteLink(string[] args)
        {
            if (args.Length == 2 && args[1].Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                PrintAllLinkStatus();
                return;
            }

            if (args.Length == 3 && args[1].Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryResolveProtocolServer(args[2], out var server, out var serverName, out var detail))
                {
                    Console.WriteLine(detail);
                    return;
                }
                Console.WriteLine(FormatLinkStatus(args[2], serverName, server!, detail));
                return;
            }

            if (args.Length != 3)
            {
                Console.WriteLine("用法: esscmd link pcsN|bmsN on|off");
                Console.WriteLine("      esscmd link status [pcsN|bmsN]");
                return;
            }

            if (!TryResolveProtocolServer(args[1], out var simServer, out var resolvedName, out var resolveMessage))
            {
                Console.WriteLine(resolveMessage);
                return;
            }

            if (!TryParseLinkState(args[2], out var enable, out var stateMessage))
            {
                Console.WriteLine(stateMessage);
                return;
            }

            bool ok = simServer!.SetOnline(enable);
            if (!ok)
            {
                Console.WriteLine($"操作失败: {resolvedName} 未能{(enable ? "恢复" : "关闭")} Modbus 服务");
                return;
            }

            var listenInfo = SimServer.serverListenInfo.TryGetValue(resolvedName, out var info) ? info : resolvedName;
            Console.WriteLine(enable
                ? $"执行成功: {args[1]} -> {resolvedName} 已上线（{listenInfo}）{resolveMessage}"
                : $"执行成功: {args[1]} -> {resolvedName} 已离线，外部无法连接{resolveMessage}");
        }

        private static bool TryParseLinkState(string raw, out bool enable, out string message)
        {
            enable = false;
            message = string.Empty;
            switch (raw.ToLowerInvariant())
            {
                case "on":
                case "online":
                case "connect":
                    enable = true;
                    return true;
                case "off":
                case "offline":
                case "disconnect":
                    enable = false;
                    return true;
                default:
                    message = "链路状态仅支持 on/off（或 online/offline、connect/disconnect）";
                    return false;
            }
        }

        private static bool TryResolveProtocolServer(
            string target,
            out ModbusSimServer? server,
            out string serverName,
            out string detail)
        {
            server = null;
            serverName = string.Empty;
            detail = string.Empty;
            if (string.IsNullOrWhiteSpace(target))
            {
                detail = "请指定 pcsN 或 bmsN";
                return false;
            }

            var store = SimulatorHost.Instance;
            if (target.StartsWith("pcs", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(target.AsSpan(3), out int pcsIdx) &&
                pcsIdx >= 1)
            {
                int emuUnit = (pcsIdx - 1) / 2 + 1;
                int pcsPeer = (emuUnit - 1) * 2 + 1;
                serverName = $"simEmu{emuUnit}";
                server = store.Get<ModbusSimServer>(serverName);
                if (server == null)
                {
                    detail = $"找不到 {serverName}（pcs{pcsIdx} 超出当前配置范围）";
                    return false;
                }

                detail = $"（emu 单元 {emuUnit}，影响 pcs{pcsPeer} 与 pcs{pcsPeer + 1}）";
                return true;
            }

            if (target.StartsWith("bms", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(target.AsSpan(3), out int bmsIdx) &&
                bmsIdx >= 1)
            {
                serverName = $"simBms{bmsIdx}";
                server = store.Get<ModbusSimServer>(serverName);
                if (server == null)
                {
                    detail = $"找不到 {serverName}（bms{bmsIdx} 超出当前配置范围）";
                    return false;
                }
                return true;
            }

            detail = "目标格式应为 pcsN 或 bmsN，例如 pcs1、bms3";
            return false;
        }

        private static void PrintAllLinkStatus()
        {
            var store = SimulatorHost.Instance;
            Console.WriteLine("协议链路状态:");

            int bms = 1;
            while (store.Contains($"simBms{bms}"))
            {
                var server = store.Get<ModbusSimServer>($"simBms{bms}");
                Console.WriteLine(FormatLinkStatus($"bms{bms}", $"simBms{bms}", server!, string.Empty));
                bms++;
            }

            int emu = 1;
            while (store.Contains($"simEmu{emu}"))
            {
                var server = store.Get<ModbusSimServer>($"simEmu{emu}");
                int pcsA = (emu - 1) * 2 + 1;
                int pcsB = pcsA + 1;
                Console.WriteLine(FormatLinkStatus($"pcs{pcsA}/pcs{pcsB}", $"simEmu{emu}", server!, $"emu 单元 {emu}"));
                emu++;
            }
        }

        private static string FormatLinkStatus(string label, string serverName, ModbusSimServer server, string extra)
        {
            var listenInfo = SimServer.serverListenInfo.TryGetValue(serverName, out var info) ? info : serverName;
            var state = server.IsOnline ? "在线" : "离线";
            return string.IsNullOrWhiteSpace(extra)
                ? $"  {label,-12} {serverName,-10} {state,-4} {listenInfo}"
                : $"  {label,-14} {serverName,-10} {state,-4} {listenInfo}  ({extra})";
        }
    }

    public class BreakerCommand() : ICommand
    {
        public string Name => "breaker";
        public string Description => "breaker 操控命令 (set)";

        public void Execute(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("用法: breaker <operation> <state>");
                return;
            }

            if (!bool.TryParse(args[1], out var flag))
            {
                Console.WriteLine("请输入有效的数字");
                return;
            }

            var objectsCollect = SimulatorHost.Instance;
            EnergyStorageSystem ess = objectsCollect.Get<EnergyStorageSystem>("ess"); ;
            if (ess == null)
            {
                Console.WriteLine("找不到对应的模型");
                return;
            }

            if (args[0] == "set")
            {
                ess._breaker.IsClosed = flag;
            }
            else
            {
                Console.WriteLine("操作命令参数不正确");
                return;
            }
            Console.WriteLine($"执行成功");
        }
    }
    public class CommandProcessor
    {
        private readonly Dictionary<string, ICommand> _commands;

        public CommandProcessor(IEnumerable<ICommand> commands)
        {
            _commands = commands.ToDictionary(c => c.Name.ToLower());
        }

        public void ProcessCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var commandName = parts[0].ToLower();
            var args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

            if (_commands.TryGetValue(commandName, out var command))
            {
                try
                {
                    command.Execute(args);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"执行命令时出错: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"未知命令: {commandName}");
                Console.WriteLine("当前可用命令: esscmd, breaker, dpc, dpctest, exit");
            }
        }
    }

}
