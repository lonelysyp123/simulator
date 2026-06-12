using System;

namespace EssSimulator.EssDeviceSimModel
{
    public enum RampCurve
    {
        Linear,
        Quadratic,
        SquareRoot
    }

    public enum OperationMode
    {
        Off,
        Standby,
        Normal,
    }

    public enum GridMode
    {
        GridConnected,
        Islanded
    }

    public enum BlackStartPhase
    {
        Inactive = 0,
        /// <summary>DC 就绪、自检，AC 未输出。</summary>
        Preparing = 1,
        /// <summary>V/f 软启动：电压/频率按斜坡爬升。</summary>
        SoftStarting = 2,
        /// <summary>闭环调压：追踪设定，可因限流导致母线电压滞后。</summary>
        VoltageRegulating = 3,
        /// <summary>母线已达标，稳态构网。</summary>
        Synchronized = 4
    }

    public class PcsConfiguration
    {
        public double RatedPower { get; set; }
        public double MaxPower { get; set; }
        public double Efficiency { get; set; }
        public double DcVoltageRangeMin { get; set; }
        public double DcVoltageRangeMax { get; set; }
        public double AcVoltageNominal { get; set; }
        public double FrequencyNominal { get; set; }
        public double MaxCurrent { get; set; }
    }

    public class PcsState
    {
        public OperationMode Mode { get; set; }
        public GridMode GMode { get; set; }
        public double DcVoltage { get; set; }
        public double DcCurrent { get; set; }
        public double AcVoltage { get; set; }
        public double AcCurrent { get; set; }
        public double ActivePower { get; set; }
        public double ReactivePower { get; set; }
        public double Frequency { get; set; }
        public double Temperature { get; set; }
        public double ActivePowerSettingVal { get; set; }
        public double ReactivePowerSettingVal { get; set; }
        public double PowerFatorSettingVal { get; set; }
        public double DcCCSettingVal { get; set; }
        public double DcCVSettingVal { get; set; }
        public double DcCPSettingVal { get; set; }
        public double DcProtectChgCurrent { get; set; }
        public double DcProtectChgVoltage { get; set; }
        public double DcProtectDsgCurrent { get; set; }
        public double DcProtectDsgVoltage { get; set; }
        public double DcLimitChgCurrent { get; set; }
        public double DcLimitChgVoltage { get; set; }
        public double DcLimitDsgCurrent { get; set; }
        public double DcLimitDsgVoltage { get; set; }
        public double DcLimitChgPower { get; set; }
        public double DcLimitDsgPower { get; set; }
        public ushort FaultType { get; set; }
        public string? FaultMessage { get; set; }
        public double DailyChargeEnergy { get; set; }
        public double TotalChargeEnergy { get; set; }
        public double DailyDischargeEnergy { get; set; }
        public double TotalDischargeEnergy { get; set; }
        public DateTime Timestamp { get; set; }
        public double IslandVoltageCommandV { get; set; }
        public double IslandVoltageEffectiveV { get; set; }
        public bool BlackStartEnabled { get; set; }
        public BlackStartPhase BlackStartPhase { get; set; }
    }

    public class GridState
    {
        public double Voltage { get; set; }
        public double Frequency { get; set; }
        public bool IsAvailable { get; set; }
    }
}
