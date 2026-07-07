using EssSimulator.Display;

namespace EssSimulator.Web
{
    /// <summary>为主接线 Web 视图补充 PCS/BMS 展示字段（对齐原 TUI 主接线图信息量）。</summary>
    public static class MainLineEnricher
    {
        public static MainLineViewModel Build()
        {
            int channelCount = Math.Max(1, GuiSimDataAccess.GetEssUnitCount());
            int unitCount = Math.Max(1, (int)Math.Ceiling(channelCount / 2.0));
            var snap = GuiElectricalReader.ReadMainLine(0, unitCount);
            return Build(snap, channelCount);
        }

        public static MainLineViewModel Build(MainLineSnapshot snap, int? channelCountOverride = null)
        {
            int channelCount = channelCountOverride ?? Math.Max(1, GuiSimDataAccess.GetEssUnitCount());
            return new MainLineViewModel
            {
                PropagationEnabled = snap.PropagationEnabled,
                MainBreakerClosed = snap.MainBreakerClosed,
                MainBreakerTripped = snap.MainBreakerTripped,
                PccLineVoltageV = snap.PccLineVoltageV,
                StationBus35LineVoltageV = snap.StationBus35LineVoltageV,
                BusGrid = snap.BusGrid,
                Bus35Propagation = snap.Bus35Propagation,
                MeterPrimary = snap.MeterPrimary,
                MainTransformerPrimary = snap.MainTransformerPrimary,
                MainTransformerSecondary = snap.MainTransformerSecondary,
                LoadActivePowerKw = snap.LoadActivePowerKw,
                LoadReactivePowerKvar = snap.LoadReactivePowerKvar,
                BlackStartSummary = GuiStatusFormatters.BuildBlackStartSwitchSummary(0, channelCount),
                MainBreakerLabel = FormatBreaker(snap.MainBreakerClosed, snap.MainBreakerTripped),
                Units = snap.Units.Select(u => EnrichUnit(u, channelCount)).ToList()
            };
        }

        private static MainLineUnitViewModel EnrichUnit(UnitBranchSnapshot u, int channelCount)
        {
            int a = u.UnitIndex * 2;
            int b = a + 1;
            return new MainLineUnitViewModel
            {
                UnitIndex = u.UnitIndex,
                UnitNumber = u.UnitIndex + 1,
                UnitBreakerClosed = u.UnitBreakerClosed,
                UnitBreakerTripped = u.UnitBreakerTripped,
                UnitBreakerLabel = FormatBreaker(u.UnitBreakerClosed, u.UnitBreakerTripped),
                UnitTransformerPrimary = u.UnitTransformerPrimary,
                UnitTransformerSecondary = u.UnitTransformerSecondary,
                UnitTransformerLine = GuiStatusFormatters.FormatAcPhasorViPhi(u.UnitTransformerSecondary),
                Bus690 = u.Bus690,
                PcsA = u.PcsA,
                PcsB = u.PcsB,
                ChannelA = a < channelCount ? BuildChannel(a, u.UnitIndex, 0, u.PcsA) : null,
                ChannelB = b < channelCount ? BuildChannel(b, u.UnitIndex, 1, u.PcsB) : null
            };
        }

        private static MainLineChannelViewModel BuildChannel(
            int channelIndex0, int unitIndex0, int slotInUnit0, PcsChannelSnapshot? pcs)
        {
            return new MainLineChannelViewModel
            {
                ChannelIndex = channelIndex0,
                ChannelNumber = channelIndex0 + 1,
                CompartmentNumber = channelIndex0 + 1,
                PcsNumber = channelIndex0 + 1,
                UnitIndex = unitIndex0,
                SlotInUnit = slotInUnit0,
                SocPercent = 100 * GuiSimDataAccess.SafeGetDouble(
                    $"ess._batteryRacks[{channelIndex0}]._currentState.MinClusterSOC"),
                DcVoltage = GuiSimDataAccess.SafeGetDouble(
                    $"ess._batteryRacks[{channelIndex0}]._currentState.TotalVoltage"),
                DcCurrent = GuiSimDataAccess.SafeGetDouble(
                    $"ess._batteryRacks[{channelIndex0}]._currentState.TotalCurrent"),
                GridConnect = GuiStatusFormatters.FormatGridConnectStatus(channelIndex0),
                BmsBlackStart = GuiStatusFormatters.FormatBmsMainLineBlackStart(channelIndex0),
                BmsCompact = BuildBmsCompact(channelIndex0),
                PcsDeviceState = GuiStatusFormatters.FormatPcsMainLineDeviceState(unitIndex0, slotInUnit0, channelIndex0),
                PcsStartStop = GuiStatusFormatters.FormatPcsMainLineStartStop(unitIndex0, slotInUnit0),
                PcsTargetP = GuiStatusFormatters.FormatPcsMainLineTargetPower(unitIndex0, slotInUnit0),
                PcsActualP = GuiStatusFormatters.FormatPcsMainLineActualPower(
                    channelIndex0, pcs?.ActivePowerKw ?? 0),
                PcsTargetQ = GuiStatusFormatters.FormatPcsMainLineTargetReactive(unitIndex0, slotInUnit0),
                PcsActualQ = GuiStatusFormatters.FormatPcsMainLineActualReactive(
                    channelIndex0, pcs?.ReactivePowerKw ?? 0),
                PcsBlackStart = GuiStatusFormatters.FormatPcsMainLineBlackStart(unitIndex0, slotInUnit0, channelIndex0),
                PcsAcLine = pcs != null ? GuiStatusFormatters.FormatPcsAcLine(pcs.Value) : "—",
                PcsGridMode = pcs != null ? GuiStatusFormatters.FormatGridModeLabel(pcs.Value.GridMode) : "—"
            };
        }

