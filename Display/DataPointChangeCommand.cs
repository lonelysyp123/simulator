using EssSimulator.Core;
using System;
using System.Linq;

namespace EssSimulator.Display
{
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
        IModbusRegisterServer? simServer = obj as IModbusRegisterServer;
        if (simServer == null)
        {
            message = "找不到对应的 Modbus 设备";
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
}
