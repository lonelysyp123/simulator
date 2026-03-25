using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EssSimulator.EssDeviceSimModel
{
    // 功率爬坡曲线模式
    public enum RampCurve
    {
        Linear,      // y = a * x
        Quadratic,   // y = a * x^2
        SquareRoot   // y = a * sqrt(x)
    }

    // PCS运行模式
    public enum OperationMode
    {
        Off,            // 停机
        Standby,        // 待机
        Normal,         // 正常运行
    }

    // 并离网模式
    public enum GridMode
    {
        GridConnected,  // 并网
        Islanded        // 离网
    }
    
    // PCS配置参数
    public class PcsConfiguration
    {
        public double RatedPower { get; set; }          // 额定功率(kVA)
        public double MaxPower { get; set; }            // 最大过载功率(kW)
        public double Efficiency { get; set; }          // 转换效率(0-1)
        public double DcVoltageRangeMin { get; set; }   // 直流电压范围最小值(V)
        public double DcVoltageRangeMax { get; set; }   // 直流电压范围最大值(V)
        public double AcVoltageNominal { get; set; }    // 交流额定电压(V)
        public double FrequencyNominal { get; set; }    // 额定频率(Hz)
        public double MaxCurrent { get; set; }          // 最大输出电流(A)
    }

    // PCS实时状态
    public class PcsState
    {
        public OperationMode Mode { get; set; }         // 当前运行模式
        public GridMode GMode { get; set; }             // 并离网模式
        public double DcVoltage { get; set; }           // 直流侧电压(V)
        public double DcCurrent { get; set; }           // 直流侧电流(A)
        public double AcVoltage { get; set; }           // 交流侧电压(V)
        public double AcCurrent { get; set; }           // 交流侧电流(A)
        public double ActivePower { get; set; }         // 有功功率(kW)
        public double ReactivePower { get; set; }        // 无功功率(kvar, legacy符号: 正=感性吸收)
        public double Frequency { get; set; }           // 输出频率(Hz)
        public double Temperature { get; set; }         // 设备温度(°C)
        public double ActivePowerSettingVal { get; set; }  //有功设置值
        public double ReactivePowerSettingVal { get; set; } //无功功率设置值
        public double PowerFatorSettingVal { get; set; } //功率因数设置值
        public double DcCCSettingVal { get; set; } //直流恒流设置值
        public double DcCVSettingVal { get; set; } //直流恒压设置值
        public double DcCPSettingVal { get; set; } //直流恒功率设置值

        //保护参数，超过值，保护动作
        public double DcProtectChgCurrent { get; set; }    //Dc保护电流
        public double DcProtectChgVoltage { get; set; }   //Dc保护电压
        public double DcProtectDsgCurrent { get; set; }    //Dc放电保护电流
        public double DcProtectDsgVoltage { get; set; }    //Dc 放电保护电压

        //限制参数，超过值，维持该值
        public double DcLimitChgCurrent { get; set; }    //Dc限值电流
        public double DcLimitChgVoltage { get; set; }  //Dc限值电压
        public double DcLimitDsgCurrent { get; set; }    //Dc放电限值电流
        public double DcLimitDsgVoltage { get; set; }    //Dc放电限值电压
        public double DcLimitChgPower { get; set; }      //Dc 充电功率限制
        public double DcLimitDsgPower { get; set; }      //Dc 放电功率限制
        public ushort FaultType { get; set; }               // 故障类型 0-无故障 1-充电故障 2-放电故障 3-其他故障
        public string? FaultMessage { get; set; }        // 故障信息
        public double DailyChargeEnergy { get; set; }    // 日充电能量(kWh)
        public double TotalChargeEnergy { get; set; }    // 累计充电能量(kWh)
        public double DailyDischargeEnergy { get; set; } // 日放电能量(kWh)
        public double TotalDischargeEnergy { get; set; } // 累计放电能量(kWh)

        public DateTime Timestamp { get; set; }         // 状态时间戳
    }

    // 电网状态
    public class GridState
    {
        public double Voltage { get; set; }             // 电网电压 (V)
        public double Frequency { get; set; }           // 电网频率 (Hz)
        public bool IsAvailable { get; set; }           // 电网是否可用
    }

    public class PCSSimulator
    {
        public PcsConfiguration _config { get; set; }
        public PcsState _currentState { get; set; }
        private GridState _gridState;
        private double _ambientTemperature;
        private readonly Random _random = new Random();
        private readonly object _setpointLock = new object();
        private double _pendingActiveSetpoint;
        private double _pendingReactiveSetpoint;
        private Thread? _activeRampThread;
        private bool _activeRampThreadRunning;
        private Thread? _reactiveRampThread;
        private bool _reactiveRampThreadRunning;
        private double _slope = 1; // 默认斜率
        private int _interval = 100; // 默认间隔时间(ms)
        private int _delay = 0; // 默认延迟时间(ms)
        private RampCurve _activeRampCurve = RampCurve.Linear;
        private RampCurve _reactiveRampCurve = RampCurve.Linear;
        private double _gridLossCoefficient = 0.01; // 电网损耗系数，用于简化计算
        /// <summary>
        /// 仿真时间加速倍率。爬坡线程的真实 Sleep 时长 = 配置值 / Speedup，
        /// 使 PCS 爬坡速率在仿真时间轴上与主循环的 SimStep 保持一致。
        /// </summary>
        private readonly double _speedup;

        private struct RampStage
        {
            public double Target;
            public int DelayMs;
        }

        public PCSSimulator(PcsConfiguration config, double speedup = 1.0, double ambientTemp = 25.0)
        {
            _config = config;
            _speedup = speedup > 0 ? speedup : 1.0;
            _ambientTemperature = ambientTemp;
            _currentState = new PcsState
            {
                Mode = OperationMode.Standby,
                Temperature = ambientTemp,
                Timestamp = DateTime.Now
            };
            _gridState = new GridState
            {
                Voltage = config.AcVoltageNominal / (1 - _gridLossCoefficient),
                Frequency = config.FrequencyNominal,
                IsAvailable = false
            };

            // 初始化设定值，调节线程按需启动
            _pendingActiveSetpoint = 0;
            _pendingReactiveSetpoint = 0;
            _activeRampThreadRunning = false;
            _reactiveRampThreadRunning = false;
        }

        // 获取当前状态（返回内部引用，调用方只应读取，不应直接赋值属性）
        public PcsState GetCurrentState() => _currentState;

        // 更新电网状态
        public void UpdateGridState(double voltage, double frequency, bool isAvailable)
        {
            _gridState.Voltage = voltage / (1 - _gridLossCoefficient);
            _gridState.Frequency = frequency;
            _gridState.IsAvailable = isAvailable;

            // 电网状态变化时自动切换模式
            if (!isAvailable && _currentState.GMode == GridMode.GridConnected)
            {
                TransitionToGMode(GridMode.Islanded);
            }
            else if (isAvailable && _currentState.GMode == GridMode.Islanded)
            {
                TransitionToGMode(GridMode.GridConnected);
            }
        }

        // 模式切换
        public void TransitionToMode(OperationMode newMode)
        {
            if (_currentState.Mode == newMode) return;
            _currentState.Mode = newMode;
        }

                public void TransitionToGMode(GridMode newMode)
        {
            if (_currentState.GMode == newMode) return;

            // 模式切换前检查
            if (newMode == GridMode.GridConnected && !_gridState.IsAvailable)
            {
                throw new InvalidOperationException("Cannot connect to grid when grid is not available");
            }

            //Console.WriteLine($"PCS mode transition: {_currentState.Mode} -> {newMode}");
            _currentState.GMode = newMode;
        }

        // 设置功率指令
        public void SetPowerCommand(double activePower, double reactivePower = 0)
        {
            // if (_currentState.Mode != OperationMode.GridConnected &&
            //     _currentState.Mode != OperationMode.Islanded)
            // {
            //     //throw new InvalidOperationException("PCS is not in power delivery mode");
            // }

            // 功率限制检查
            activePower = Math.Max(-_config.MaxPower, Math.Min(_config.MaxPower, activePower));
            reactivePower = Math.Max(-_config.MaxPower, Math.Min(_config.MaxPower, reactivePower));

            // 检查总视在功率
            double apparentPower = Math.Sqrt(Math.Pow(activePower, 2) + Math.Pow(reactivePower, 2));
            if (apparentPower > _config.RatedPower * 1.1) // 允许10%过载
            {
                //throw new InvalidOperationException(
                //    $"Power command exceeds PCS capacity: {apparentPower}kVA > {_config.RatedPower * 1.1}kVA");
            }

            // 仅记录设定值，由后台线程分阶段逼近
            lock (_setpointLock)
            {
                _pendingActiveSetpoint = activePower;
                _pendingReactiveSetpoint = reactivePower;
            }

            // 在启动功率变换线程前检查pcs的运行状态，如果是故障则不启动
            if (_currentState.Mode != OperationMode.Off)
            {
                if (_currentState.FaultType == 3) return;
                // 正放负充：充电故障(1)拦截负功率设定（充电），放电故障(2)拦截正功率设定（放电）
                if (_currentState.FaultType == 1 && _pendingActiveSetpoint < 0) return;
                if (_currentState.FaultType == 2 && _pendingActiveSetpoint > 0) return;
                StartPowerRampThreadIfNeeded();
            }
        }

        // 设置PCS内部特性
        public void SetPcsCharacteristic(string characteristic, double num)
        {
            switch (characteristic)
            {
                case "slope":
                    _slope = num;
                    break;
                case "interval":
                    _interval = (int)num;
                    break;
                case "delay":
                    _delay = (int)num;
                    break;
                default:
                    throw new ArgumentException($"Unknown PCS characteristic: {characteristic}");
            }
        }

        // 按需启动功率调节线程，避免无用的后台占用
        private void StartPowerRampThreadIfNeeded()
        {
            lock (_setpointLock)
            {
                if (_activeRampThreadRunning)
                {
                    return;
                }
                _activeRampThreadRunning = true;
                _activeRampThread = new Thread(ActiveRampWorker) { IsBackground = true };
                _activeRampThread.Start();

                if (_reactiveRampThreadRunning)
                {
                    return;
                }
                _reactiveRampThreadRunning = true;
                _reactiveRampThread = new Thread(ReactiveRampWorker) { IsBackground = true };
                _reactiveRampThread.Start();

            }
        }

        // 计算功率响应阶段：统一函数，支持线性/二次/平方根曲线；总时长按默认总时长分摊
        private List<RampStage> ComputePowerRampStages(double current, double target, RampCurve curve)
        {
            var stages = new List<RampStage>();
            if (Math.Abs(target - current) == 0)
            {
                return stages;
            }

            // _slope * _interval 定义每段最大变化量,直到达到目标，将每个阶段放入stages
            double intervalPowerChange = curve switch
                {
                    RampCurve.Linear => (int)(_slope * _interval),
                    RampCurve.Quadratic => (int)(_slope * _interval * _interval),
                    RampCurve.SquareRoot => (int)(_slope * Math.Sqrt(_interval)),
                    _ => (int)(_slope * _interval)
                };
            while (Math.Abs(target - current) > 0)
            {
                double stepChange = Math.Min(Math.Abs(target - current), intervalPowerChange);
                double stepTarget = current + Math.Sign(target - current) * stepChange;
                stages.Add(new RampStage
                {
                    Target = stepTarget,
                    DelayMs = _interval
                });
                current = stepTarget;
            }
            return stages;
        }

        // 后台线程：监控设定值变化并按阶段延迟调整实际功率
        private void ActiveRampWorker()
        {
            Thread.Sleep((int)(_delay / _speedup)); // 初始延迟（按仿真加速倍率压缩）
            while (true)
            {
                double desiredActive;
                lock (_setpointLock)
                {
                    desiredActive = _pendingActiveSetpoint;
                }

                double currentActive = _currentState.ActivePower;
                var ActiveStages = ComputePowerRampStages(currentActive, desiredActive, _activeRampCurve);

                // 如果当前已达到目标且没有阶段需要执行，则退出线程节省资源
                if (ActiveStages.Count == 0)
                {
                    lock (_setpointLock)
                    {
                        _activeRampThreadRunning = false;
                        _activeRampThread = null;
                    }
                    return;
                }

                foreach (var stage in ActiveStages)
                {
                    // 在执行每个阶段前检查是否有新的设定值，如果有则打断并重算
                    double latestActive;
                    lock (_setpointLock)
                    {
                        latestActive = _pendingActiveSetpoint;
                    }
                    if (Math.Abs(latestActive - desiredActive) > 0)
                    {
                        // 设定发生变化，跳出重新计算
                        break;
                    }

                    // 爬坡延迟按仿真加速倍率压缩，使仿真时间轴上的爬坡速率与配置一致
                    Thread.Sleep(Math.Max(1, (int)(stage.DelayMs / _speedup)));

                    // 根据阶段目标更新无功功率
                    _currentState.ActivePower = stage.Target;
                }
            }
        }

        private void ReactiveRampWorker()
        {
            while (true)
            {
                double desiredReactive;
                lock (_setpointLock)
                {
                    desiredReactive = _pendingReactiveSetpoint;
                }

                double currentReactive = _currentState.ReactivePower;
                var ReactiveStages = ComputePowerRampStages(currentReactive, desiredReactive, _reactiveRampCurve);
                // 如果当前已达到目标且没有阶段需要执行，则退出线程节省资源
                if (ReactiveStages.Count == 0)
                {
                    lock (_setpointLock)
                    {
                        _reactiveRampThreadRunning = false;
                        _reactiveRampThread = null;
                    }
                    return;
                }

                foreach (var stage in ReactiveStages)
                {
                    // 在执行每个阶段前检查是否有新的设定值，如果有则打断并重算
                    double latestReactive;
                    lock (_setpointLock)
                    {
                        latestReactive = _pendingReactiveSetpoint;
                    }
                    if (Math.Abs(latestReactive - desiredReactive) > 0)
                    {
                        // 设定发生变化，跳出重新计算
                        break;
                    }

                    // 爬坡延迟按仿真加速倍率压缩，使仿真时间轴上的爬坡速率与配置一致
                    Thread.Sleep(Math.Max(1, (int)(stage.DelayMs / _speedup)));

                    // 根据阶段目标更新当前功率
                    _currentState.ReactivePower = stage.Target;
                }
            }
        }

        public enum ControlStrategy
        {
            ConstantPower,
            ConstantCurrent,
            VoltageDroop,
            FrequencyDroop
        }

        public void SetControlStrategy(ControlStrategy strategy, double setpoint)
        {
            switch (strategy)
            {
                case ControlStrategy.ConstantPower:
                    // 实现恒功率控制
                    break;
                case ControlStrategy.ConstantCurrent:
                    // 实现恒电流控制
                    break;
                case ControlStrategy.VoltageDroop:
                    // 实现电压下垂控制
                    break;
                case ControlStrategy.FrequencyDroop:
                    // 实现频率下垂控制
                    break;
            }
        }

        // 更新PCS状态
        public void Update(double dcVoltage, ushort isBmsFault, DateTime timeStamp, TimeSpan timeStep)
        {
            // 判断时间戳是否不再同一天，若是则重置日统计
            if (_currentState.Timestamp.Date != timeStamp.Date)
            {
                _currentState.DailyChargeEnergy = 0;
                _currentState.DailyDischargeEnergy = 0;
            }
            _currentState.Timestamp = timeStamp;
            _currentState.DcVoltage = dcVoltage;

            // 1. 检查故障条件
            CheckFaultConditions();
            if (_currentState.FaultType != 3)
            {
                _currentState.FaultType = isBmsFault;
            }

            if (_currentState.FaultType != 0)
            {
                // 如果isBmsFault为3，则直接进入故障状态，如果为1，则需要判断当前充放电状态，如果正在充电则进入故障状态，否则继续运行，如果为2，则需要判断当前充放电状态，如果正在放电则进入故障状态，否则继续运行
                // 正放负充约定：充电故障(1)阻止 ActivePower<0，放电故障(2)阻止 ActivePower>0
                if (_currentState.FaultType == 3 ||
                    (_currentState.FaultType == 1 && _currentState.ActivePower < 0) ||
                    (_currentState.FaultType == 2 && _currentState.ActivePower > 0))
                {
                    _currentState.Mode = OperationMode.Off;
                }
                else
                {
                    _currentState.Mode = OperationMode.Normal;
                }
            }
            else
            {
                _currentState.Mode = OperationMode.Normal;
            }

            // 设计约束：无功目标由 EMS 主控下发，PCS 不在本地自动改写无功设定值。
            // 因此这里不调用本地 Volt-Var 控制逻辑，避免覆盖 EMS 设定。

            // 2. 根据模式更新状态
            switch (_currentState.Mode)
            {
                case OperationMode.Off:
                    UpdateStandbyState();
                    break;

                case OperationMode.Standby:
                    UpdateStandbyState();
                    break;
                case OperationMode.Normal:
                    if (_currentState.GMode == GridMode.GridConnected)
                    {
                        UpdateGridConnectedState();
                    }
                    else if (_currentState.GMode == GridMode.Islanded)
                    {
                        UpdateIslandedState();
                    }
                    else
                    {
                        UpdateStandbyState();
                    }
                    break;
                default:
                    UpdateStandbyState();
                    break;
            }

            // 3. 更新温度模型
            UpdateTemperatureModel(timeStep);

            // 4. 更新充放电能量统计（正放负充：ActivePower>0为放电，<0为充电）
            double energyChange = _currentState.ActivePower * timeStep.TotalHours; // kWh
            if (energyChange > 0)
            {
                // 放电
                _currentState.DailyDischargeEnergy += energyChange;
                _currentState.TotalDischargeEnergy += energyChange;
            }
            else
            {
                // 充电
                _currentState.DailyChargeEnergy += -energyChange;
                _currentState.TotalChargeEnergy += -energyChange;
            }
        }

        private void UpdateStandbyState()
        {
            _currentState.DcCurrent = 0;
            _currentState.AcVoltage = 0;
            _currentState.AcCurrent = 0;
            _currentState.Frequency = 0;
            _currentState.ActivePower = 0;
            _currentState.ReactivePower = 0;
        }

        private void UpdateGridConnectedState()
        {
            // 电流方向约定：正放负充
            // 放电(ActivePower > 0)：直流电流从电池流出为正；
            // 在系统并网点汇总中，约定“从电网流向设备”为正，因此放电对应交流电流为负。
            // 充电(ActivePower < 0)：从电网吸收功率，对应交流电流为正。
            double dcPower = _currentState.ActivePower > 0
                ? _currentState.ActivePower / _config.Efficiency   // 放电：直流侧需提供更多功率
                : _currentState.ActivePower * _config.Efficiency;  // 充电：直流侧吸收更少功率
            _currentState.DcCurrent = dcPower * 1000 / _currentState.DcVoltage;

            // 交流侧参数 (与电网同步)
            _currentState.AcVoltage = _gridState.Voltage * (1 - _gridLossCoefficient);
            _currentState.Frequency = _gridState.Frequency;

            // 计算交流电流（带符号，正=从网侧取电，负=向网侧送电）
            double apparentPower = Math.Sqrt(
                Math.Pow(_currentState.ActivePower, 2) +
                Math.Pow(_currentState.ReactivePower, 2));
            double acCurrentMag = apparentPower * 1000 / (_currentState.AcVoltage * Math.Sqrt(3));
            _currentState.AcCurrent = _currentState.ActivePower >= 0 ? -acCurrentMag : acCurrentMag;
        }

        private void UpdateIslandedState()
        {
            // 电流方向约定：正放负充（离网模式作为电压源向负载供电）
            double dcPower = _currentState.ActivePower > 0
                ? _currentState.ActivePower / _config.Efficiency
                : _currentState.ActivePower * _config.Efficiency;
            _currentState.DcCurrent = dcPower * 1000 / _currentState.DcVoltage;

            // 交流侧参数 (作为电压源)
            _currentState.AcVoltage = _config.AcVoltageNominal;
            _currentState.Frequency = _config.FrequencyNominal;

            // 计算交流电流（带符号，正=放电，负=充电）
            double apparentPower = Math.Sqrt(
                Math.Pow(_currentState.ActivePower, 2) +
                Math.Pow(_currentState.ReactivePower, 2));
            double acCurrentMag = apparentPower * 1000 / (_currentState.AcVoltage * Math.Sqrt(3));
            _currentState.AcCurrent = _currentState.ActivePower >= 0 ? acCurrentMag : -acCurrentMag;
        }

        private void UpdateTemperatureModel(TimeSpan timeStep)
        {
            // 计算功率损耗 (简化模型)
            double powerLoss = Math.Abs(_currentState.ActivePower) * (1 - _config.Efficiency);

            // 温度变化计算
            double cooling = (_currentState.Temperature - _ambientTemperature) * 50; // 假设冷却系数50W/°C
            double tempChange = (powerLoss - cooling) * timeStep.TotalHours / 10.0; // 假设热容10kWh/°C

            _currentState.Temperature = Math.Max(_ambientTemperature, _currentState.Temperature + tempChange);
        }

        private void CheckFaultConditions()
        {
            _currentState.FaultType = 0;
            _currentState.FaultMessage = "";

            // 检查直流电压范围
            if (_currentState.DcVoltage < _config.DcVoltageRangeMin * 0.9 ||
                _currentState.DcVoltage > _config.DcVoltageRangeMax * 1.1)
            {
                _currentState.FaultType = 3;
                _currentState.FaultMessage += $"DC voltage fault: {_currentState.DcVoltage}V; ";
            }

            // 检查过流
            if (Math.Abs(_currentState.AcCurrent) > _config.MaxCurrent)
            {
                _currentState.FaultType = 3;
                _currentState.FaultMessage += $"Over current: {_currentState.AcCurrent}A; ";
            }

            // 检查温度
            if (_currentState.Temperature > 70.0) // 假设70°C为过热阈值
            {
                _currentState.FaultType = 3;
                _currentState.FaultMessage += $"Over temperature: {_currentState.Temperature}°C; ";
            }

            // 检查孤岛保护 (仅并网模式)
            if (_currentState.GMode == GridMode.GridConnected && !_gridState.IsAvailable)
            {
                _currentState.FaultType = 3;
                _currentState.FaultMessage += "Islanding detected; ";
            }
        }
    }
}