        private static string BuildBmsCompact(int bmsIndex0)
        {
            double s = 100 * GuiSimDataAccess.SafeGetDouble(
                $"ess._batteryRacks[{bmsIndex0}]._currentState.MinClusterSOC");
            double v = GuiSimDataAccess.SafeGetDouble(
                $"ess._batteryRacks[{bmsIndex0}]._currentState.TotalVoltage");
            double c = GuiSimDataAccess.SafeGetDouble(
                $"ess._batteryRacks[{bmsIndex0}]._currentState.TotalCurrent");
            return $"SOC {s:0.0}%  Vdc {v:0.0}  Idc {c:0.0}";
        }

        private static string FormatBreaker(bool closed, bool tripped) =>
            tripped ? "跳闸" : closed ? "合" : "分";
    }

    public sealed class MainLineViewModel
    {
        public bool PropagationEnabled { get; set; }
        public bool MainBreakerClosed { get; set; }
        public bool MainBreakerTripped { get; set; }
        public string MainBreakerLabel { get; set; } = "";
        public double PccLineVoltageV { get; set; }
        public double StationBus35LineVoltageV { get; set; }
        public BusNodeSnapshot? BusGrid { get; set; }
        public BusNodeSnapshot? Bus35Propagation { get; set; }
        public AcPhasorSnapshot MeterPrimary { get; set; }
        public AcPhasorSnapshot MainTransformerPrimary { get; set; }
        public AcPhasorSnapshot MainTransformerSecondary { get; set; }
        public double LoadActivePowerKw { get; set; }
        public double LoadReactivePowerKvar { get; set; }
        public string BlackStartSummary { get; set; } = "";
        public List<MainLineUnitViewModel> Units { get; set; } = new();
    }

    public sealed class MainLineUnitViewModel
    {
        public int UnitIndex { get; set; }
        public int UnitNumber { get; set; }
        public bool UnitBreakerClosed { get; set; }
        public bool UnitBreakerTripped { get; set; }
        public string UnitBreakerLabel { get; set; } = "";
        public AcPhasorSnapshot UnitTransformerPrimary { get; set; }
        public AcPhasorSnapshot UnitTransformerSecondary { get; set; }
        public string UnitTransformerLine { get; set; } = "";
        public BusNodeSnapshot? Bus690 { get; set; }
        public PcsChannelSnapshot? PcsA { get; set; }
        public PcsChannelSnapshot? PcsB { get; set; }
        public MainLineChannelViewModel? ChannelA { get; set; }
        public MainLineChannelViewModel? ChannelB { get; set; }
    }

    public sealed class MainLineChannelViewModel
    {
        public int ChannelIndex { get; set; }
        public int ChannelNumber { get; set; }
        public int CompartmentNumber { get; set; }
        public int PcsNumber { get; set; }
        public int UnitIndex { get; set; }
        public int SlotInUnit { get; set; }
        public double SocPercent { get; set; }
        public double DcVoltage { get; set; }
        public double DcCurrent { get; set; }
        public string GridConnect { get; set; } = "";
        public string BmsBlackStart { get; set; } = "";
        public string BmsCompact { get; set; } = "";
        public string PcsDeviceState { get; set; } = "";
        public string PcsStartStop { get; set; } = "";
        public string PcsTargetP { get; set; } = "";
        public string PcsActualP { get; set; } = "";
        public string PcsTargetQ { get; set; } = "";
        public string PcsActualQ { get; set; } = "";
        public string PcsBlackStart { get; set; } = "";
        public string PcsAcLine { get; set; } = "";
        public string PcsGridMode { get; set; } = "";
    }
}
