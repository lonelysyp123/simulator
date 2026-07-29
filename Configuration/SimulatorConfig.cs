using System.Collections.Generic;
using System.Linq;

namespace EssSimulator.Configuration
{
    public class RuntimeConfig
    {
        /// <summary>是否禁用控制台 GUI（无头模式）</summary>
        public bool NoGui { get; set; } = false;

        /// <summary>
        /// 积分步长倍数（无量纲）：仅放大 SOC、电能等积分量的 dt（基于真实回调间隔），不改变瞬时值动力学 dt。
        /// </summary>
        public double IntegrationStepMultiplier { get; set; } = 1.0;

        /// <summary>PCS 功率爬坡参数</summary>
        public PcsRampConfig PcsRamp { get; set; } = new();

        /// <summary>协议与仿真就绪后，向各 PCS 启停线圈写入 1 并驱动并网（默认合闸工况）。</summary>
        public bool AutoStartPcsOnStartup { get; set; } = true;

        /// <summary>电压源（电网）激活性传播周期（ms）。</summary>
        public int PropagationIntervalMs { get; set; } = 100;

        /// <summary>使用事件驱动电气传播替代 Solver 主循环步进。</summary>
        public bool UseElectricalPropagation { get; set; } = true;

        /// <summary>设备 Step 后 Q-U 与电压传播的最大迭代轮数（≥1，首轮为 Phase3–5 主路径）。</summary>
        public int PropagationQuvMaxIterations { get; set; } = 3;

        /// <summary>Q-U/V 迭代相对电压收敛阈值（pu，相对 PCC 额定线电压）。</summary>
        public double PropagationVoltageTolerancePu { get; set; } = 0.001;

        /// <summary>电站热网络 / 气候（阶段 2：外温 + BMS 柜体空气）。</summary>
        public ThermalRuntimeConfig Thermal { get; set; } = new();
    }

    /// <summary>热仿真运行时配置（Simulator.Runtime.Thermal）。</summary>
    public class ThermalRuntimeConfig
    {
        /// <summary>为 false 时 BMS 环境温回退到 <see cref="ClimateConfig.FixedCelsius"/>（默认 25）。</summary>
        public bool Enabled { get; set; } = true;

        public ClimateConfig Climate { get; set; } = new();
        public BmsCabinetThermalConfig Cabinet { get; set; } = new();

        /// <summary>多点温度探针偏置（°C）；挂点见 <see cref="ThermalProbeBiasesConfig"/>。</summary>
        public ThermalProbeBiasesConfig ProbeBiases { get; set; } = new();

        /// <summary>温度反馈：降额与寿命加速（阶段 5）。</summary>
        public ThermalFeedbackConfig Feedback { get; set; } = new();
    }

    /// <summary>电–热反馈：高温降额、温度加速日历老化。</summary>
    public class ThermalFeedbackConfig
    {
        public bool DeratingEnabled { get; set; } = true;

        /// <summary>开始线性降额的温度（°C）。</summary>
        public double DerateStartCelsius { get; set; } = 40;

        /// <summary>降额至下限的温度（°C）。</summary>
        public double DerateFullCelsius { get; set; } = 55;

        /// <summary>满降额时仍保留的功率比例（0–1）。</summary>
        public double MinPowerFactor { get; set; } = 0.2;

        public bool TemperatureAgingEnabled { get; set; } = true;

        public double AgingReferenceCelsius { get; set; } = 25;

        /// <summary>Arrhenius 系数 B≈Ea/R（K）；约 5000 时 45°C 相对 25°C 加速约 2–3 倍。</summary>
        public double AgingArrheniusB { get; set; } = 5000;

        /// <summary>参考温度下日历老化速率（每年 Age 增量，0–1）。</summary>
        public double CalendarAgingPerYearAtRef { get; set; } = 0.02;
    }

    /// <summary>各测点相对混合温度的固定偏置（模拟传感器安装位置误差）。</summary>
    public class ThermalProbeBiasesConfig
    {
        public double CabinetAirCelsius { get; set; }
        public double CoilCelsius { get; set; } = -1.5;
        public double CondenserCelsius { get; set; } = 2.0;
        public double LiquidSupplyCelsius { get; set; } = -2.0;
        public double LiquidReturnCelsius { get; set; } = 1.0;
        public double DehumidifierTopCelsius { get; set; } = 0.8;
        public double DehumidifierMidCelsius { get; set; }
        public double DehumidifierBottomCelsius { get; set; } = -0.8;
    }

