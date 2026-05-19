using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.Hosting;

namespace EssSimulator.EssDeviceSimModel
{
    using EssSimulator.Configuration;
    using EssSimulator.EssSimModelApi;
    using EssSimulator.EssSimModelApi.BatteryManagementSystem;
    using System;
    using System.Collections.Generic;
    using static EssSimulator.EssDeviceSimModel.PCSSimulator;
    using static EssSimulator.EssDeviceSimModel.TransformerSimulator;

    public class EnergyStorageSystem : BackgroundService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(EnergyStorageSystem));
        // 储能系统参数
        public double Capacity { get; private set; } // 储能容量 (kWh)
        public double CurrentEnergy { get; private set; } // 当前储能 (kWh)
        public double Efficiency { get; private set; } // 充放电效率 (0-1)

        // 统计数据
        public double TotalChargeEnergy { get; private set; } // 总充电能量 (kWh)
        public double TotalDischargeEnergy { get; private set; } // 总放电能量 (kWh)
        public List<double> ChargeSessions { get; private set; } // 单次充电能量记录
        public List<double> DischargeSessions { get; private set; } // 单次放电能量记录
        public Dictionary<DateTime, double> DailyCharge { get; private set; } // 日充电能量
        public Dictionary<DateTime, double> DailyDischarge { get; private set; } // 日放电能量
        public double AvailableChargeEnergy => Capacity - CurrentEnergy; // 可获得充电能量
        public double AvailableDischargeEnergy => CurrentEnergy; // 可获得放电能量

        // 当前充电/放电状态


        /// <summary>电池堆列表，索引 i 对应第 i+1 个储能单元。通过 ess._batteryRacks[i] 或路径 ess._batteryRacks[0] 访问。</summary>
        public IReadOnlyList<BatteryRackSimulator> _batteryRacks { get; }

        /// <summary>PCS 列表，索引 i 对应第 i+1 个 PCS。通过 ess._pcsList[i] 或路径 ess._pcsList[0] 访问。</summary>
        public IReadOnlyList<PCSSimulator> _pcsList { get; }

        /// <summary>兼容旧路径：ess._batteryRack 等价于 ess._batteryRacks[0]</summary>
        [Obsolete("请使用 _batteryRacks[0]")]
        public BatteryRackSimulator _batteryRack => _batteryRacks.Count > 0 ? _batteryRacks[0] : null!;

        /// <summary>兼容旧路径：ess._batteryRack2 等价于 ess._batteryRacks[1]</summary>
        [Obsolete("请使用 _batteryRacks[1]")]
        public BatteryRackSimulator _batteryRack2 => _batteryRacks.Count > 1 ? _batteryRacks[1] : null!;

        /// <summary>兼容旧路径：ess._pcs1 等价于 ess._pcsList[0]</summary>
        [Obsolete("请使用 _pcsList[0]")]
        public PCSSimulator _pcs1 => _pcsList.Count > 0 ? _pcsList[0] : null!;

        /// <summary>兼容旧路径：ess._pcs2 等价于 ess._pcsList[1]</summary>
        [Obsolete("请使用 _pcsList[1]")]
        public PCSSimulator _pcs2 => _pcsList.Count > 1 ? _pcsList[1] : null!;

        //public GridState _gridState;
        public Breaker _breaker { get; set; } //断路器
        public IReadOnlyList<Breaker> _unitBreakers { get; }                 // 单元变前高压断路器（每个 Unit 1 个）
        public TransformerSimulator _mainTransformer { get; set; }              // 220kV/35kV 主变
        public IReadOnlyList<TransformerSimulator> _unitTransformers { get; }   // 35kV/690V 单元变（每个 Unit 1 台）
        public ScheduledLoadSimulator _loadSimulator { get; set; }
        //private string modelName;

        public EnergyStorageSystem(
            SimulatorConfig simCfg,
            PcsPhysicalConfig pcsCfg,
            TransformerConfig transCfg,
            UnitTransformerConfig unitTransCfg,
            LoadConfig loadCfg)
        {
            var racks = new List<BatteryRackSimulator>();
            var pcsList = new List<PCSSimulator>();
            var bmsDeviceConfigs = simCfg.GetBmsDeviceConfigs();
            var pcsDeviceConfigs = simCfg.GetPcsDeviceConfigs();
            int channelCount = Math.Max(1, bmsDeviceConfigs.Count); // PCS/BMS 通道数（= Unit*2）
            int unitCount = Math.Max(1, simCfg.Devices?.Count ?? 1); // 储能单元数（每单元2路PCS+2路BMS）

            for (int i = 0; i < channelCount; i++)
            {
                var bmsCfg = bmsDeviceConfigs[i];
                var rackConfig = new RackConfiguration
                {
                    ClusterCount = bmsCfg.ClusterCount,
                    ClusterConfig = new ClusterConfiguration
                    {
                        PackCount = bmsCfg.PackCount,
                        PackConfig = new PackConfiguration
                        {
                            SeriesCount = bmsCfg.CellSeriesCount,
                            ParallelCount = bmsCfg.CellParallelCount,
                            NominalVoltage = bmsCfg.CellNominalVoltage,
                            NominalCapacity = bmsCfg.CellNominalCapacity,
                            InitialSoc = bmsCfg.CellInitialSoc,
                            InitialSocRandomRange = bmsCfg.CellInitialSocRandomRange,
                            PackInternalResistance = bmsCfg.PackInternalResistance
                        },
                        ClusterInternalResistance = bmsCfg.ClusterInternalResistance
                    },
                    RackInternalResistance = bmsCfg.RackInternalResistance
                };
                racks.Add(new BatteryRackSimulator(rackConfig));
            }

            var pcsConfig = new PcsConfiguration
            {
                RatedPower        = pcsCfg.RatedPower,
                MaxPower          = pcsCfg.MaxPower,
                Efficiency        = pcsCfg.Efficiency,
                DcVoltageRangeMin = pcsCfg.DcVoltageRangeMin,
                DcVoltageRangeMax = pcsCfg.DcVoltageRangeMax,
                AcVoltageNominal  = pcsCfg.AcVoltageNominal,
                FrequencyNominal  = pcsCfg.FrequencyNominal,
                MaxCurrent        = pcsCfg.MaxCurrent
            };
            for (int i = 0; i < channelCount; i++)
            {
                var pcsDeviceCfg = i < pcsDeviceConfigs.Count ? pcsDeviceConfigs[i] : new PcsDeviceConfig();
                var rampCfg = pcsDeviceCfg.PcsRamp ?? simCfg.Runtime.PcsRamp;
                pcsList.Add(new PCSSimulator(
                    pcsConfig,
                    speedup: simCfg.Speedup,
                    gridLossCoefficient: pcsCfg.GridLossCoefficient,
                    slope: rampCfg.Slope,
                    intervalMs: rampCfg.IntervalMs,
                    delayMs: rampCfg.DelayMs,
                    islandVfSlewRatePercentPerSecond: pcsCfg.IslandVfSlewRatePercentPerSecond,
                    islandVoltageStepFaultThresholdPercent: pcsCfg.IslandVoltageStepFaultThresholdPercent,
                    islandVoltageGridConflictThresholdPercent: pcsCfg.IslandVoltageGridConflictThresholdPercent,
                    blackStartActivePowerGainKwPerPercent: pcsCfg.BlackStartActivePowerGainKwPerPercent,
                    blackStartMaxActivePowerKw: pcsCfg.BlackStartMaxActivePowerKw,
                    blackStartMagnetizingPowerFraction: pcsCfg.BlackStartMagnetizingPowerFraction));
            }

            _batteryRacks = racks;
            _pcsList      = pcsList;

            _breaker = new Breaker();

            // 单元断路器（默认合闸，允许通过 emu.poweronoff 控制）
            var unitBreakers = new List<Breaker>();
            for (int u = 0; u < unitCount; u++)
            {
                var brk = new Breaker();
                brk.IsClosed = true;
                unitBreakers.Add(brk);
            }
            _unitBreakers = unitBreakers;

            // 主变（220kV/35kV）
            var mainSpecs = new TransformerSpecifications
            {
                RatedPower            = transCfg.RatedPower,
                PrimaryVoltage        = transCfg.PrimaryVoltage,
                SecondaryVoltage      = transCfg.SecondaryVoltage,
                NoLoadLoss            = transCfg.NoLoadLoss,
                LoadLoss              = transCfg.LoadLoss,
                ImpedancePercent      = transCfg.ImpedancePercent,
                ReactiveVoltageInfluenceCoefficient = transCfg.ReactiveVoltageInfluenceCoefficient,
                NoLoadCurrentPercent  = transCfg.NoLoadCurrentPercent,
                MagnetizingInrushEnabled = transCfg.MagnetizingInrushEnabled,
                MagnetizingInrushDvDtThresholdPuPerSec = transCfg.MagnetizingInrushDvDtThresholdPuPerSec,
                MagnetizingInrushPeakExtraMultipleOfRatedPrimary = transCfg.MagnetizingInrushPeakExtraMultipleOfRatedPrimary,
                MagnetizingInrushDecayTimeConstantSec = transCfg.MagnetizingInrushDecayTimeConstantSec,
                MagnetizingInrushMaxExtraMultipleOfRatedPrimary = transCfg.MagnetizingInrushMaxExtraMultipleOfRatedPrimary
            };
            _mainTransformer = new TransformerSimulator(mainSpecs);

            // 单元变（35kV/690V），每个 Unit 一台（每台带两路 PCS）
            var unitTransformers = new List<TransformerSimulator>();
            var unitSpecs = new TransformerSpecifications
            {
                RatedPower            = unitTransCfg.RatedPower,
                PrimaryVoltage        = unitTransCfg.PrimaryVoltage,
                SecondaryVoltage      = unitTransCfg.SecondaryVoltage,
                NoLoadLoss            = unitTransCfg.NoLoadLoss,
                LoadLoss              = unitTransCfg.LoadLoss,
                ImpedancePercent      = unitTransCfg.ImpedancePercent,
                ReactiveVoltageInfluenceCoefficient = unitTransCfg.ReactiveVoltageInfluenceCoefficient,
                NoLoadCurrentPercent  = unitTransCfg.NoLoadCurrentPercent,
                MagnetizingInrushEnabled = unitTransCfg.MagnetizingInrushEnabled,
                MagnetizingInrushDvDtThresholdPuPerSec = unitTransCfg.MagnetizingInrushDvDtThresholdPuPerSec,
                MagnetizingInrushPeakExtraMultipleOfRatedPrimary = unitTransCfg.MagnetizingInrushPeakExtraMultipleOfRatedPrimary,
                MagnetizingInrushDecayTimeConstantSec = unitTransCfg.MagnetizingInrushDecayTimeConstantSec,
                MagnetizingInrushMaxExtraMultipleOfRatedPrimary = unitTransCfg.MagnetizingInrushMaxExtraMultipleOfRatedPrimary
            };
            for (int u = 0; u < unitCount; u++)
            {
                unitTransformers.Add(new TransformerSimulator(unitSpecs));
            }
            _unitTransformers = unitTransformers;

            // 负载配置（从 LoadConfig 读取）
            _loadSimulator = new ScheduledLoadSimulator(new List<LoadWindow>
            {
                new LoadWindow
                {
                    Start             = TimeSpan.Zero,
                    ActivePowerPlan   = loadCfg.ActivePowerPlan,
                    ReactivePowerPlan = loadCfg.ReactivePowerPlan
                }
            });

            // 初始化统计数据
            TotalChargeEnergy   = 0;
            TotalDischargeEnergy = 0;
            ChargeSessions    = new List<double>();
            DischargeSessions = new List<double>();
            DailyCharge    = new Dictionary<DateTime, double>();
            DailyDischarge = new Dictionary<DateTime, double>();

            // 保存仿真步长参数，供 ExecuteAsync 使用
            _simStepMs = simCfg.SimStepMs;
            _speedup   = simCfg.Speedup;
            // _simStep = 真实休眠间隔 × 加速倍率，即每次 tick 推进的仿真时间量
            _simStep   = TimeSpan.FromMilliseconds(_simStepMs * _speedup);
            _transCfg  = transCfg;
            _pcsCfg    = pcsCfg;
        }

        // 仿真步长参数（由构造函数写入，ExecuteAsync 只读）
        // _simStepMs : 主循环的真实休眠间隔（ms），决定 CPU 占用率
        // _speedup   : 仿真加速倍率，传给所有物理模型的时间步长 = _simStepMs × _speedup
        // _simStep   : 传给物理模型的仿真时间步长（= _simStepMs × _speedup ms）
        private readonly int            _simStepMs;
        private readonly double         _speedup;
        private readonly TimeSpan       _simStep;
        private readonly TransformerConfig _transCfg;
        private readonly PcsPhysicalConfig _pcsCfg;

        /// <summary>
        /// 仿真主循环（IHostedService / BackgroundService）。
        /// 由 .NET Host 在 StartAsync 时调用，stoppingToken 取消时自动退出。
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _log.Info("[EnergyStorageSystem] 仿真主循环启动");
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_simStepMs));
            int inputVoltage = 0;
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    DateTime simTime = DateTime.Now;
                    var priCurrent = Math.Abs(_mainTransformer.GetCurrentState().PrimaryCurrent);
                    _breaker.Update(priCurrent);
                    _loadSimulator.SetPowered(_breaker.IsClosed);

                    // 35kV PCC（母线）测点：主变二次侧电压（用于负载与并网反馈）
                    var pccLineVoltageV = _mainTransformer.GetCurrentState().SecondaryVoltage;
                    // 调用一次以刷新负载时段计划（返回值不再作为并网总电流叠加）。
                    _ = _loadSimulator.ComputeLoadCurrentA(pccLineVoltageV);

                    // 并网点功率方向约定：+ 向电网送电（放电），- 从电网取电（用电）。
                    double totalActiveKw = _loadSimulator.ActivePower;
                    // 无功统一约定：正=升压支撑，负=降压作用。
                    double totalReactiveLegacyKvar = _loadSimulator.ReactivePower;
                    foreach (var pcs in _pcsList)
                    {
                        var st = pcs.GetCurrentState();
                        totalActiveKw += pcs.GetGridSideActivePower();
                        totalReactiveLegacyKvar += st.ReactivePower;
                    }

                    var totalApparentKva = Math.Sqrt(totalActiveKw * totalActiveKw + totalReactiveLegacyKvar * totalReactiveLegacyKvar);
                    var powerFactor     = totalApparentKva > 0 ? totalActiveKw / totalApparentKva : 1.0;
                    // 用并网点净 P/Q 反算总电流，避免支路标量电流累加引入伪环流。
                    double totalSecCurrentMag = pccLineVoltageV > 0
                        ? totalApparentKva * 1000.0 / (pccLineVoltageV * Math.Sqrt(3.0))
                        : 0;
                    double totalSecCurrent = Math.Abs(totalActiveKw) > 1e-6
                        ? (totalActiveKw >= 0 ? -totalSecCurrentMag : totalSecCurrentMag)
                        : totalSecCurrentMag;

                    if (_breaker.IsClosed)
                    {
                        inputVoltage = (int)_transCfg.PrimaryVoltage;
                        // 主变：220kV -> 35kV 母线
                        _mainTransformer.Update(inputVoltage, totalSecCurrent, powerFactor, totalApparentKva, totalReactiveLegacyKvar, simTime, _simStep);
                        var bus35kV = _mainTransformer.GetCurrentState().SecondaryVoltage;

                        // 单元变：35kV -> 690V（每个 Unit 1 台，带两路 PCS）
                        for (int u = 0; u < _unitTransformers.Count; u++)
                        {
                            int a = u * 2;
                            int b = u * 2 + 1;

                            // 单元断路器断开：单元变不带电；黑启动时 PCS 仍可在 PCS 侧离网建压
                            bool unitClosed = u < _unitBreakers.Count && _unitBreakers[u].IsClosed;
                            if (!unitClosed)
                            {
                                _unitTransformers[u].Update(0, 0, 1.0, 0, 0, simTime, _simStep);
                                if (a < _pcsList.Count)
                                    ApplyPcsGridWhenUnitDeenergized(_pcsList[a]);
                                if (b < _pcsList.Count)
                                    ApplyPcsGridWhenUnitDeenergized(_pcsList[b]);
                                continue;
                            }

                            double unitP = 0, unitQ = 0;
                            if (a < _pcsList.Count)
                            {
                                var st = _pcsList[a].GetCurrentState();
                                unitP += _pcsList[a].GetGridSideActivePower();
                                unitQ += st.ReactivePower;
                            }
                            if (b < _pcsList.Count)
                            {
                                var st = _pcsList[b].GetCurrentState();
                                unitP += _pcsList[b].GetGridSideActivePower();
                                unitQ += st.ReactivePower;
                            }

                            var unitS = Math.Sqrt(unitP * unitP + unitQ * unitQ);
                            var unitPf = unitS > 0 ? unitP / unitS : 1.0;
                            double unitSecCurrentMag = _pcsCfg.AcVoltageNominal > 0
                                ? unitS * 1000.0 / (_pcsCfg.AcVoltageNominal * Math.Sqrt(3.0))
                                : 0;
                            double unitSecCurrent = Math.Abs(unitP) > 1e-6
                                ? (unitP >= 0 ? -unitSecCurrentMag : unitSecCurrentMag)
                                : unitSecCurrentMag;

                            _unitTransformers[u].Update(bus35kV, unitSecCurrent, unitPf, unitS, unitQ, simTime, _simStep);
                            var lv690 = _unitTransformers[u].GetCurrentState().SecondaryVoltage;

                            // 仅更新电网侧电压/频率/可用性；运行模式 Off/Normal 由 EMS（emu.PcsList[].pcsOnOffSwitch）
                            // 经 PcsMapper.ApplyEmuCommands 统一驱动，此处不再每帧强制 Normal，否则会与“停机”命令来回打架。
                            if (a < _pcsList.Count)
                                _pcsList[a].UpdateGridState(lv690, _pcsCfg.FrequencyNominal, true);
                            if (b < _pcsList.Count)
                                _pcsList[b].UpdateGridState(lv690, _pcsCfg.FrequencyNominal, true);
                        }
                    }
                    else
                    {
                        inputVoltage    = 0;
                        totalSecCurrent = 0;
                        _mainTransformer.Update(0, 0, powerFactor, totalApparentKva, totalReactiveLegacyKvar, simTime, _simStep);
                        foreach (var xf in _unitTransformers)
                        {
                            xf.Update(0, 0, powerFactor, 0, 0, simTime, _simStep);
                        }
                        foreach (var pcs in _pcsList)
                            ApplyPcsGridWhenUnitDeenergized(pcs);
                    }

                    Update(simTime, _simStep);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常停止，不视为错误
                _log.Info("[EnergyStorageSystem] 仿真主循环收到停止信号，正常退出");
            }
            catch (Exception ex)
            {
                _log.Fatal("[EnergyStorageSystem] 仿真主循环发生未处理异常，已停止", ex);
                throw; // 重新抛出，让 Host 感知到服务崩溃
            }
        }

        /// <summary>
        /// 单元/主网侧无电：非黑启动则停机；黑启动保留离网建压（网侧不可用，由 EMS 启停+黑启动驱动）。
        /// </summary>
        private void ApplyPcsGridWhenUnitDeenergized(PCSSimulator pcs)
        {
            if (pcs.GetCurrentState().BlackStartEnabled)
            {
                pcs.UpdateGridState(0, _pcsCfg.FrequencyNominal, false);
                return;
            }

            pcs.UpdateGridState(0, 0, false);
            pcs.ApplyBlackStartEnabled(false);
            pcs.TransitionToMode(OperationMode.Off);
        }

        // 更新系统状态
        private void Update(DateTime simTime, TimeSpan step)
        {
            int n = Math.Min(_batteryRacks.Count, _pcsList.Count);
            for (int i = 0; i < n; i++)
            {
                var rackState = _batteryRacks[i].GetRackState();
                if (rackState == null) continue;

                _pcsList[i].Update(rackState.TotalVoltage, rackState.IsFault, simTime, step);
                // 电池内部电流方向：正充负放。PCS约定正放负充，因此对电池取负
                _batteryRacks[i].Update(-_pcsList[i].GetCurrentState().DcCurrent, 25.0, simTime, step);
            }
        }

        // // 示例使用
        // public void EssMain(string[] args)
        // {
        //     if (args[0] != null)
        //     {
        //         modelName = args[0];
        //     }   
        // }

        //private static void PrintSystemState(EnergyStorageSystem ess)
        //{
        //    DateTime dt = DateTime.Now;
        //    var pcsState = ess.GetPcsState();
        //    var rackState = ess.GetBatteryRackState();
        //    Console.WriteLine($"{modelName}\t{dt.ToString()}" + $"ActivePower:{pcsState.ActivePower:F1}kW\tMinClusterSOC:{rackState.MinClusterSOC * 100:F1}%\t" );
        //}
    }
}
