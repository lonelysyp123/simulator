using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEC61850_simulatorServer2.EssDeviceSimModel
{
    using IEC61850_simulatorServer2.EssSimModelApi;
    using IEC61850_simulatorServer2.EssSimModelApi.BatteryManagementSystem;
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Security.Cryptography.Xml;
    using static IEC61850_simulatorServer2.EssDeviceSimModel.PCSSimulator;
    using static IEC61850_simulatorServer2.EssDeviceSimModel.TransformerSimulator;

    public class EnergyStorageSystem
    {
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
        private bool isCharging = false;
        private bool isDischarging = false;
        private DateTime currentDay;
        private DateTime lastOperationTime;


        public  BatteryRackSimulator _batteryRack { get; set; }

        public BatteryRackSimulator _batteryRack2 { get; set; }
        public PCSSimulator _pcs1 { get; set; }

        //public readonly BatteryRackSimulator _batteryRack2;
        public PCSSimulator _pcs2 { get; set; }

        //public GridState _gridState;
        private DateTime _lastUpdateTime;
        public Breaker _breaker { get; set; } //断路器
        public TransformerSimulator _transformer { get; set; }
        public ScheduledLoadSimulator _loadSimulator { get; set; }
        //private string modelName;

        public EnergyStorageSystem(string? name, int clusterCount, int packCount)
        {
            // 创建电池堆
            var rackConfig = new RackConfiguration
            {
                ClusterCount = clusterCount,
                ClusterConfig = new ClusterConfiguration
                {
                    PackCount = packCount,
                    PackConfig = new PackConfiguration
                    {
                        SeriesCount = 104,
                        ParallelCount = 1,
                        NominalVoltage = 3.2,
                        NominalCapacity = 314,
                        PackInternalResistance = 0.05
                    },
                    ClusterInternalResistance = 0.1
                },
                RackInternalResistance = 0.02
            };
            _batteryRack = new BatteryRackSimulator(rackConfig);
            _batteryRack2 = new BatteryRackSimulator(rackConfig);

            // 创建PCS配置 (1250) EH-1250-HB-UD
            var pcsConfig = new PcsConfiguration
            {
                RatedPower = 1725,
                MaxPower = 1897.5,
                Efficiency = 0.99,
                DcVoltageRangeMin = 1000,
                DcVoltageRangeMax = 1500,
                AcVoltageNominal = 690,
                FrequencyNominal = 50,
                MaxCurrent = 1588
            };
            _pcs1 = new PCSSimulator(pcsConfig);
            _pcs2 = new PCSSimulator(pcsConfig);
            //创建一个主断路器
            _breaker = new Breaker();
            //创建一个变压器
            var specs = new TransformerSpecifications
            {
                RatedPower = 2500,        // 2500kVA
                PrimaryVoltage = 10500,     // 690V
                SecondaryVoltage = 690,   // 230V
                NoLoadLoss = 50,          // 50W
                LoadLoss = 200,           // 200W
                ImpedancePercent = 4,     // 4%
                NoLoadCurrentPercent = 2  // 2%
            };
            _transformer = new TransformerSimulator(specs);
            var loadConfigls = new List<LoadWindow>();
            var loadConfig = new LoadWindow
            {
                Start = TimeSpan.Zero,
                ActivePowerPlan = 500,
                ReactivePowerPlan = 0
            };
            loadConfigls.Add(loadConfig);
            _loadSimulator = new ScheduledLoadSimulator(loadConfigls);
         
            // 初始化统计数据
            TotalChargeEnergy = 0;
            TotalDischargeEnergy = 0;
            ChargeSessions = new List<double>();
            DischargeSessions = new List<double>();
            DailyCharge = new Dictionary<DateTime, double>();
            DailyDischarge = new Dictionary<DateTime, double>();

            currentDay = DateTime.Today;
            _lastUpdateTime = DateTime.Now;
            //this.modelName = name;

            // 仿真主循环线程：以固定时间步推进断路器、变压器、PCS 与电池模型的状态
            Thread thread = new Thread(() =>
            {
                int timeIntervalInMs = 200; // 线程实际sleep时间
                double speedup = 10.0; // 仿真加速倍率
                var step = TimeSpan.FromMilliseconds(timeIntervalInMs * speedup); // 仿真推进步长
                var inputTransfromerVol = 0;
                while (true)
                {
                    DateTime simTime = DateTime.Now;
                    // 1) 采样当前两台 PCS 状态
                    var pcs1State = _pcs1.GetCurrentState();
                    var pcs2State = _pcs2.GetCurrentState();
                    // 1.1) 叠加外部负载，计算二次侧总电流
                    var secVoltageForLoad = _transformer.GetCurrentState().SecondaryVoltage;
                    var _currentLoadCurrentA = _loadSimulator.ComputeLoadCurrentA(ref secVoltageForLoad);
                    double totalSecCurrent = pcs1State.AcCurrent + pcs2State.AcCurrent + _currentLoadCurrentA;
                    // 1.2) 计算功率因数
                    var totalActivePowerKw = pcs1State.ActivePower + pcs2State.ActivePower + _loadSimulator.ActivePower;
                    var totalReactivePowerKvar = pcs1State.ReactivePower + pcs2State.ReactivePower + _loadSimulator.ReactivePower;
                    var totalApparentPowerKva = Math.Sqrt(totalActivePowerKw * totalActivePowerKw + totalReactivePowerKvar * totalReactivePowerKvar);
                    var powerFactor = totalApparentPowerKva > 0 ? totalActivePowerKw / totalApparentPowerKva : 1.0;

                    // 3) 用一次侧电流更新断路器状态
                    var priCurrent = Math.Abs(_transformer.GetCurrentState().PrimaryCurrent); // 一次侧电流
                    _breaker.Update(priCurrent);

                    // 4) 根据断路器状态更新PCS和变压器
                    if (_breaker.IsClosed)
                    {
                        // 断路器合闸，PCS并网，接通主回路
                        inputTransfromerVol = 10500;
                        _transformer.Update(inputTransfromerVol, totalSecCurrent, powerFactor, totalApparentPowerKva, simTime);
                        _pcs1.UpdateGridState(_transformer.GetCurrentState().SecondaryVoltage, 50, true);
                        _pcs2.UpdateGridState(_transformer.GetCurrentState().SecondaryVoltage, 50, true);
                    }
                    else
                    {
                        // 断路器分闸，断开主回路
                        inputTransfromerVol = 0;
                        totalSecCurrent = 0;
                        _transformer.Update(inputTransfromerVol, totalSecCurrent, powerFactor, totalApparentPowerKva, simTime);
                        _pcs1.UpdateGridState(0, 0, false);
                        _pcs1.TransitionToMode(OperationMode.Standby);
                        _pcs2.UpdateGridState(0, 0, false);
                        _pcs2.TransitionToMode(OperationMode.Standby);
                    }
                    // 5) 推进设备内部状态
                    Update(simTime, step);
                    // 6) sleep 固定200ms，不变
                    var remainingMs = timeIntervalInMs - (DateTime.Now - simTime).TotalMilliseconds;
                    if (remainingMs > 0)
                    {
                        Thread.Sleep((int)remainingMs);
                    }
                }
            });
            thread.Start();
        }
        
        // 更新系统状态
        private void Update(DateTime simTime, TimeSpan step)
        {
            // 更新时间
            _lastUpdateTime = simTime;

            var rackState1 = _batteryRack.GetRackState();
            var rackState2 = _batteryRack2.GetRackState();
            if (rackState1 == null || rackState2 == null)
            {
                return;
            }
            // 设备可根据需要选择用simTime或step
            _pcs1.Update(rackState1.TotalVoltage, rackState1.IsFault, simTime, step);
            _pcs2.Update(rackState2.TotalVoltage, rackState2.IsFault, simTime, step);
            _batteryRack.Update(_pcs1.GetCurrentState().DcCurrent, 25.0, simTime, step);
            _batteryRack2.Update(_pcs2.GetCurrentState().DcCurrent, 25.0, simTime, step);

            //统计量计算
            //UpdateEnergyStorage(_pcsCurrentState);
        }
     
        private void UpdateEnergyStorage(PcsState pcsCurrent)
        {
            DateTime now = DateTime.Now;
            double timeElapsed = (now - lastOperationTime).TotalHours;

            // 检查是否跨天
            if (now.Date != currentDay)
            {
                if (!DailyCharge.ContainsKey(currentDay))
                    DailyCharge[currentDay] = 0;

                if (!DailyDischarge.ContainsKey(currentDay))
                    DailyDischarge[currentDay] = 0;

                currentDay = now.Date;
            }

            if (isCharging)
            {
                double potentialCharge = pcsCurrent.ActivePower * timeElapsed;
                double actualCharge = Math.Min(potentialCharge, AvailableChargeEnergy) * Efficiency;

                CurrentEnergy += actualCharge;
                TotalChargeEnergy += actualCharge;

                // 记录单次充电会话
                if (ChargeSessions.Count == 0 || !isCharging)
                    ChargeSessions.Add(0);
                ChargeSessions[ChargeSessions.Count - 1] += actualCharge;

                // 更新日充电量
                if (!DailyCharge.ContainsKey(currentDay))
                    DailyCharge[currentDay] = 0;
                DailyCharge[currentDay] += actualCharge;
            }
            else if (isDischarging)
            {
                double potentialDischarge = pcsCurrent.ActivePower * timeElapsed;
                double actualDischarge = Math.Min(potentialDischarge, AvailableDischargeEnergy);

                CurrentEnergy -= actualDischarge;
                TotalDischargeEnergy += actualDischarge;

                // 记录单次放电会话
                if (DischargeSessions.Count == 0 || !isDischarging)
                    DischargeSessions.Add(0);
                DischargeSessions[DischargeSessions.Count - 1] += actualDischarge;

                // 更新日放电量
                if (!DailyDischarge.ContainsKey(currentDay))
                    DailyDischarge[currentDay] = 0;
                DailyDischarge[currentDay] += actualDischarge;
            }

            lastOperationTime = now;
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