    /// <summary>室外气温模型参数。</summary>
    public class ClimateConfig
    {
        /// <summary>若有值则忽略日变化，始终返回该温度（°C）。</summary>
        public double? FixedCelsius { get; set; }

        public double MinCelsius { get; set; } = 15;
        public double MaxCelsius { get; set; } = 35;

        /// <summary>日最高温出现的小时（0–24，可小数）。</summary>
        public double PeakHour { get; set; } = 14;
    }

    /// <summary>单个 BMS 柜体集总热网络参数。</summary>
    public class BmsCabinetThermalConfig
    {
        public double AirThermalCapacityJPerK { get; set; } = 80_000;
        public double ShellThermalCapacityJPerK { get; set; } = 200_000;
        public double BatteryThermalCapacityJPerK { get; set; } = 800_000;

        /// <summary>室外→外壳热阻（K/W）。</summary>
        public double OutdoorToShellResistanceKPerW { get; set; } = 0.04;

        /// <summary>外壳→柜内空气热阻（K/W）。</summary>
        public double ShellToAirResistanceKPerW { get; set; } = 0.015;

        /// <summary>电池等效热容节点→柜内空气热阻（K/W）。</summary>
        public double BatteryToAirResistanceKPerW { get; set; } = 0.008;

        /// <summary>空调制冷功率（W）；0 表示无空调。</summary>
        public double HvacCoolingPowerW { get; set; } = 0;

        /// <summary>空调目标柜温（°C）。</summary>
        public double HvacSetpointCelsius { get; set; } = 25;

        /// <summary>运行时是否允许空调工作（仍需 HvacCoolingPowerW&gt;0）。</summary>
        public bool HvacEnabled { get; set; } = true;

        /// <summary>制冷回差（°C）：高于设定+回差开启，低于设定关闭。</summary>
        public double HvacHysteresisCelsius { get; set; } = 2;

        /// <summary>比例制冷增益（W/°C），与额定功率取小。</summary>
        public double HvacProportionalGainWPerK { get; set; } = 800;
    }

    public class PcsRampConfig
    {
        /// <summary>功率变化斜率（默认与历史行为一致）</summary>
        public double Slope { get; set; } = 1;

        /// <summary>每一级功率变化时间间隔（ms）</summary>
        public int IntervalMs { get; set; } = 100;

        /// <summary>新设定值生效前初始延时（ms）</summary>
        public int DelayMs { get; set; } = 0;
    }

    public class ProtocolConfig
    {
        /// <summary>BMS Modbus TCP 基础端口（后续按步长递增）</summary>
        public int BaseBmsModbusPort { get; set; } = 1502;

        /// <summary>BMS 端口步长</summary>
        public int BmsPortStep { get; set; } = 10;

        /// <summary>EMU Modbus TCP 基础端口（每个储能单元一个 EMU 从站）</summary>
        public int BaseEmuModbusPort { get; set; } = 1501;

        /// <summary>EMU 端口步长（单位：端口号）</summary>
        public int EmuPortStep { get; set; } = 1;

        /// <summary>电表 Modbus TCP 端口</summary>
        public int EmModbusPort { get; set; } = 1500;

        /// <summary>是否启用 LocalControl 聚合协议（每路聚合 4 个 EMU / 8 台 PCS）。</summary>
        public bool EnableLocalControl { get; set; } = false;

        /// <summary>LocalControl Modbus TCP 基础端口。</summary>
        public int BaseLocalControlModbusPort { get; set; } = 1700;

        /// <summary>LocalControl 端口步长（单位：端口号）。</summary>
        public int LocalControlPortStep { get; set; } = 1;

        /// <summary>每路 LocalControl 聚合的 EMU 数。</summary>
        public int LocalControlEmuPerGroup { get; set; } = 4;
    }

    public class PcsDeviceConfig
    {
        public string Name { get; set; } = "PCS";
        /// <summary>
        /// PCS 爬坡参数覆盖项；为空时回退到 Runtime.PcsRamp。
        /// </summary>
        public PcsRampConfig? PcsRamp { get; set; }
    }

    public class BmsDeviceConfig
    {
        public string Name { get; set; } = "BMS";
        public int ClusterCount { get; set; } = 12;
        public int PackCount { get; set; } = 4;
        public int CellSeriesCount { get; set; } = 104;
        public int CellParallelCount { get; set; } = 1;
        public double CellNominalVoltage { get; set; } = 3.2;
        public double CellNominalCapacity { get; set; } = 314;
        public double CellInitialSoc { get; set; } = 0.5;
        public double CellInitialSocRandomRange { get; set; } = 0.05;
        public double PackInternalResistance { get; set; } = 0.05;
        public double ClusterInternalResistance { get; set; } = 0.1;
        public double RackInternalResistance { get; set; } = 0.02;
    }

