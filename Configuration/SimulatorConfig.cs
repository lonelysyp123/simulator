namespace EssSimulator.Configuration
{
    /// <summary>顶层仿真器配置（对应 appsettings.json: Simulator 节）</summary>
    public class SimulatorConfig
    {
        public const string Section = "Simulator";

        /// <summary>储能单元（ESS Unit）数量，每个单元拥有独立的 BMS Modbus 服务</summary>
        public int UnitCount { get; set; } = 2;

        /// <summary>每个单元的电池簇数量</summary>
        public int ClusterCount { get; set; } = 12;

        /// <summary>每簇电池模组数量</summary>
        public int PackCount { get; set; } = 4;

        /// <summary>BMS Modbus TCP 基础端口（单元 i 使用 BaseModbusPort + i*10）</summary>
        public int BaseModbusPort { get; set; } = 1502;

        /// <summary>PCS (EMU) Modbus TCP 端口</summary>
        public int PcsModbusPort { get; set; } = 1501;

        /// <summary>电表 Modbus TCP 端口</summary>
        public int EmModbusPort { get; set; } = 1500;

        /// <summary>
        /// 主循环的真实休眠间隔（毫秒）。决定 CPU 占用率，与仿真速度无关。
        /// 例如设为 200，则主循环每 200ms 真实时间 tick 一次。
        /// </summary>
        public int SimStepMs { get; set; } = 200;

        /// <summary>
        /// 仿真时间加速倍率。每次 tick 传给物理模型的时间步长 = SimStepMs × Speedup（ms）。
        /// Speedup=1 表示实时仿真；Speedup=100 表示仿真时间以 100 倍速推进。
        /// 注意：此值与主循环休眠时间无关，仅影响物理量（SOC、能量、温度等）的积分速率
        /// 以及 PCS 功率爬坡线程的 Sleep 压缩倍率。
        /// </summary>
        public double Speedup { get; set; } = 1000.0;

        /// <summary>是否禁用控制台 GUI（无头模式）</summary>
        public bool NoGui { get; set; } = false;

        /// <summary>电池单体串联数</summary>
        public int CellSeriesCount { get; set; } = 104;

        /// <summary>电池单体并联数</summary>
        public int CellParallelCount { get; set; } = 1;

        /// <summary>单体额定电压（V）</summary>
        public double CellNominalVoltage { get; set; } = 3.2;

        /// <summary>单体额定容量（Ah）</summary>
        public double CellNominalCapacity { get; set; } = 314;

        /// <summary>模组内阻（Ω）</summary>
        public double PackInternalResistance { get; set; } = 0.05;

        /// <summary>簇内阻（Ω）</summary>
        public double ClusterInternalResistance { get; set; } = 0.1;

        /// <summary>堆内阻（Ω）</summary>
        public double RackInternalResistance { get; set; } = 0.02;
    }

    /// <summary>PCS 物理参数配置（对应 appsettings.json: Pcs 节）</summary>
    public class PcsPhysicalConfig
    {
        public const string Section = "Pcs";

        public double RatedPower { get; set; } = 1725;
        public double MaxPower { get; set; } = 1897.5;
        public double Efficiency { get; set; } = 0.99;
        public double DcVoltageRangeMin { get; set; } = 1000;
        public double DcVoltageRangeMax { get; set; } = 1500;
        public double AcVoltageNominal { get; set; } = 690;
        public double FrequencyNominal { get; set; } = 50;
        public double MaxCurrent { get; set; } = 1588;
    }

    /// <summary>变压器参数配置（对应 appsettings.json: Transformer 节）</summary>
    public class TransformerConfig
    {
        public const string Section = "Transformer";

        public double RatedPower { get; set; } = 2500;
        public double PrimaryVoltage { get; set; } = 10500;
        public double SecondaryVoltage { get; set; } = 690;
        public double NoLoadLoss { get; set; } = 50;
        public double LoadLoss { get; set; } = 200;
        public double ImpedancePercent { get; set; } = 4;
        public double NoLoadCurrentPercent { get; set; } = 2;
    }

    /// <summary>PCS 接口层默认限值配置（对应 appsettings.json: PcsDefault 节）</summary>
    public class PcsDefaultConfig
    {
        public const string Section = "PcsDefault";

        public float BatteryChargeProtectionVoltage { get; set; } = 950;
        public float BatteryDischargeProtectionVoltage { get; set; } = 500;
        public float BatteryChargeProtectionCurrent { get; set; } = 500;
        public float BatteryDischargeProtectionCurrent { get; set; } = 550;

        public float BatteryChargeCurrentLimit { get; set; } = 450;
        public float BatteryChargeVoltageLimit { get; set; } = 950;
        public float BatteryDischargeCurrentLimit { get; set; } = 450;
        public float BatteryDischargeVoltageLimit { get; set; } = 450;
        public float BatteryChargePowerLimit { get; set; } = 450;
        public float BatteryDischargePowerLimit { get; set; } = 450;
        public float ChargePowerLimit { get; set; } = 500;
        public float DischargePowerLimit { get; set; } = 500;
        public float PCSRatePower { get; set; } = 1250;

        public int ActivePowerDispatchMode { get; set; } = 1;
        public int ReactivePowerDispatchMode { get; set; } = 1;
        public int ActiveReactivePriority { get; set; } = 1;
        public int FrequencyActiveSetting { get; set; } = 1;

        public float EmuMaxChargePower { get; set; } = 1000;
        public float EmuMaxDischargePower { get; set; } = 1000;
    }

    /// <summary>负载仿真配置（对应 appsettings.json: Load 节）</summary>
    public class LoadConfig
    {
        public const string Section = "Load";

        /// <summary>有功计划负载（kW）</summary>
        public double ActivePowerPlan { get; set; } = 500;

        /// <summary>无功计划负载（kvar）</summary>
        public double ReactivePowerPlan { get; set; } = 0;
    }
}
