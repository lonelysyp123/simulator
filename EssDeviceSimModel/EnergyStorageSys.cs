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


        public  BatteryRackSimulator _batteryRack { get; set; }

        public BatteryRackSimulator _batteryRack2 { get; set; }
        public PCSSimulator _pcs1 { get; set; }

        //public readonly BatteryRackSimulator _batteryRack2;
        public PCSSimulator _pcs2 { get; set; }

        //public GridState _gridState;
        public Breaker _breaker { get; set; } //断路器
        public TransformerSimulator _transformer { get; set; }
        public ScheduledLoadSimulator _loadSimulator { get; set; }
        //private string modelName;

        public EnergyStorageSystem(SimulatorConfig simCfg, PcsPhysicalConfig pcsCfg, TransformerConfig transCfg, LoadConfig loadCfg)
        {
            // 电池堆配置（从 SimulatorConfig 读取）
            var rackConfig = new RackConfiguration
            {
                ClusterCount = simCfg.ClusterCount,
                ClusterConfig = new ClusterConfiguration
                {
                    PackCount = simCfg.PackCount,
                    PackConfig = new PackConfiguration
                    {
                        SeriesCount             = simCfg.CellSeriesCount,
                        ParallelCount           = simCfg.CellParallelCount,
                        NominalVoltage          = simCfg.CellNominalVoltage,
                        NominalCapacity         = simCfg.CellNominalCapacity,
                        PackInternalResistance   = simCfg.PackInternalResistance
                    },
                    ClusterInternalResistance = simCfg.ClusterInternalResistance
                },
                RackInternalResistance = simCfg.RackInternalResistance
            };
            _batteryRack  = new BatteryRackSimulator(rackConfig);
            _batteryRack2 = new BatteryRackSimulator(rackConfig);

            // PCS 配置（从 PcsPhysicalConfig 读取）
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
            _pcs1 = new PCSSimulator(pcsConfig, speedup: simCfg.Speedup);
            _pcs2 = new PCSSimulator(pcsConfig, speedup: simCfg.Speedup);

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

                    var pcs1State = _pcs1.GetCurrentState();
                    var pcs2State = _pcs2.GetCurrentState();

                    var secVoltage   = _transformer.GetCurrentState().SecondaryVoltage;
                    var loadCurrentA = _loadSimulator.ComputeLoadCurrentA(ref secVoltage);
                    double totalSecCurrent = pcs1State.AcCurrent + pcs2State.AcCurrent + loadCurrentA;

                    var totalActiveKw     = pcs1State.ActivePower   + pcs2State.ActivePower   + _loadSimulator.ActivePower;
                    var totalReactiveKvar = pcs1State.ReactivePower + pcs2State.ReactivePower + _loadSimulator.ReactivePower;
                    var totalApparentKva  = Math.Sqrt(totalActiveKw * totalActiveKw + totalReactiveKvar * totalReactiveKvar);
                    var powerFactor       = totalApparentKva > 0 ? totalActiveKw / totalApparentKva : 1.0;

                    var priCurrent = Math.Abs(_transformer.GetCurrentState().PrimaryCurrent);
                    _breaker.Update(priCurrent);

                    if (_breaker.IsClosed)
                    {
                        inputVoltage = (int)_transCfg.PrimaryVoltage;
                        _transformer.Update(inputVoltage, totalSecCurrent, powerFactor, totalApparentKva, simTime);
                        var secV = _transformer.GetCurrentState().SecondaryVoltage;
                        _pcs1.UpdateGridState(secV, _pcsCfg.FrequencyNominal, true);
                        _pcs2.UpdateGridState(secV, _pcsCfg.FrequencyNominal, true);
                    }
                    else
                    {
                        inputVoltage    = 0;
                        totalSecCurrent = 0;
                        _transformer.Update(0, 0, powerFactor, totalApparentKva, simTime);
                        _pcs1.UpdateGridState(0, 0, false);
                        _pcs1.TransitionToMode(OperationMode.Standby);
                        _pcs2.UpdateGridState(0, 0, false);
                        _pcs2.TransitionToMode(OperationMode.Standby);
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
            var rackState1 = _batteryRack.GetRackState();
            var rackState2 = _batteryRack2.GetRackState();
            if (rackState1 == null || rackState2 == null)
            {
                return;
            }
            // 设备可根据需要选择用simTime或step
            _pcs1.Update(rackState1.TotalVoltage, rackState1.IsFault, simTime, step);
            _pcs2.Update(rackState2.TotalVoltage, rackState2.IsFault, simTime, step);
            // 电池内部电流方向：正充负放。PCS约定正放负充，因此对电池取负
            _batteryRack.Update(-_pcs1.GetCurrentState().DcCurrent, 25.0, simTime, step);
            _batteryRack2.Update(-_pcs2.GetCurrentState().DcCurrent, 25.0, simTime, step);

            //统计量计算
            //UpdateEnergyStorage(_pcsCurrentState);
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
