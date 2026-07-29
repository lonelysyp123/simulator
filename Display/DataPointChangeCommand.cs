using EssSimulator.Core;
using System;
using System.Linq;

namespace EssSimulator.Display
{
    public class DataPointChangeCommand : ICommand
    {
        public string Name => "dpc";
        public string Description => "数据点变位 (set/get)";

        public CommandResult Execute(string[] args)
        {
            if (args.Length == 1 && args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
                return CommandResult.Ok(PrintHelp());

            if (args.Length < 2)
                return CommandResult.Ok(PrintHelp());

            if (!TryExecuteDpcOperation(args, out var message))
                return CommandResult.Fail(message);

            return CommandResult.Ok(message);
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
                bool anyEmu = false;
                try { anyEmu = SimulatorHost.Instance.Contains("simEmu1"); } catch { /* ignore */ }
                message = anyEmu
                    ? $"找不到 Modbus 设备 `{dpcDeviceName}`"
                    : $"找不到对应的 Modbus 设备 `{dpcDeviceName}`（仿真尚未就绪：点表未加载或 Modbus 从站启动失败。请执行 ./scripts/sync-pointmaps-to-root.sh 后重启）";
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
                    message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {dpcDeviceName}.{dpcDeviceDataPoint} 控制点写入寄存器原始值 {val}（工程值=原始值/Scale，经控制管道解析）";
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

        private static string PrintHelp()
        {
            return new[]
            {
                "用法: dpc <dpcname> <operation> <data>",
                "  dpcname: <device>.<datapoint> 例如 pcs1.ActivePower",
                "  operation: set / get",
                "  data: set 时填写值，get 时可省略",
                "  控制点 set：写入 Modbus 原始寄存器值（与 mbpoll 一致）；工程值 = 原始值 / CSV 的 Scale",
                "  遥测点 set：若 ModelSim 不为 0，将在下一个轮询周期被覆盖",
                "示例:",
                "  dpc simEmu1.yt0 set 1000   # yt0 Scale=10 → 100 kW 有功设定",
                "  dpc simEmu1.yx3 set 0      # PCS1 停机",
                "  dpc simBms1.yc11 get"
            }.JoinLines();
        }
    }
}
