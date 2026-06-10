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

        /// <summary>220kV 并网点（PCC）线电压（V），与并网电表、无功调压闭环一致。</summary>
        public double PccLineVoltageV { get; private set; }

        /// <summary>35kV 站内母线线电压（V），由 PCC 电压按额定变比推导。</summary>
        public double StationBus35LineVoltageV { get; private set; }

        private readonly PccConfig _pccCfg;

        public EnergyStorageSystem(
            SimulatorConfig simCfg,
            PcsPhysicalConfig pcsCfg,
            TransformerConfig transCfg,
            UnitTransformerConfig unitTransCfg,
            LoadConfig loadCfg,
            PccConfig pccCfg)
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
                    islandVoltageRampDurationMs: pcsCfg.IslandVoltageRampDurationMs,
                    blackStartActivePowerGainKwPerVolt: pcsCfg.BlackStartActivePowerGainKwPerVolt,
                    blackStartMaxActivePowerKw: pcsCfg.BlackStartMaxActivePowerKw,
                    blackStartMagnetizingPowerFraction: pcsCfg.BlackStartMagnetizingPowerFraction,
                    blackStartBusEnergizedFraction: pcsCfg.BlackStartBusEnergizedFraction));
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
            _pccCfg    = pccCfg;
            PccLineVoltageV = pccCfg.NominalLineVoltage;
            StationBus35LineVoltageV = pccCfg.StationBusNominalLineVoltage;
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

        public string GetBlackStartSteadyLossShareMode() => _pcsCfg.BlackStartSteadyLossShareMode;

        /// <summary>
        /// 仿真主循环（IHostedService / BackgroundService）。
        /// 由 .NET Host 在 StartAsync 时调用，stoppingToken 取消时自动退出。
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _log.Info("[EnergyStorageSystem] 仿真主循环启动");
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_simStepMs));
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    DateTime simTime = DateTime.Now;
                    var priCurrent = Math.Abs(_mainTransformer.GetCurrentState().PrimaryCurrent);
                    _breaker.Update(priCurrent);
                    _loadSimulator.SetPowered(_breaker.IsClosed);

                    // 并网点功率方向约定：+ 向电网送电（放电），- 从电网取电（用电）。
                    double totalActiveKw = _loadSimulator.ActivePower;
                    // 无功统一约定：正=升压支撑，负=降压作用（负载 + 储能，汇总到 220kV PCC）。
                    double totalReactiveLegacyKvar = _loadSimulator.ReactivePower;
                    foreach (var pcs in _pcsList)
                    {
                        var st = pcs.GetCurrentState();
                        totalActiveKw += pcs.GetGridSideActivePower();
                        totalReactiveLegacyKvar += st.ReactivePower;
                    }

                    if (_breaker.IsClosed)
                    {
                        PccLineVoltageV = GridFeedbackConventions.CalculatePccLineVoltage(
                            _pccCfg.NominalLineVoltage,
                            totalReactiveLegacyKvar,
                            _pccCfg.ShortCircuitMva,
                            _pccCfg.ReactiveVoltageInfluenceCoefficient,
                            _pccCfg.MaxVoltageShiftPercent);
                        StationBus35LineVoltageV = GridFeedbackConventions.DeriveStationBusVoltage(
                            PccLineVoltageV,
                            _pccCfg.NominalLineVoltage,
                            _pccCfg.StationBusNominalLineVoltage);
                    }
                    else
                    {
                        PccLineVoltageV = 0;
                        // 主断分闸时，35kV 母线可能仍被离网黑启动建压；此处先用上一步状态估算母线电压
                        StationBus35LineVoltageV = EstimateIslandedBus35LineVoltageV();
                    }

                    // 负载仍在 35kV 侧，用站内母线电压换算电流（返回值不叠加到主变二次电流）。
                    _ = _loadSimulator.ComputeLoadCurrentA(StationBus35LineVoltageV);

                    var totalApparentKva = Math.Sqrt(totalActiveKw * totalActiveKw + totalReactiveLegacyKvar * totalReactiveLegacyKvar);
                    var powerFactor     = totalApparentKva > 0 ? totalActiveKw / totalApparentKva : 1.0;
                    double totalSecCurrentMag = StationBus35LineVoltageV > 0
                        ? totalApparentKva * 1000.0 / (StationBus35LineVoltageV * Math.Sqrt(3.0))
                        : 0;
                    double totalSecCurrent = Math.Abs(totalActiveKw) > 1e-6
                        ? (totalActiveKw >= 0 ? -totalSecCurrentMag : totalSecCurrentMag)
                        : totalSecCurrentMag;

                    if (_breaker.IsClosed)
                    {
                        // 主变：一次侧 = 220kV PCC（无功调压）；二次侧 = 变比推导，避免重复 Q 抬压
                        _mainTransformer.Update(
                            PccLineVoltageV,
                            totalSecCurrent,
                            powerFactor,
                            totalApparentKva,
                            totalReactiveLegacyKvar,
                            simTime,
                            _simStep,
                            applyReactiveVoltageShift: false);
                        _mainTransformer._currentState.SecondaryVoltage = StationBus35LineVoltageV;
                        var bus35kV = StationBus35LineVoltageV;

                        // 单元变：35kV -> 690V（每个 Unit 1 台，带两路 PCS）
                        for (int u = 0; u < _unitTransformers.Count; u++)
                        {
                            int a = u * 2;
                            int b = u * 2 + 1;

                            // 单元断路器分闸：35kV 一次侧无网；黑启动/离网 V/f 在 PCS 侧建压（见 SyncUnitTransformerAfterPcsUpdate）
                            bool unitClosed = u < _unitBreakers.Count && _unitBreakers[u].IsClosed;
                            if (!unitClosed)
                            {
                                if (a < _pcsList.Count)
                                    ApplyPcsGridWhenUnitDeenergized(a, _pcsList[a]);
                                if (b < _pcsList.Count)
                                    ApplyPcsGridWhenUnitDeenergized(b, _pcsList[b]);
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
                        // 主断分闸：220kV 侧断电。
                        // 若 35kV 母线被黑启动带电，则主变仍会被二次侧反向励磁（含暂态涌流），
                        // 其励磁无功应由黑启动 PCS 承担。
                        if (StationBus35LineVoltageV > 1.0)
                        {
                            var ms = _mainTransformer._specs;
                            double tr = ms.SecondaryVoltage > 0 ? ms.PrimaryVoltage / ms.SecondaryVoltage : 1.0;
                            double primaryEqV = StationBus35LineVoltageV * tr;
                            _mainTransformer.Update(
                                primaryEqV,
                                totalSecCurrent,
                                powerFactor,
                                totalApparentKva,
                                totalReactiveLegacyKvar,
                                simTime,
                                _simStep,
                                applyReactiveVoltageShift: false);
                            _mainTransformer._currentState.SecondaryVoltage = StationBus35LineVoltageV;
                            // 主断分闸时忽略220kV侧寄生/位移电流显示，一次侧电流固定为0A。
                            _mainTransformer._currentState.PrimaryCurrent = 0;
                        }
                        else
                        {
                            _mainTransformer.Update(0, 0, powerFactor, totalApparentKva, totalReactiveLegacyKvar, simTime, _simStep, applyReactiveVoltageShift: false);
                            _mainTransformer._currentState.SecondaryVoltage = 0;
                        }

                        // 真实黑启动顺序：主断分 → 单元高压合 → 黑启动；仅「主断合+单元合+黑启动」由 BlackStartSafety 禁止
                        for (int u = 0; u < _unitTransformers.Count; u++)
                        {
                            int a = u * 2;
                            int b = u * 2 + 1;
                            bool unitEnergized = u < _unitBreakers.Count && _unitBreakers[u].IsClosed;
                            if (!unitEnergized)
                            {
                                _unitTransformers[u].Update(0, 0, powerFactor, 0, 0, simTime, _simStep);
                                if (a < _pcsList.Count)
                                    ApplyPcsGridWhenUnitDeenergized(a, _pcsList[a]);
                                if (b < _pcsList.Count)
                                    ApplyPcsGridWhenUnitDeenergized(b, _pcsList[b]);
                                continue;
                            }

                            if (a < _pcsList.Count)
                                ApplyPcsGridWhenUnitDeenergized(a, _pcsList[a]);
                            if (b < _pcsList.Count)
                                ApplyPcsGridWhenUnitDeenergized(b, _pcsList[b]);
                        }
                    }

                    SyncUnitTransformerAfterPcsUpdate(simTime, _simStep);
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
        private void ApplyPcsGridWhenUnitDeenergized(int pcsSimIndex, PCSSimulator pcs)
        {
            int unit = pcsSimIndex / 2;
            double busV = GetUnitAcBusVoltage(unit);
            double energizedV = _pcsCfg.AcVoltageNominal * _pcsCfg.BlackStartBusEnergizedFraction;
            if (busV >= energizedV)
            {
                pcs.UpdateGridState(busV, _pcsCfg.FrequencyNominal, false);
                return;
            }

            if (pcs.GetCurrentState().BlackStartEnabled)
            {
                pcs.UpdateGridState(0, _pcsCfg.FrequencyNominal, false);
                return;
            }

            pcs.UpdateGridState(0, 0, false);
            pcs.ApplyBlackStartEnabled(false);
            pcs.TransitionToMode(OperationMode.Off);
        }

        /// <summary>PCS 是否处于离网 V/f 建压（黑启动或孤岛电压有效）。</summary>
        private static bool IsPcsIslandVoltageBuilding(PcsState st)
        {
            if (st.Mode != OperationMode.Normal || st.GMode != GridMode.Islanded)
                return false;
            if (st.BlackStartEnabled)
                return st.BlackStartPhase is BlackStartPhase.VoltageBuilding or BlackStartPhase.Synchronized;
            return st.IslandVoltageEffectiveV > 1.0;
        }

        /// <summary>
        /// 在主断分闸时估算离网 35kV 母线电压：取所有“单元高压合 + 正在离网建压”PCS 的最高二次侧电压，
        /// 再按单元变变比折算到 35kV 侧。
        /// </summary>
        private double EstimateIslandedBus35LineVoltageV()
        {
            double bus35 = 0;
            for (int u = 0; u < _unitTransformers.Count; u++)
            {
                if (u >= _unitBreakers.Count || !_unitBreakers[u].IsClosed)
                    continue;

                int a = u * 2;
                int b = a + 1;
                double lv690 = 0;

                void Acc(int idx)
                {
                    if (idx < 0 || idx >= _pcsList.Count) return;
                    var st = _pcsList[idx].GetCurrentState();
                    if (!IsPcsIslandVoltageBuilding(st)) return;
                    lv690 = Math.Max(lv690, st.AcVoltage);
                }

                Acc(a);
                Acc(b);
                if (lv690 <= 0)
                    continue;

                var specs = _unitTransformers[u]._specs;
                double turnsRatio = specs.SecondaryVoltage > 0
                    ? specs.PrimaryVoltage / specs.SecondaryVoltage
                    : 1.0;
                bus35 = Math.Max(bus35, lv690 * turnsRatio);
            }
            return bus35;
        }

        /// <summary>同单元 690V 母线电压（单元变二次侧与各 PCS 交流电压取大）。</summary>
        public double GetUnitAcBusVoltage(int unitIndex)
        {
            double v = 0;
            int a = unitIndex * 2;
            int b = a + 1;
            if (unitIndex >= 0 && unitIndex < _unitTransformers.Count)
                v = Math.Max(v, _unitTransformers[unitIndex].GetCurrentState().SecondaryVoltage);
            if (a >= 0 && a < _pcsList.Count)
                v = Math.Max(v, _pcsList[a].GetCurrentState().AcVoltage);
            if (b >= 0 && b < _pcsList.Count)
                v = Math.Max(v, _pcsList[b].GetCurrentState().AcVoltage);
            return v;
        }

        private void RefreshUnitBlackStartBusContext(int unitIndex)
        {
            double busV = GetUnitAcBusVoltage(unitIndex);
            int a = unitIndex * 2;
            int b = a + 1;
            if (a >= 0 && a < _pcsList.Count)
                _pcsList[a].RefreshBlackStartBusContext(busV);
            if (b >= 0 && b < _pcsList.Count)
                _pcsList[b].RefreshBlackStartBusContext(busV);
        }

        /// <summary>
        /// PCS.Update 之后校正单元变状态：
        /// - 主断分时，任一单元建压会在 35kV 母线上形成共享电压；
        /// - 共享母线仅作用于已合闸单元变的一次侧受电，不直接把待机 PCS 拉成“有网可并”；
        /// - 由单元变汇总出的站用电（励磁/损耗）再在黑启动在线 PCS 间近似均分。
        /// </summary>
        private void SyncUnitTransformerAfterPcsUpdate(DateTime simTime, TimeSpan simStep)
        {
            bool gridFeeds35kVBus = _breaker.IsClosed;
            int unitCount = _unitTransformers.Count;
            if (unitCount == 0)
                return;

            var unitHvClosed = new bool[unitCount];
            var localUnitP = new double[unitCount];
            var localUnitQ = new double[unitCount];
            var localLv690 = new double[unitCount];
            var unitPrimaryV = new double[unitCount];

            double sharedBus35kVFromIsland = 0;

            // 1) 收集每单元本地建压与功率
            for (int u = 0; u < unitCount; u++)
            {
                int a = u * 2;
                int b = u * 2 + 1;
                unitHvClosed[u] = u < _unitBreakers.Count && _unitBreakers[u].IsClosed;
                if (!unitHvClosed[u])
                    continue;

                void Accumulate(int pcsIdx)
                {
                    if (pcsIdx < 0 || pcsIdx >= _pcsList.Count) return;
                    var st = _pcsList[pcsIdx].GetCurrentState();
                    if (!IsPcsIslandVoltageBuilding(st)) return;
                    localLv690[u] = Math.Max(localLv690[u], st.AcVoltage);
                    localUnitP[u] += _pcsList[pcsIdx].GetGridSideActivePower();
                    localUnitQ[u] += st.ReactivePower;
                }

                Accumulate(a);
                Accumulate(b);

                if (gridFeeds35kVBus || localLv690[u] <= 0)
                    continue;

                var specs = _unitTransformers[u]._specs;
                double turnsRatio = specs.SecondaryVoltage > 0
                    ? specs.PrimaryVoltage / specs.SecondaryVoltage
                    : 1.0;
                sharedBus35kVFromIsland = Math.Max(sharedBus35kVFromIsland, localLv690[u] * turnsRatio);
            }

            // 2) 将 35kV 母线电压传播到各已合闸单元变（仅变压器受电）
            for (int u = 0; u < unitCount; u++)
            {
                if (!unitHvClosed[u])
                {
                    _unitTransformers[u].Update(0, 0, 1.0, 0, 0, simTime, simStep);
                    continue;
                }

                var specs = _unitTransformers[u]._specs;
                double turnsRatio = specs.SecondaryVoltage > 0
                    ? specs.PrimaryVoltage / specs.SecondaryVoltage
                    : 1.0;

                unitPrimaryV[u] = gridFeeds35kVBus
                    ? StationBus35LineVoltageV
                    : sharedBus35kVFromIsland;

                if (unitPrimaryV[u] <= 0)
                {
                    _unitTransformers[u].Update(0, 0, 1.0, 0, 0, simTime, simStep);
                    continue;
                }

                double secV = Math.Max(unitPrimaryV[u] / Math.Max(turnsRatio, 1e-6), 1.0);
                double unitS = Math.Sqrt(localUnitP[u] * localUnitP[u] + localUnitQ[u] * localUnitQ[u]);
                double unitPf = unitS > 0 ? localUnitP[u] / unitS : 1.0;
                double unitSecCurrentMag = unitS * 1000.0 / (secV * Math.Sqrt(3.0));
                double unitSecCurrent = Math.Abs(localUnitP[u]) > 1e-6
                    ? (localUnitP[u] >= 0 ? -unitSecCurrentMag : unitSecCurrentMag)
                    : unitSecCurrentMag;

                _unitTransformers[u].Update(
                    unitPrimaryV[u], unitSecCurrent, unitPf, unitS, localUnitQ[u], simTime, simStep,
                    applyReactiveVoltageShift: false);
            }

            // 3) 汇总站用电并在全母线黑启动在线 PCS 间均分
            ApplyBlackStartStationElectricalLoadAcrossBus(localUnitP, unitPrimaryV);
        }

        /// <summary>
        /// 汇总全 35kV 母线已受电单元变的励磁/损耗需求，并在黑启动在线 PCS 间近似均分。
        /// </summary>
        private void ApplyBlackStartStationElectricalLoadAcrossBus(double[] localUnitP, double[] unitPrimaryV)
        {
            double totalMagQ = 0;
            double totalLossP = 0;
            double lineCoeff = Math.Clamp(_pcsCfg.GridLossCoefficient, 0, 0.5);

            for (int u = 0; u < _unitTransformers.Count; u++)
            {
                if (u >= unitPrimaryV.Length || unitPrimaryV[u] <= 0)
                    continue;
                var xf = _unitTransformers[u];
                totalMagQ += xf.GetSecondaryMagnetizingReactiveKvar();
                double ironKw = xf.GetSecondaryNoLoadActivePowerKw();
                double lineKw = Math.Abs(localUnitP[u]) * lineCoeff / Math.Max(1e-6, 1.0 - lineCoeff);
                totalLossP += ironKw + lineKw;
            }

            // 主断分闸且 35kV 母线被离网建压时，主变由二次侧反向励磁：
            // 其暂态/稳态励磁无功同样由黑启动 PCS 承担。
            if (!_breaker.IsClosed && StationBus35LineVoltageV > 1.0)
                totalMagQ += _mainTransformer.GetSecondaryMagnetizingReactiveKvar();

            var participants = new List<int>();
            for (int i = 0; i < _pcsList.Count; i++)
            {
                var st = _pcsList[i].GetCurrentState();
                if (st.BlackStartEnabled && IsPcsIslandVoltageBuilding(st))
                    participants.Add(i);
            }

            for (int i = 0; i < _pcsList.Count; i++)
            {
                _pcsList[i].SetTransformerMagnetizingReactiveKvar(0);
                _pcsList[i].SetBlackStartSharedLossActivePowerKw(0);
            }

            if (participants.Count == 0)
                return;

            double qEach = totalMagQ / participants.Count;
            double pEach = totalLossP / participants.Count;
            foreach (var idx in participants)
            {
                _pcsList[idx].SetTransformerMagnetizingReactiveKvar(qEach);
                _pcsList[idx].SetBlackStartSharedLossActivePowerKw(pEach);
            }
        }

        // 更新系统状态
        private void Update(DateTime simTime, TimeSpan step)
        {
            int unitCount = (_pcsList.Count + 1) / 2;
            for (int u = 0; u < unitCount; u++)
                RefreshUnitBlackStartBusContext(u);

            int n = Math.Min(_batteryRacks.Count, _pcsList.Count);
            for (int i = 0; i < n; i++)
            {
                var rackState = _batteryRacks[i].GetRackState();
                if (rackState == null) continue;

                if (rackState.IsPcsLinked)
                {
                    _pcsList[i].Update(rackState.TotalVoltage, rackState.IsFault, simTime, step);
                    // 电池内部电流方向：正充负放。PCS约定正放负充，因此对电池取负
                    _batteryRacks[i].Update(-_pcsList[i].GetCurrentState().DcCurrent, 25.0, simTime, step);
                }
                else
                {
                    // BMS 离网：PCS 直流侧失电，电池侧无 PCS 电流
                    _pcsList[i].Update(0, 0, simTime, step);
                    _batteryRacks[i].Update(0, 25.0, simTime, step);
                }
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
