namespace EssSimulator.Web.DroopSlices
{
    /// <summary>
    /// EMS 有功/无功设定写入瞬间的白盒切片（用于下垂调压验证）。
    /// 采集于 ControlPipeline 写 DTO 成功之后、下一拍潮流之前。
    /// </summary>
    public sealed class DroopSlice
    {
        public Guid Id { get; init; }
        public long Sequence { get; init; }
        public DateTimeOffset TimestampUtc { get; init; }
        public DroopSliceTrigger Trigger { get; init; } = new();
        public DroopSliceGrid Grid { get; init; } = new();
        public DroopSliceMeter Meter { get; init; } = new();
        public DroopSlicePcs Pcs { get; init; } = new();
        public DroopSliceBms Bms { get; init; } = new();
        public DroopSliceTopology Topology { get; init; } = new();
        public string CapturePhase { get; init; } = "postControlWrite";
        public string Note { get; init; } =
            "Actual P/Q may precede next propagation step; use set vs actual carefully.";
    }

    public sealed class DroopSliceTrigger
    {
        public string ServerName { get; init; } = "";
        public string ParamName { get; init; } = "";
        public string Kind { get; init; } = ""; // activePowerSetting | reactivePowerSetting
        public string TargetPath { get; init; } = "";
        public double EngineeringValue { get; init; }
        public double? PreviousEngineeringValue { get; init; }
        public string Unit { get; init; } = ""; // kW | kvar
    }

    public sealed class DroopSliceGrid
    {
        public double NominalLineVoltageV { get; init; }
        public double PccLineVoltageV { get; init; }
        public double StationBus35LineVoltageV { get; init; }
        public double SystemFrequencyHz { get; init; }
        public bool MainBreakerClosed { get; init; }
    }

    public sealed class DroopSliceMeter
    {
        public double LineVoltageAB { get; init; }
        public double LineVoltageBC { get; init; }
        public double LineVoltageCA { get; init; }
        public double PhaseACurrent { get; init; }
        public double PhaseBCurrent { get; init; }
        public double PhaseCCurrent { get; init; }
        public double TotalActivePowerKw { get; init; }
        public double TotalReactivePowerKvar { get; init; }
        public double TotalApparentPowerKva { get; init; }
        public double PowerFactor { get; init; }
        public double FrequencyHz { get; init; }
    }

    public sealed class DroopSlicePcs
    {
        public int UnitIndex { get; init; }
        public int SlotInUnit { get; init; }
        public int ChannelIndex { get; init; }
        public double PcsActivePowerSettingKw { get; init; }
        public double PcsReactivePowerSettingKvar { get; init; }
        public double ActivePowerKw { get; init; }
        public double ReactivePowerKvar { get; init; }
        public double LineVoltageV { get; init; }
        public double FrequencyHz { get; init; }
        public int OperationStatus { get; init; }
        public bool PcsOnOffSwitch { get; init; }
        public string SimulatorMode { get; init; } = "";
        public bool BlackStartEnabled { get; init; }
    }

    public sealed class DroopSliceBms
    {
        public int BmsIndex { get; init; }
        public bool IsPcsLinked { get; init; }
        public int GridConnectStatus { get; init; }
        public double SocPercent { get; init; }
        public double TotalVoltageV { get; init; }
        public double CurrentA { get; init; }
        public double PowerKw { get; init; }
        public int? OperationStatus { get; init; }
        public float? MaxChargePowerKw { get; init; }
        public float? MaxDischargePowerKw { get; init; }
    }

    public sealed class DroopSliceTopology
    {
        public bool UnitBreakerClosed { get; init; }
        public bool PropagationEnabled { get; init; }
    }

    /// <summary>列表摘要（不含大字段时可扩展）。</summary>
    public sealed class DroopSliceSummary
    {
        public Guid Id { get; init; }
        public long Sequence { get; init; }
        public DateTimeOffset TimestampUtc { get; init; }
        public string ServerName { get; init; } = "";
        public string ParamName { get; init; } = "";
        public string Kind { get; init; } = "";
        public double EngineeringValue { get; init; }
        public double? PreviousEngineeringValue { get; init; }
        public string Unit { get; init; } = "";
        public int ChannelIndex { get; init; }
        public double PccLineVoltageV { get; init; }
        public double GridNominalLineVoltageV { get; init; }
        public double MeterActivePowerKw { get; init; }
        public double MeterReactivePowerKvar { get; init; }
        public double PcsActiveSettingKw { get; init; }
        public double PcsReactiveSettingKvar { get; init; }
        public double PcsActiveKw { get; init; }
        public double PcsReactiveKvar { get; init; }
    }
}
