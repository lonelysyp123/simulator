namespace EssSimulator.Display
{
    /// <summary>主接线视图用的 AC 相量快照（V/I/φ 为主，P/Q 按需）。</summary>
    public readonly record struct AcPhasorSnapshot(
        double LineVoltageV,
        double LineCurrentA,
        double PhaseAngleDeg,
        double FrequencyHz)
    {
        public double ActivePowerKw =>
            EssSimulator.EssDeviceSimModel.Model.AcQuantityConverter.ComputeActivePowerKw(
                LineVoltageV, LineCurrentA, PhaseAngleDeg);

        public double ReactivePowerKvar =>
            EssSimulator.EssDeviceSimModel.Model.AcQuantityConverter.ComputeReactivePowerKvar(
                LineVoltageV, LineCurrentA, PhaseAngleDeg);

        public double PowerFactor =>
            EssSimulator.EssDeviceSimModel.Model.AcQuantityConverter.ComputePowerFactor(
                LineVoltageV, LineCurrentA, PhaseAngleDeg);
    }

    public readonly record struct BusNodeSnapshot(
        string BusId,
        double LineVoltageV,
        double LineCurrentA,
        double PhaseAngleDeg,
        double FrequencyHz);

    public readonly record struct PcsChannelSnapshot(
        int Index,
        int UnitIndex,
        int SlotInUnit,
        AcPhasorSnapshot AcOutput,
        double ActivePowerKw,
        double ReactivePowerKw,
        double AcVoltageState,
        double AcCurrentState,
        double FrequencyHz,
        string GridMode,
        string BlackStartPhase,
        bool BlackStartEnabled);

    public readonly record struct UnitBranchSnapshot(
        int UnitIndex,
        bool UnitBreakerClosed,
        bool UnitBreakerTripped,
        AcPhasorSnapshot UnitTransformerPrimary,
        AcPhasorSnapshot UnitTransformerSecondary,
        BusNodeSnapshot? Bus690,
        PcsChannelSnapshot? PcsA,
        PcsChannelSnapshot? PcsB);

    public sealed class MainLineSnapshot
    {
        public bool PropagationEnabled { get; init; }
        public bool MainBreakerClosed { get; init; }
        public bool MainBreakerTripped { get; init; }
        public double PccLineVoltageV { get; init; }
        public double StationBus35LineVoltageV { get; init; }
        public BusNodeSnapshot? BusGrid { get; init; }
        public BusNodeSnapshot? Bus35Propagation { get; init; }
        public AcPhasorSnapshot MeterPrimary { get; init; }
        public AcPhasorSnapshot MainTransformerPrimary { get; init; }
        public AcPhasorSnapshot MainTransformerSecondary { get; init; }
        public double LoadActivePowerKw { get; init; }
        public double LoadReactivePowerKvar { get; init; }
        public IReadOnlyList<UnitBranchSnapshot> Units { get; init; } = Array.Empty<UnitBranchSnapshot>();
    }

    /// <summary>从 ess/em 路径读取主接线电气量（对齐传播求解 V-I-φ 架构）。</summary>
    internal static class GuiElectricalReader
    {
        public static MainLineSnapshot ReadMainLine(int unitStart, int unitEndExclusive)
        {
            bool propagation = GuiSimDataAccess.TryGetObject("ess.RadialGraph") != null;

            bool mainClosed = GuiSimDataAccess.SafeGetBool("ess._breaker.IsClosed");
            bool mainTripped = GuiSimDataAccess.SafeGetBool(
                "ess.ElectricalNetwork.MainBreaker.SwitchState.IsTripped");

            var meter = ReadAcPhasor("ess.ElectricalNetwork.PccMeter.Telemetry.Primary");
            if (meter.LineVoltageV <= 0 && meter.LineCurrentA <= 0)
            {
                meter = new AcPhasorSnapshot(
                    GuiSimDataAccess.SafeGetDouble("em.LineVoltageAB"),
                    Math.Max(
                        GuiSimDataAccess.SafeGetDouble("em.PhaseACurrent"),
                        Math.Max(
                            GuiSimDataAccess.SafeGetDouble("em.PhaseBCurrent"),
                            GuiSimDataAccess.SafeGetDouble("em.PhaseCCurrent"))),
                    0,
                    GuiSimDataAccess.SafeGetDouble("em.Frequency", 50));
                if (meter.LineCurrentA > 0)
                {
                    double p = GuiSimDataAccess.SafeGetDouble("em.TotalActivePower");
                    double q = GuiSimDataAccess.SafeGetDouble("em.TotalReactivePower");
                    var ph = EssSimulator.EssDeviceSimModel.Model.AcQuantityConverter.FromPowerToPhasor(
                        meter.LineVoltageV, p, q);
                    meter = meter with { PhaseAngleDeg = ph.PhaseAngleDeg, LineCurrentA = ph.LineCurrentA };
                }
            }

            var mainPri = ReadTransformerSide(
                "ess._mainTransformer._currentState.PrimaryVoltage",
                "ess._mainTransformer._currentState.PrimaryCurrent",
                "ess._mainTransformer.Primary.Output.Ac.Internal");
            var mainSec = ReadTransformerSide(
                "ess._mainTransformer._currentState.SecondaryVoltage",
                "ess._mainTransformer._currentState.SecondaryCurrent",
                "ess._mainTransformer.Secondary.Output.Ac.Internal");

            var units = new List<UnitBranchSnapshot>();
            for (int u = unitStart; u < unitEndExclusive; u++)
            {
                int a = u * 2;
                int b = u * 2 + 1;
                units.Add(new UnitBranchSnapshot(
                    u,
                    GuiSimDataAccess.SafeGetBool($"ess.ElectricalNetwork.UnitBreakers[{u}].SwitchState.IsClosed",
                        GuiSimDataAccess.SafeGetBool($"ess._unitBreakers[{u}].IsClosed")),
                    GuiSimDataAccess.SafeGetBool($"ess.ElectricalNetwork.UnitBreakers[{u}].SwitchState.IsTripped"),
                    ReadTransformerSide(
                        $"ess._unitTransformers[{u}]._currentState.PrimaryVoltage",
                        $"ess._unitTransformers[{u}]._currentState.PrimaryCurrent",
                        $"ess._unitTransformers[{u}].Primary.Output.Ac.Internal"),
                    ReadTransformerSide(
                        $"ess._unitTransformers[{u}]._currentState.SecondaryVoltage",
                        $"ess._unitTransformers[{u}]._currentState.SecondaryCurrent",
                        $"ess._unitTransformers[{u}].Secondary.Output.Ac.Internal"),
                    ReadBusNode($"ess.RadialGraph.UnitBuses690[{u}]"),
                    ReadPcsChannel(a, u, 0),
                    ReadPcsChannel(b, u, 1)));
            }

            return new MainLineSnapshot
            {
                PropagationEnabled = propagation,
                MainBreakerClosed = mainClosed,
                MainBreakerTripped = mainTripped,
                PccLineVoltageV = GuiSimDataAccess.SafeGetDouble("ess.PccLineVoltageV"),
                StationBus35LineVoltageV = GuiSimDataAccess.SafeGetDouble("ess.StationBus35LineVoltageV"),
                BusGrid = ReadBusNode("ess.RadialGraph.BusGrid"),
                Bus35Propagation = ReadBusNode("ess.RadialGraph.Bus35"),
                MeterPrimary = meter,
                MainTransformerPrimary = mainPri,
                MainTransformerSecondary = mainSec,
                LoadActivePowerKw = GuiSimDataAccess.SafeGetDouble("ess._loadSimulator.ActivePower"),
                LoadReactivePowerKvar = GuiSimDataAccess.SafeGetDouble("ess._loadSimulator.ReactivePower"),
                Units = units
            };
        }

        private static PcsChannelSnapshot? ReadPcsChannel(int pcsIndex, int unitIndex, int slot)
        {
            int channelCount = GuiSimDataAccess.GetEssUnitCount();
            if (pcsIndex < 0 || pcsIndex >= channelCount)
                return null;

            var acOut = ReadAcPhasor($"ess._pcsList[{pcsIndex}].Ac.Output.Ac.Internal");
            return new PcsChannelSnapshot(
                pcsIndex,
                unitIndex,
                slot,
                acOut,
                GuiSimDataAccess.SafeGetDouble($"ess._pcsList[{pcsIndex}]._currentState.ActivePower"),
                GuiSimDataAccess.SafeGetDouble($"ess._pcsList[{pcsIndex}]._currentState.ReactivePower"),
                GuiSimDataAccess.SafeGetDouble($"ess._pcsList[{pcsIndex}]._currentState.AcVoltage"),
                GuiSimDataAccess.SafeGetDouble($"ess._pcsList[{pcsIndex}]._currentState.AcCurrent"),
                GuiSimDataAccess.SafeGetDouble($"ess._pcsList[{pcsIndex}]._currentState.Frequency", 50),
                GuiSimDataAccess.SafeGetString($"ess._pcsList[{pcsIndex}]._currentState.GMode", "?"),
                GuiSimDataAccess.SafeGetString($"ess._pcsList[{pcsIndex}]._currentState.BlackStartPhase", "Inactive"),
                GuiSimDataAccess.SafeGetBool($"ess._pcsList[{pcsIndex}]._currentState.BlackStartEnabled"));
        }

        private static AcPhasorSnapshot ReadTransformerSide(
            string stateVoltagePath,
            string stateCurrentPath,
            string portInternalPath)
        {
            var port = ReadAcPhasor(portInternalPath);
            double v = GuiSimDataAccess.SafeGetDouble(stateVoltagePath);
            double i = Math.Abs(GuiSimDataAccess.SafeGetDouble(stateCurrentPath));
            if (port.LineVoltageV > 1)
                return port with { LineVoltageV = v > 1 ? v : port.LineVoltageV };

            double phi = port.PhaseAngleDeg;
            if (i > 0 && phi == 0 && port.LineCurrentA <= 0)
                return new AcPhasorSnapshot(v, i, phi, 50);
            return new AcPhasorSnapshot(v, port.LineCurrentA > 0 ? port.LineCurrentA : i, phi, port.FrequencyHz);
        }

        private static AcPhasorSnapshot ReadAcPhasor(string internalPathPrefix)
        {
            double v = GuiSimDataAccess.SafeGetDouble($"{internalPathPrefix}.LineVoltageV");
            double i = GuiSimDataAccess.SafeGetDouble($"{internalPathPrefix}.LineCurrentA");
            double phi = GuiSimDataAccess.SafeGetDouble($"{internalPathPrefix}.PhaseAngleDeg");
            double f = GuiSimDataAccess.SafeGetDouble($"{internalPathPrefix}.FrequencyHz", 50);
            return new AcPhasorSnapshot(v, i, phi, f);
        }

        private static BusNodeSnapshot? ReadBusNode(string objectPath)
        {
            if (GuiSimDataAccess.TryGetObject(objectPath) == null)
                return null;

            string id = GuiSimDataAccess.SafeGetString($"{objectPath}.BusId", objectPath);
            return new BusNodeSnapshot(
                id,
                GuiSimDataAccess.SafeGetDouble($"{objectPath}.LineVoltageV"),
                GuiSimDataAccess.SafeGetDouble($"{objectPath}.TotalLineCurrentA"),
                GuiSimDataAccess.SafeGetDouble($"{objectPath}.TotalPhaseAngleDeg"),
                GuiSimDataAccess.SafeGetDouble($"{objectPath}.FrequencyHz", 50));
        }
    }
}
