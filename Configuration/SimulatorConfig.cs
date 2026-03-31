using System.Collections.Generic;
using System.Linq;

namespace EssSimulator.Configuration
{
    public class RuntimeConfig
    {
        /// <summary>是否禁用控制台 GUI（无头模式）</summary>
        public bool NoGui { get; set; } = false;

        /// <summary>主循环真实休眠间隔（ms）</summary>
        public int SimStepMs { get; set; } = 200;

        /// <summary>仿真加速倍率</summary>
        public double Speedup { get; set; } = 100.0;

        /// <summary>PCS 功率爬坡参数</summary>
        public PcsRampConfig PcsRamp { get; set; } = new();
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
        public int SimStepMs => Runtime.SimStepMs;
        public double Speedup => Runtime.Speedup;
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

        public int ClusterCount => GetBmsDeviceConfigs().First().ClusterCount;
        public int PackCount => GetBmsDeviceConfigs().First().PackCount;
        public int CellSeriesCount => GetBmsDeviceConfigs().First().CellSeriesCount;
        public int CellParallelCount => GetBmsDeviceConfigs().First().CellParallelCount;
        public double CellNominalVoltage => GetBmsDeviceConfigs().First().CellNominalVoltage;
        public double CellNominalCapacity => GetBmsDeviceConfigs().First().CellNominalCapacity;
        public double CellInitialSoc => GetBmsDeviceConfigs().First().CellInitialSoc;
        public double CellInitialSocRandomRange => GetBmsDeviceConfigs().First().CellInitialSocRandomRange;
        public double PackInternalResistance => GetBmsDeviceConfigs().First().PackInternalResistance;
        public double ClusterInternalResistance => GetBmsDeviceConfigs().First().ClusterInternalResistance;
        public double RackInternalResistance => GetBmsDeviceConfigs().First().RackInternalResistance;
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
        /// <summary>并网点无功-电压影响系数（1.0=按阻抗标称影响）</summary>
        public double ReactiveVoltageInfluenceCoefficient { get; set; } = 1.0;
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

        public int ActiveReactivePriority { get; set; } = 1;
        public int FrequencyActiveSetting { get; set; } = 1;

        public float EmuMaxChargePower { get; set; } = 1000;
        public float EmuMaxDischargePower { get; set; } = 1000;
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
}
