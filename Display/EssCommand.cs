using EssSimulator.Core;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssSimModelApi.Bms;
using System;
using System.Collections.Generic;

namespace EssSimulator.Display
{
    public class EssCommand : ICommand
    {
        public string Name => "esscmd";
        public string Description => "Ess 操控命令（负载 / 协议链路 / BMS 并离网）";

        public CommandResult Execute(string[] args)
        {
            if (args.Length == 0 || (args.Length == 1 && args[0].Equals("help", StringComparison.OrdinalIgnoreCase)))
                return CommandResult.Ok(PrintHelp());

            var verb = args[0];
            if (verb.Equals("setLoad", StringComparison.OrdinalIgnoreCase))
                return ExecuteSetLoad(args);

            if (verb.Equals("link", StringComparison.OrdinalIgnoreCase))
                return ExecuteLink(args);

            if (verb.Equals("setGrid", StringComparison.OrdinalIgnoreCase))
                return ExecuteSetGrid(args);

            if (verb.StartsWith("setbms", StringComparison.OrdinalIgnoreCase))
                return ExecuteSetBmsPower(args);

            if (verb.StartsWith("pcs", StringComparison.OrdinalIgnoreCase))
                return ExecutePcsStartStop(args);

            return CommandResult.Fail("未知子命令，请使用 esscmd help 查看用法");
        }

        private static string PrintHelp()
        {
            return new[]
            {
                "esscmd 子命令:",
                "  setLoad activePower <kW>       // 手动设定负载有功（-用电, +向电网送电）",
                "  setLoad reactivePower <kvar>   // 手动设定负载无功",
                "  link pcsN on|off               // 开启/关闭第 N 路 PCS 所属 EMU 单元的 Modbus 对外服务",
                "  link bmsN on|off               // 开启/关闭第 N 路 BMS 的 Modbus 对外服务",
                "  link em on|off                 // 开启/关闭并网点电表 simEm 的 Modbus 对外服务",
                "  link status [pcsN|bmsN|em]     // 查看协议链路状态（省略目标则列出全部）",
                "  setGrid frequency <Hz>         // 设定仿真电网额定频率（并网时 PCS 跟网、电表 yc19）",
                "  setGrid voltage <V>            // 设定仿真电网额定线电压（如 220000）",
                "  pcsN start|stop                // PCS 启停（内部走 dpc 写 EMU yx3/yx5 控制点）",
                "  setbmsN power on|off           // BMS 物理并网/离网（PCS↔BMS 直流链路）",
                "",
                "说明:",
                "  - link off：关闭 TCP 监听，模拟通信中断；与 setbms power 无关",
                "  - setGrid：调整外部电网源；主断闭合后生效于 PCC/跟网 PCS",
                "  - setbmsN power on：触发并网，GridConnectStatus→2，IsPcsLinked=true",
                "  - setbmsN power off：断开 PCS↔BMS 链路，GridConnectStatus→0",
                "  - 同一储能单元内 pcs(2n-1)/pcs(2n) 共用 simEmu{n}，关闭任一路会影响该单元两路 PCS"
            }.JoinLines();
        }

        private static CommandResult ExecuteSetLoad(string[] args)
        {
            if (args.Length != 3)
                return CommandResult.Fail("用法: esscmd setLoad activePower|reactivePower <数值>");

            if (args[1] != "activePower" && args[1] != "reactivePower")
                return CommandResult.Fail("setLoad 仅支持 activePower 或 reactivePower");

            if (!double.TryParse(args[2], out var num))
                return CommandResult.Fail("请输入有效的数字");

            var ess = SimulatorHost.Instance.Get<EnergyStorageSystem>("ess");
            if (ess == null)
                return CommandResult.Fail("找不到 ess 模型，请确认仿真已启动");

            ess.SetLoadCharacteristic(args[1], num);
            return CommandResult.Ok($"执行成功: 负载 {args[1]} = {num}");
        }

