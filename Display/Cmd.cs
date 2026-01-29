using IEC61850_simulatorServer2.EssDeviceSimModel;
using IEC61850_simulatorServer2.EssSimModelApi;
using IEC61850_simulatorServer2.EssSimModelApi.BatteryManagementSystem;
using IEC61850_simulatorServer2.EssSimModelApi.EnergyManagementSystem.EnergyManagementSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IEC61850_simulatorServer2.Display
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

            var dpcname = args[0];
            var op = args[1].ToLower();
            var opdata = args.Length > 2 ? args[2] : string.Empty;

            var dpcnameParts = dpcname.Split('.');
            if (dpcnameParts.Length != 2)
            {
                Console.WriteLine("dpcname 格式错误，应为 <device>.<datapoint>");
                return;
            }

            var dpcDeviceName = dpcnameParts[0];
            var dpcDeviceDataPoint = dpcnameParts[1];

            var objectsCollect = ObjectsCollect.Instance;
            var obj = objectsCollect.GetObjByName(dpcDeviceName);
            ModbusSimServer? simServer = obj as ModbusSimServer;
            if (simServer == null)
            {
                Console.WriteLine("找不到对应的设备模型");
                return;
            }

            if (!simServer.dataMaps.Any(m => m.ParamName == dpcDeviceDataPoint))
            {
                Console.WriteLine("指定设备找不到对应数据点");
                return;
            }

            if (op == "set")
            {
                simServer.SetDataStoreByMesurePointName(dpcDeviceDataPoint, opdata);
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {dpcDeviceName}.{dpcDeviceDataPoint} 设置值为 {opdata} (若 ModelSim 不为 0 将在下一个轮询周期被覆盖)");
            }
            else if (op == "get")
            {
                object? result = simServer.GetDataObjectByMesurePointName(dpcDeviceDataPoint);
                if (result == null)
                {
                    Console.WriteLine("获取为空，可能原因: 1) 点名错误 2) 点不支持读取");
                }
                else
                {
                    Console.WriteLine($"设备:{dpcDeviceName} 数据点:{dpcDeviceDataPoint} val:{result}");
                }
            }
            else
            {
                Console.WriteLine("不支持的操作，请使用 set 或 get，或 dpc help 查看用法");
            }
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

    public class EssCommand(): ICommand
    {
        public string Name => "esscmd";
        public string Description => "Ess 操控命令 (set)";

        public void Execute(string[] args)
        {
            if (args.Length == 1 && args[0] == "help")
            {
                Console.WriteLine("使用示例1: esscmd setPcsX slope 0.5(kw/ms)");
                Console.WriteLine("使用示例2: esscmd setPcsX interval 100(ms)");
                Console.WriteLine("使用示例3: esscmd setPcsX delay 100(ms)");
                // 可用于负载有功无功设置
                Console.WriteLine("使用示例4: esscmd setLoad activePower 500(kW)");
                Console.WriteLine("使用示例5: esscmd setLoad reactivePower 200(kVar)");
                return;
            }

            // 判断指令是否符合使用示例，如果不符合则提示用户使用help查看用法
            if (args.Length != 3)
            {
                // 建议用户查看help
                Console.WriteLine("指令参数不正确，请使用 esscmd help 查看用法");
                return;
            }

            var objectsCollect = ObjectsCollect.Instance;
            EnergyStorageSystem ess = (EnergyStorageSystem)objectsCollect.GetObjByName("ess"); ;
            if (ess == null)
            {
                Console.WriteLine("找不到对应的模型，请确认ess模型已创建");
                return;
            }

            // 如果 args[0] 不等于 setPcs1 或 setPcs2，则提示用户指令不支持
            if (args[0] != "setPcs1" && args[0] != "setPcs2" && args[0] != "setLoad")
            {
                Console.WriteLine("当前设备不支持指令，请使用 esscmd help 查看用法");
                return;
            }

            // 如果 args[1] 不等于 slope、interval 或 delay，则提示用户指令不支持
            if (args[1] != "slope" && args[1] != "interval" && args[1] != "delay" && args[1] != "activePower" && args[1] != "reactivePower")
            {
                Console.WriteLine("当前操作不支持，请使用 esscmd help 查看用法");
                return;
            }

            // 如果 args[2] 不能转换为 double，则提示用户输入有效数字
            if (!double.TryParse(args[2], out var num))
            {
                Console.WriteLine("请输入有效的数字，请使用 esscmd help 查看用法");
                return;
            }

            if (args[0] == "setPcs1")
            {
                ess._pcs1.SetPcsCharacteristic(args[1], num);
            }
            else if(args[0] == "setPcs2")
            {
                ess._pcs2.SetPcsCharacteristic(args[1], num);
            }
            else if(args[0] == "setLoad")
            {
                ess._loadSimulator.SetLoadCharacteristic(args[1], num);
            }
            /*double result = args[0] switch
            {
                "add" => num1 + num2,
                "sub" => num1 - num2,
                "mul" => num1 * num2,
                "div" when num2 != 0 => num1 / num2,
                "div" => throw new ArgumentException("除数不能为零"),
                _ => throw new ArgumentException("未知操作")
            };*/

            Console.WriteLine($"执行成功");
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

            var objectsCollect = ObjectsCollect.Instance;
            EnergyStorageSystem ess = (EnergyStorageSystem)objectsCollect.GetObjByName("ess"); ;
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
                Console.WriteLine("当前可用命令:esscmd, exit, dpc");
            }
        }
    }

}