    public class EssUnitConfig
    {
        public string Name { get; set; } = "Unit";
        /// <summary>固定 2 路 PCS 配置</summary>
        public List<PcsDeviceConfig> Pcs { get; set; } = new();
        /// <summary>固定 2 路 BMS 配置</summary>
        public List<BmsDeviceConfig> Bms { get; set; } = new();
    }

    /// <summary>储能单元列表（对应 appsettings.json: EssUnits 节，绑定到 <see cref="SimulatorConfig.Devices"/>）</summary>
    public static class EssUnitsConfig
    {
        public const string Section = "EssUnits";
    }

    /// <summary>顶层仿真器配置（对应 appsettings.json: Simulator 节）</summary>
    public class SimulatorConfig
    {
        public const string Section = "Simulator";

        public RuntimeConfig Runtime { get; set; } = new();
        public ProtocolConfig Protocol { get; set; } = new();
        public List<EssUnitConfig> Devices { get; set; } = new();

        // 兼容旧代码的便捷属性（由新结构推导）
        public int UnitCount => Math.Max(1, Devices?.Count ?? 1) * 2; // 每单元固定 2 路 PCS/BMS
        public int BaseModbusPort => Protocol.BaseBmsModbusPort;
        public int PcsModbusPort => Protocol.BaseEmuModbusPort;
        public int EmModbusPort => Protocol.EmModbusPort;
        public double IntegrationStepMultiplier => Runtime.IntegrationStepMultiplier;
        public bool NoGui => Runtime.NoGui;

        public IReadOnlyList<BmsDeviceConfig> GetBmsDeviceConfigs()
        {
            var list = new List<BmsDeviceConfig>();
            var units = (Devices == null || Devices.Count == 0) ? new List<EssUnitConfig> { new() } : Devices;

            foreach (var unit in units)
            {
                var bms = unit.Bms ?? new List<BmsDeviceConfig>();
                while (bms.Count < 2) bms.Add(new BmsDeviceConfig());
                list.Add(bms[0]);
                list.Add(bms[1]);
            }
            return list;
        }

        public IReadOnlyList<PcsDeviceConfig> GetPcsDeviceConfigs()
        {
            var list = new List<PcsDeviceConfig>();
            var units = (Devices == null || Devices.Count == 0) ? new List<EssUnitConfig> { new() } : Devices;

            foreach (var unit in units)
            {
                var pcs = unit.Pcs ?? new List<PcsDeviceConfig>();
                while (pcs.Count < 2) pcs.Add(new PcsDeviceConfig());
                list.Add(pcs[0]);
                list.Add(pcs[1]);
            }
            return list;
        }
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
        /// <summary>线损相关系数（用于并网电压折算）</summary>
        public double GridLossCoefficient { get; set; } = 0.01;

        /// <summary>离网 V/f：有效电压向命令值过渡的最长仿真时间（ms），默认 100ms 内完成建压/降压。</summary>
        public double IslandVoltageRampDurationMs { get; set; } = 100;

        /// <summary>黑启动：孤岛电压设定与有效值每差 1V 对应的有功调节量（kW，正放）。</summary>
        public double BlackStartActivePowerGainKwPerVolt { get; set; } = 2.174;

        /// <summary>黑启动自动有功上限（kW）。</summary>
        public double BlackStartMaxActivePowerKw { get; set; } = 200;

        /// <summary>建压过程中按有效电压百分比附加的励磁有功（占额定功率比例，0–1）。</summary>
        public double BlackStartMagnetizingPowerFraction { get; set; } = 0.02;

        /// <summary>判定 690V 母线已带电的电压比例（相对 AcVoltageNominal，默认 0.85≈587V）。</summary>
        public double BlackStartBusEnergizedFraction { get; set; } = 0.85;

        /// <summary>黑启动准备阶段时长（ms）：DC 就绪自检，AC 保持 0。</summary>
        public double BlackStartPrechargeDelayMs { get; set; } = 300;

        /// <summary>软启动电压爬坡速率（V/s，线电压）。</summary>
        public double BlackStartVoltageRampVs { get; set; } = 120;

        /// <summary>软启动起始频率（Hz）。</summary>
        public double BlackStartFrequencyStartHz { get; set; } = 47;

        /// <summary>频率爬升最大速率（Hz/s）。</summary>
        public double BlackStartFrequencyRampHzPerSec { get; set; } = 12;