        private static CommandResult ExecuteSetGrid(string[] args)
        {
            if (args.Length != 3)
                return CommandResult.Fail("用法: esscmd setGrid frequency|voltage <数值>\n示例: esscmd setGrid frequency 50\n      esscmd setGrid voltage 220000");

            var ess = SimulatorHost.Instance.Get<EnergyStorageSystem>("ess");
            if (ess == null)
                return CommandResult.Fail("找不到 ess 模型，请确认仿真已启动");

            if (args[1].Equals("frequency", StringComparison.OrdinalIgnoreCase))
            {
                if (!double.TryParse(args[2], out var hz))
                    return CommandResult.Fail("请输入有效的频率数值（Hz）");

                if (!ess.TrySetGridFrequency(hz, out var message))
                    return CommandResult.Fail($"操作失败: {message}");

                return CommandResult.Ok($"执行成功: {message}");
            }

            if (args[1].Equals("voltage", StringComparison.OrdinalIgnoreCase))
            {
                if (!double.TryParse(args[2], out var volts))
                    return CommandResult.Fail("请输入有效的电压数值（V）");

                if (!ess.TrySetGridVoltage(volts, out var message))
                    return CommandResult.Fail($"操作失败: {message}");

                return CommandResult.Ok($"执行成功: {message}");
            }

            return CommandResult.Fail("setGrid 仅支持 frequency 或 voltage");
        }

        private static CommandResult ExecuteSetBmsPower(string[] args)
        {
            if (args.Length != 3 || !args[1].Equals("power", StringComparison.OrdinalIgnoreCase))
                return CommandResult.Fail("用法: esscmd setbmsN power on|off\n示例: esscmd setbms1 power on");

            if (!TryParseSetBmsIndex(args[0], out int bms1Based, out var parseMessage))
                return CommandResult.Fail(parseMessage);

            if (!TryParseLinkState(args[2], out bool connect, out var stateMessage))
                return CommandResult.Fail(stateMessage);

            if (!SimulatorHost.Instance.Contains($"bms{bms1Based}"))
                return CommandResult.Fail($"找不到 bms{bms1Based}（超出当前配置范围）");

            if (!BmsLinkEngine.TrySetGridPower(bms1Based - 1, connect, out var result))
                return CommandResult.Fail($"操作失败: {result}");

            SimulatorHost.Instance.Get<ModbusSimServer>($"simBms{bms1Based}")?.InvalidateDataShadow("yc0");
            SimulatorHost.Instance.Get<ModbusSimServer>($"simBms{bms1Based}")?.InvalidateDataShadow("yc45");
            return CommandResult.Ok($"执行成功: bms{bms1Based} {(connect ? "并网" : "离网")} — {result}");
        }

        private static CommandResult ExecutePcsStartStop(string[] args)
        {
            if (args.Length != 2)
                return CommandResult.Fail("用法: esscmd pcsN start|stop\n示例: esscmd pcs1 start");

            if (!args[0].StartsWith("pcs", StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(args[0].AsSpan(3), out int pcs1Based) ||
                pcs1Based < 1)
                return CommandResult.Fail("子命令格式应为 pcsN，例如 pcs1");

            string state = args[1].ToLowerInvariant();
            bool? start = state switch
            {
                "start" or "on" or "1" or "true" => true,
                "stop" or "off" or "0" or "false" => false,
                _ => null
            };
            if (start == null)
                return CommandResult.Fail("请使用 start|stop");

            int emuUnit = (pcs1Based - 1) / 2 + 1;
            string point = pcs1Based % 2 == 1 ? "yx3" : "yx5";
            string[] dpcArgs = { $"simEmu{emuUnit}.{point}", "set", start.Value ? "1" : "0" };

            if (!DataPointChangeCommand.TryExecuteDpcOperation(dpcArgs, out var message))
                return CommandResult.Fail(message);

            return CommandResult.Ok($"执行成功: PCS{pcs1Based} {(start.Value ? "启动" : "停机")} — {message}");
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

        private static CommandResult ExecuteLink(string[] args)
        {
            if (args.Length == 2 && args[1].Equals("status", StringComparison.OrdinalIgnoreCase))
                return CommandResult.Ok("协议链路状态:", PrintAllLinkStatus());

            if (args.Length == 3 && args[1].Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryResolveProtocolServer(args[2], out var server, out var serverName, out var detail))
                    return CommandResult.Fail(detail);
                return CommandResult.Ok(FormatLinkStatus(args[2], serverName, server!, detail));
            }

            if (args.Length != 3)
                return CommandResult.Fail("用法: esscmd link pcsN|bmsN|em on|off\n      esscmd link status [pcsN|bmsN|em]");

            if (!TryResolveProtocolServer(args[1], out var simServer, out var resolvedName, out var resolveMessage))
                return CommandResult.Fail(resolveMessage);

            if (!TryParseLinkState(args[2], out var enable, out var stateMessage))
                return CommandResult.Fail(stateMessage);

            bool ok = simServer!.SetOnline(enable);
            if (!ok)
                return CommandResult.Fail($"操作失败: {resolvedName} 未能{(enable ? "恢复" : "关闭")} Modbus 服务");

            var listenInfo = SimServer.serverListenInfo.TryGetValue(resolvedName, out var info) ? info : resolvedName;
            return CommandResult.Ok(enable
                ? $"执行成功: {args[1]} -> {resolvedName} 已上线（{listenInfo}）{resolveMessage}"
                : $"执行成功: {args[1]} -> {resolvedName} 已离线，外部无法连接{resolveMessage}");
        }

        public static bool TryParseLinkState(string raw, out bool enable, out string message)
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

        public static bool TryResolveProtocolServer(
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
                detail = "请指定 pcsN、bmsN 或 em";
                return false;
            }

            var store = SimulatorHost.Instance;
            if (target.Equals("em", StringComparison.OrdinalIgnoreCase))
            {
                serverName = "simEm";
                server = store.Get<ModbusSimServer>(serverName);
                if (server == null)
                {
                    detail = "找不到 simEm（电表 Modbus 未注册，请确认仿真已启动）";
                    return false;
                }
                return true;
            }

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

            detail = "目标格式应为 pcsN、bmsN 或 em，例如 pcs1、bms3、em";
            return false;
        }

