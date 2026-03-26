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
        public TransformerSimulator _transformer { get; set; }
        public ScheduledLoadSimulator _loadSimulator { get; set; }
        //private string modelName;

        public EnergyStorageSystem(SimulatorConfig simCfg, PcsPhysicalConfig pcsCfg, TransformerConfig transCfg, LoadConfig loadCfg)
        {
            var racks = new List<BatteryRackSimulator>();
            var pcsList = new List<PCSSimulator>();
            var bmsDeviceConfigs = simCfg.GetBmsDeviceConfigs();
            int unitCount = Math.Max(1, bmsDeviceConfigs.Count);

            for (int i = 0; i < unitCount; i++)
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
            for (int i = 0; i < unitCount; i++)
            {
                pcsList.Add(new PCSSimulator(
                    pcsConfig,
                    speedup: simCfg.Speedup,
                    gridLossCoefficient: pcsCfg.GridLossCoefficient));
            }

            _batteryRacks = racks;
            _pcsList      = pcsList;

            _breaker = new Breaker();

            // 变压器配置（从 TransformerConfig 读取）
            var transSpecs = new TransformerSpecifications
            {
                RatedPower            = transCfg.RatedPower,
                PrimaryVoltage        = transCfg.PrimaryVoltage,
                SecondaryVoltage      = transCfg.SecondaryVoltage,
                NoLoadLoss            = transCfg.NoLoadLoss,
                LoadLoss              = transCfg.LoadLoss,
                ImpedancePercent      = transCfg.ImpedancePercent,
                NoLoadCurrentPercent  = transCfg.NoLoadCurrentPercent
            };
            _transformer = new TransformerSimulator(transSpecs);

            // 负载配置（从 LoadConfig 读取）
            _loadSimulator = new ScheduledLoadSimulator(new List<LoadWindow>
            {
                new LoadWindow
                {
                    Start             = TimeSpan.Zero,
                    ActivePowerPlan   = loadCfg.ActivePowerPlan,
                    ReactivePowerPlan = loadCfg.ReactivePowerPlan
                }
            }, reactiveVoltageFeedbackCoefficient: loadCfg.ReactiveVoltageFeedbackCoefficient);

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

                    double totalSecCurrent = 0;
                    // 并网点功率约定：向电网送出为正，负载消耗视为负注入。
                    double totalActiveKw   = -_loadSimulator.ActivePower;
                    // 无功统一约定：
                    // - legacy符号（当前模型量）: 正=感性吸收
                    // - support符号（控制语义）: 正=支撑电压
                    double totalReactiveLegacyKvar = -_loadSimulator.ReactivePower;

                    foreach (var pcs in _pcsList)
                    {
                        var st = pcs.GetCurrentState();
                        totalSecCurrent += st.AcCurrent;
                        totalActiveKw   += pcs.GetGridSideActivePower();
                        totalReactiveLegacyKvar += st.ReactivePower;
                    }
                    // 统一测点：并网点线电压（PCC）取变压器二次侧电压。
                    var pccLineVoltageV = _transformer.GetCurrentState().SecondaryVoltage;
                    var loadCurrentA = _loadSimulator.ComputeLoadCurrentA(ref pccLineVoltageV);
                    totalSecCurrent += loadCurrentA;

                    var totalApparentKva = Math.Sqrt(totalActiveKw * totalActiveKw + totalReactiveLegacyKvar * totalReactiveLegacyKvar);
                    var powerFactor     = totalApparentKva > 0 ? totalActiveKw / totalApparentKva : 1.0;

                    var priCurrent = Math.Abs(_transformer.GetCurrentState().PrimaryCurrent);
                    _breaker.Update(priCurrent);

                    if (_breaker.IsClosed)
                    {
                        inputVoltage = (int)_transCfg.PrimaryVoltage;
                        _transformer.Update(inputVoltage, totalSecCurrent, powerFactor, totalApparentKva, simTime);
                        var secV = _transformer.GetCurrentState().SecondaryVoltage;
                        foreach (var pcs in _pcsList)
                        {
                            pcs.UpdateGridState(secV, _pcsCfg.FrequencyNominal, true);
                        }
                    }
                    else
                    {
                        inputVoltage    = 0;
                        totalSecCurrent = 0;
                        _transformer.Update(0, 0, powerFactor, totalApparentKva, simTime);
                        foreach (var pcs in _pcsList)
                        {
                            pcs.UpdateGridState(0, 0, false);
                            pcs.TransitionToMode(OperationMode.Standby);
                        }
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
