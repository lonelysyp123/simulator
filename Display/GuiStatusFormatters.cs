using EssSimulator.EssDeviceSimModel;
using log4net;

namespace EssSimulator.Display
{
    /// <summary>主接线等视图的状态文本格式化。</summary>
    internal static class GuiStatusFormatters
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(GuiStatusFormatters));

        public static string FormatBlackStartSwitchStatus(int unitIndex0, int pcsSlotInUnit0, int essPcsListIndex)
        {
            bool swOn = GuiSimDataAccess.SafeGetBool($"emu{unitIndex0 + 1}.PcsList[{pcsSlotInUnit0}].BlackStartEnabled");
            if (!swOn)
                return "关";

            bool simOn = GuiSimDataAccess.SafeGetBool($"ess._pcsList[{essPcsListIndex}]._currentState.BlackStartEnabled");
            if (!simOn)
                return "开(未生效)";

            try
            {
                var m = SimServer.GetExtIfVariableVal($"ess._pcsList[{essPcsListIndex}]._currentState.Mode");
                var g = SimServer.GetExtIfVariableVal($"ess._pcsList[{essPcsListIndex}]._currentState.GMode");
                if (m != null && g != null &&
                    Enum.TryParse<OperationMode>(m.ToString(), out var mode) &&
                    Enum.TryParse<GridMode>(g.ToString(), out var gMode) &&
                    mode == OperationMode.Normal &&
                    gMode == GridMode.Islanded)
                    return "开(运行中)";
            }
            catch (Exception ex)
            {
                Log.Debug($"FormatBlackStartSwitchStatus 读取 PCS{essPcsListIndex + 1} 模式失败", ex);
            }

            return "开";
        }

        public static string BuildBlackStartSwitchSummary(int channelStart, int channelEndExclusive)
        {
            var parts = new List<string>();
            for (int i = channelStart; i < channelEndExclusive; i++)
            {
                int u = i / 2;
                int slot = i % 2;
                string st = FormatBlackStartSwitchStatus(u, slot, i);
                parts.Add($"PCS{i + 1}:{st}");
            }
            return string.Join("  ", parts);
        }

        public static string FormatPcsControlStatus(int unitIndex0, int pcsSlotInUnit0, int essPcsListIndex)
        {
            bool cmdOn = GuiSimDataAccess.SafeGetBool($"emu{unitIndex0 + 1}.PcsList[{pcsSlotInUnit0}].pcsOnOffSwitch");
            double pSet = GuiSimDataAccess.SafeGetDouble($"emu{unitIndex0 + 1}.PcsList[{pcsSlotInUnit0}].PCSActivePowerSetting");
            string blackStartSw = FormatBlackStartSwitchStatus(unitIndex0, pcsSlotInUnit0, essPcsListIndex);
            string modeLabel = "?";
            try
            {
                var m = SimServer.GetExtIfVariableVal($"ess._pcsList[{essPcsListIndex}]._currentState.Mode");
                double pAct = GuiSimDataAccess.SafeGetDouble($"ess._pcsList[{essPcsListIndex}]._currentState.ActivePower");
                bool blackStart = GuiSimDataAccess.SafeGetBool($"ess._pcsList[{essPcsListIndex}]._currentState.BlackStartEnabled");
                ushort fault = (ushort)GuiSimDataAccess.SafeGetDouble($"ess._pcsList[{essPcsListIndex}]._currentState.FaultType");
                if (m != null && Enum.TryParse<OperationMode>(m.ToString(), out var mode))
                    modeLabel = PcsDisplayLabels.GetRunPhaseLabel(mode, pAct, blackStart, fault);
            }
            catch (Exception ex)
            {
                Log.Debug($"FormatPcsControlStatus 读取 PCS{essPcsListIndex + 1} 模式失败", ex);
            }

            return $"启停控制:{(cmdOn ? "开" : "停")} 黑启动:{blackStartSw} P设定:{pSet:0}kW 设备状态:{modeLabel}";
        }

        public static string FormatGridConnectStatus(int bmsIndex0)
        {
            int status = (int)GuiSimDataAccess.SafeGetDouble($"bms{bmsIndex0 + 1}.BatteryStacks[0].GridConnectStatus");
            string label = status switch
            {
                0 => "未开始",
                1 => "进行中",
                2 => "成功",
                3 => "失败",
                _ => $"未知({status})"
            };
            bool linked = GuiSimDataAccess.SafeGetBool($"bms{bmsIndex0 + 1}.BatteryStacks[0].IsPcsLinked");
            return $"{label}({(linked ? "已关联" : "未关联")})";
        }

        public static string FormatBlackStartModeStatus(int bmsIndex0)
        {
            int status = (int)GuiSimDataAccess.SafeGetDouble($"bms{bmsIndex0 + 1}.BatteryStacks[0].BlackStartStatus");
            int success = (int)GuiSimDataAccess.SafeGetDouble($"bms{bmsIndex0 + 1}.BatteryStacks[0].BlackStartEnterSuccess");
            string statusLabel = status switch
            {
                0 => "空闲",
                3 => "已进入",
                4 => "进入失败",
                5 => "已退出",
                _ => $"状态{status}"
            };
            return $"{statusLabel} 成功:{(success == 1 ? "是" : "否")}";
        }

        public static string FormatBatteryCompartmentLine(int bmsIndex0, double soc, double vdc, double idc)
        {
            return
                $"舱{bmsIndex0 + 1}:  SOC {soc:0.0}%  Vdc {vdc:0.0} V  Idc {idc:0.0} A  " +
                $"并离网:{FormatGridConnectStatus(bmsIndex0)}  黑启动:{FormatBlackStartModeStatus(bmsIndex0)}";
        }
    }
}