        public static List<LinkStatusDto> BuildAllLinkStatus()
        {
            var list = new List<LinkStatusDto>();
            var store = SimulatorHost.Instance;

            if (store.Contains("simEm"))
            {
                var emServer = store.Get<ModbusSimServer>("simEm");
                list.Add(BuildLinkStatusDto("em", "simEm", emServer!, "", "em"));
            }

            int bms = 1;
            while (store.Contains($"simBms{bms}"))
            {
                var server = store.Get<ModbusSimServer>($"simBms{bms}");
                list.Add(BuildLinkStatusDto($"bms{bms}", $"simBms{bms}", server!, "", $"bms{bms}"));
                bms++;
            }

            int emu = 1;
            while (store.Contains($"simEmu{emu}"))
            {
                var server = store.Get<ModbusSimServer>($"simEmu{emu}");
                int pcsA = (emu - 1) * 2 + 1;
                int pcsB = pcsA + 1;
                list.Add(BuildLinkStatusDto($"pcs{pcsA}/pcs{pcsB}", $"simEmu{emu}", server!, $"emu 单元 {emu}", $"pcs{pcsA}"));
                emu++;
            }

            return list;
        }

        private static LinkStatusDto BuildLinkStatusDto(string label, string serverName, ModbusSimServer server, string extra, string target)
        {
            var listenInfo = SimServer.serverListenInfo.TryGetValue(serverName, out var info) ? info : serverName;
            return new LinkStatusDto
            {
                Label = label,
                ServerName = serverName,
                Target = target,
                Online = server.IsOnline,
                ListenInfo = listenInfo,
                Extra = extra
            };
        }

        private static string PrintAllLinkStatus()
        {
            var list = BuildAllLinkStatus();
            var lines = new List<string>();
            foreach (var s in list)
            {
                lines.Add(string.IsNullOrWhiteSpace(s.Extra)
                    ? $"  {s.Label,-12} {s.ServerName,-10} {(s.Online ? "在线" : "离线"),-4} {s.ListenInfo}"
                    : $"  {s.Label,-14} {s.ServerName,-10} {(s.Online ? "在线" : "离线"),-4} {s.ListenInfo}  ({s.Extra})");
            }
            return lines.JoinLines();
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

    public sealed class LinkStatusDto
    {
        public string Label { get; set; } = "";
        public string ServerName { get; set; } = "";
        public string Target { get; set; } = "";
        public bool Online { get; set; }
        public string ListenInfo { get; set; } = "";
        public string Extra { get; set; } = "";
    }

    internal static class StringJoinExtensions
    {
        public static string JoinLines(this IEnumerable<string> lines) =>
            string.Join(Environment.NewLine, lines);
    }
}
