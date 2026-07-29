using EssSimulator.Display;
using EssSimulator.EssSimModelApi.Mappers;

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
                LoadActivePowerSetKw = snap.LoadActivePowerSetKw,
                LoadReactivePowerSetKvar = snap.LoadReactivePowerSetKvar,
                GridNominalLineVoltageV = snap.GridNominalLineVoltageV,
                GridNominalFrequencyHz = snap.GridNominalFrequencyHz,
                SystemFrequencyHz = snap.SystemFrequencyHz,
                MeterThreePhase = snap.MeterThreePhase,
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
            bool linked = IsBmsPcsLinked(channelIndex0);
            double packVoltage = GuiSimDataAccess.SafeGetDouble(
                $"ess._batteryRacks[{channelIndex0}]._currentState.TotalVoltage");
            double packCurrent = GuiSimDataAccess.SafeGetDouble(
                $"ess._batteryRacks[{channelIndex0}]._currentState.TotalCurrent");
            // 与电气端口一致：下电/断链后 DC 侧对外电压、电流为 0
            double dcVoltage = linked ? packVoltage : 0;
            double dcCurrent = linked ? packCurrent : 0;

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
                DcVoltage = dcVoltage,
                DcCurrent = dcCurrent,
                GridConnect = GuiStatusFormatters.FormatGridConnectStatus(channelIndex0),
                BmsBlackStart = GuiStatusFormatters.FormatBmsMainLineBlackStart(channelIndex0),
                BmsCompact = BuildBmsCompact(channelIndex0, dcVoltage, dcCurrent),
                CumulativeChargeEnergyKwh = GuiSimDataAccess.SafeGetDouble(
                    $"ess._batteryRacks[{channelIndex0}]._currentState.TotalChargeEnergy"),
                CumulativeDischargeEnergyKwh = GuiSimDataAccess.SafeGetDouble(
                    $"ess._batteryRacks[{channelIndex0}]._currentState.TotalDischargeEnergy"),
                BmsEnergy = BuildBmsEnergy(channelIndex0),
                BmsRunStatus = BuildBmsRunStatus(channelIndex0),
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
                PcsGridMode = pcs != null ? GuiStatusFormatters.FormatGridModeLabel(pcs.Value.GridMode) : "—",
                TargetActivePowerKw = GuiSimDataAccess.SafeGetDouble(
                    $"emu{unitIndex0 + 1}.PcsList[{slotInUnit0}].PCSActivePowerSetting"),
                TargetReactivePowerKvar = GuiSimDataAccess.SafeGetDouble(
                    $"emu{unitIndex0 + 1}.PcsList[{slotInUnit0}].PCSReactivePowerSetting"),
                ActualActivePowerKw = pcs?.ActivePowerKw
                    ?? GuiSimDataAccess.SafeGetDouble(
                        $"ess._pcsList[{channelIndex0}]._currentState.ActivePower"),
                ActualReactivePowerKvar = pcs?.ReactivePowerKw
                    ?? GuiSimDataAccess.SafeGetDouble(
                        $"ess._pcsList[{channelIndex0}]._currentState.ReactivePower"),
                EmuUnitNumber = unitIndex0 + 1,
                ActivePowerYtPoint = slotInUnit0 == 0 ? "yt0" : "yt4",
                ReactivePowerYtPoint = slotInUnit0 == 0 ? "yt1" : "yt5"
            };
        }

        private static bool IsBmsPcsLinked(int bmsIndex0) =>
            GuiSimDataAccess.SafeGetBool($"bms{bmsIndex0 + 1}.BatteryStacks[0].IsPcsLinked")
            || GuiSimDataAccess.SafeGetBool(
                $"ess._batteryRacks[{bmsIndex0}]._currentState.IsPcsLinked");

        private static string BuildBmsCompact(int bmsIndex0, double dcVoltage, double dcCurrent)
        {
            double s = 100 * GuiSimDataAccess.SafeGetDouble(
                $"ess._batteryRacks[{bmsIndex0}]._currentState.MinClusterSOC");
            return $"SOC {s:0.0}%  Vdc {dcVoltage:0.0}  Idc {dcCurrent:0.0}";
        }

        private static string BuildBmsEnergy(int bmsIndex0)
        {
            double ch = GuiSimDataAccess.SafeGetDouble(
                $"ess._batteryRacks[{bmsIndex0}]._currentState.TotalChargeEnergy");
            double dis = GuiSimDataAccess.SafeGetDouble(
                $"ess._batteryRacks[{bmsIndex0}]._currentState.TotalDischargeEnergy");
            return $"累计充 {ch:0.0} / 放 {dis:0.0} kWh";
        }

        private static string BuildBmsRunStatus(int bmsIndex0)
        {
            int code = (int)GuiSimDataAccess.SafeGetDouble(
                $"bms{bmsIndex0 + 1}.BatteryStacks[0].OperationStatus");
            string label = BmsMapper.GetStackOperationStatusLabel(code);
            return $"运行:{label}";
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
        public double LoadActivePowerSetKw { get; set; }
        public double LoadReactivePowerSetKvar { get; set; }
        public double GridNominalLineVoltageV { get; set; }
        public double GridNominalFrequencyHz { get; set; }
        public double SystemFrequencyHz { get; set; }
        public MeterThreePhaseSnapshot MeterThreePhase { get; set; }
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
        public double CumulativeChargeEnergyKwh { get; set; }
        public double CumulativeDischargeEnergyKwh { get; set; }
        public string BmsEnergy { get; set; } = "";
        public string BmsRunStatus { get; set; } = "";
        public string PcsDeviceState { get; set; } = "";
        public string PcsStartStop { get; set; } = "";
        public string PcsTargetP { get; set; } = "";
        public string PcsActualP { get; set; } = "";
        public string PcsTargetQ { get; set; } = "";
        public string PcsActualQ { get; set; } = "";
        public string PcsBlackStart { get; set; } = "";
        public string PcsAcLine { get; set; } = "";
        public string PcsGridMode { get; set; } = "";
        public double TargetActivePowerKw { get; set; }
        public double TargetReactivePowerKvar { get; set; }
        /// <summary>PCS 实时有功 kW：&gt;0 放电，&lt;0 充电。</summary>
        public double ActualActivePowerKw { get; set; }
        public double ActualReactivePowerKvar { get; set; }
        public int EmuUnitNumber { get; set; }
        public string ActivePowerYtPoint { get; set; } = "yt0";
        public string ReactivePowerYtPoint { get; set; } = "yt1";
    }
}
