using EssSimulator.Core;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssSimModelApi.Bms;
using System;

namespace EssSimulator.Display
{
public class EssCommand(): ICommand
{
    public string Name => "esscmd";
    public string Description => "Ess 操控命令（负载 / 协议链路 / BMS 并离网）";

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

        if (verb.StartsWith("setbms", StringComparison.OrdinalIgnoreCase))
        {
            ExecuteSetBmsPower(args);
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
        Console.WriteLine("  setbmsN power on|off           // BMS 物理并网/离网（PCS↔BMS 直流链路）");
        Console.WriteLine();
        Console.WriteLine("说明:");
        Console.WriteLine("  - link off：关闭 TCP 监听，模拟通信中断；与 setbms power 无关");
        Console.WriteLine("  - setbmsN power on：触发并网，GridConnectStatus→2，IsPcsLinked=true");
        Console.WriteLine("  - setbmsN power off：断开 PCS↔BMS 链路，GridConnectStatus→0");
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

        ess.SetLoadCharacteristic(args[1], num);
        Console.WriteLine($"执行成功: 负载 {args[1]} = {num}");
    }

    private static void ExecuteSetBmsPower(string[] args)
    {
        if (args.Length != 3 ||
            !args[1].Equals("power", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("用法: esscmd setbmsN power on|off");
            Console.WriteLine("示例: esscmd setbms1 power on");
            return;
        }

        if (!TryParseSetBmsIndex(args[0], out int bms1Based, out var parseMessage))
        {
            Console.WriteLine(parseMessage);
            return;
        }

        if (!TryParseLinkState(args[2], out bool connect, out var stateMessage))
        {
            Console.WriteLine(stateMessage);
            return;
        }

        if (!SimulatorHost.Instance.Contains($"bms{bms1Based}"))
        {
            Console.WriteLine($"找不到 bms{bms1Based}（超出当前配置范围）");
            return;
        }

        if (!BmsLinkEngine.TrySetGridPower(bms1Based - 1, connect, out var result))
        {
            Console.WriteLine($"操作失败: {result}");
            return;
        }

        SimulatorHost.Instance.Get<ModbusSimServer>($"simBms{bms1Based}")?.InvalidateDataShadow("yc0");
        SimulatorHost.Instance.Get<ModbusSimServer>($"simBms{bms1Based}")?.InvalidateDataShadow("yc45");
        Console.WriteLine($"执行成功: bms{bms1Based} {(connect ? "并网" : "离网")} — {result}");
    }

    private static bool TryParseSetBmsIndex(string verb, out int bms1Based, out string message)
    {
        bms1Based = 0;
        message = string.Empty;
        if (!verb.StartsWith("setbms", StringComparison.OrdinalIgnoreCase))
        {
            message = "子命令格式应为 setbmsN，例如 setbms1";
            return false;
        }

        if (!int.TryParse(verb.AsSpan(6), out bms1Based) || bms1Based < 1)
        {
            message = "子命令格式应为 setbmsN，例如 setbms1";
            return false;
        }

        return true;
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
}