        /// <summary>建压期无功电压支撑（kvar/V，正=升压）。</summary>
        public double BlackStartReactiveVoltageGainKvarPerV { get; set; } = 4.0;

        /// <summary>建压期交流电流限幅（相对 MaxCurrent 比例，0–1）。</summary>
        public double BlackStartCurrentLimitFraction { get; set; } = 0.45;

        /// <summary>
        /// 黑启动稳态站用电分担（空载铁损+线损、励磁无功）：
        /// AllOnBus=同单元所有黑启动运行 PCS 按额定功率比例分摊；
        /// LeaderOnly=仅建压机（有效电压最高者）承担，从机不重复励磁/空载。
        /// </summary>
        public string BlackStartSteadyLossShareMode { get; set; } = "AllOnBus";
    }

    /// <summary>变压器参数配置（对应 appsettings.json: Transformer 节）</summary>
    public class TransformerConfig
    {
        public const string Section = "Transformer";

        public double RatedPower { get; set; } = 2500;
        public double PrimaryVoltage { get; set; } = 220000;
        public double SecondaryVoltage { get; set; } = 35000;
        public double NoLoadLoss { get; set; } = 50;
        public double LoadLoss { get; set; } = 200;
        public double ImpedancePercent { get; set; } = 4;
        /// <summary>并网点无功-电压影响系数（1.0=按阻抗标称影响）</summary>
        public double ReactiveVoltageInfluenceCoefficient { get; set; } = 1.0;
        public double NoLoadCurrentPercent { get; set; } = 2;

        public bool MagnetizingInrushEnabled { get; set; } = true;
        public double MagnetizingInrushDvDtThresholdPuPerSec { get; set; } = 0.8;
        public double MagnetizingInrushPeakExtraMultipleOfRatedPrimary { get; set; } = 4.0;
        public double MagnetizingInrushDecayTimeConstantSec { get; set; } = 0.45;
        public double MagnetizingInrushMaxExtraMultipleOfRatedPrimary { get; set; } = 12.0;
    }

    /// <summary>单元升压一体机变压器参数（对应 appsettings.json: UnitTransformer 节，35kV/690V）</summary>
    public class UnitTransformerConfig
    {
        public const string Section = "UnitTransformer";

        public double RatedPower { get; set; } = 2500;
        public double PrimaryVoltage { get; set; } = 35000;
        public double SecondaryVoltage { get; set; } = 690;
        public double NoLoadLoss { get; set; } = 50;
        public double LoadLoss { get; set; } = 200;
        public double ImpedancePercent { get; set; } = 4;
        public double ReactiveVoltageInfluenceCoefficient { get; set; } = 1.0;
        public double NoLoadCurrentPercent { get; set; } = 2;

        public bool MagnetizingInrushEnabled { get; set; } = true;
        public double MagnetizingInrushDvDtThresholdPuPerSec { get; set; } = 0.8;
        public double MagnetizingInrushPeakExtraMultipleOfRatedPrimary { get; set; } = 4.0;
        public double MagnetizingInrushDecayTimeConstantSec { get; set; } = 0.45;
        public double MagnetizingInrushMaxExtraMultipleOfRatedPrimary { get; set; } = 12.0;
    }

    /// <summary>负载仿真配置（对应 appsettings.json: Load 节）</summary>
    public class LoadConfig
    {
        public const string Section = "Load";

        /// <summary>有功计划负载（kW，+放电/-用电）</summary>
        public double ActivePowerPlan { get; set; } = -500;

        /// <summary>无功计划负载（kvar）</summary>
        public double ReactivePowerPlan { get; set; } = 0;
    }

    /// <summary>220kV 并网点（PCC）电压模型，与并网电表同侧（对应 appsettings.json: Pcc 节）</summary>
    public class PccConfig
    {
        public const string Section = "Pcc";

        /// <summary>PCC 额定线电压（V），默认 220kV</summary>
        public double NominalLineVoltage { get; set; } = 220000;

        /// <summary>并网点等效短路容量（MVA），用于 Q-V 灵敏度</summary>
        public double ShortCircuitMva { get; set; } = 750;

        /// <summary>无功引起的电压偏移限幅（±%，相对额定）</summary>
        public double MaxVoltageShiftPercent { get; set; } = 5;

        /// <summary>无功-电压影响系数（1.0=按 Q/Ssc 标称）</summary>
        public double ReactiveVoltageInfluenceCoefficient { get; set; } = 1.0;

        /// <summary>站内 35kV 母线额定线电压（V），由 PCC 电压按变比推导</summary>
        public double StationBusNominalLineVoltage { get; set; } = 35000;
    }
}
