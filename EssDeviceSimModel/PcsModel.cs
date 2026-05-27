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

    /// <summary>黑启动子状态：建压（V/f）或同步并联（母线已带电）。</summary>
    public enum BlackStartPhase
    {
        Inactive = 0,
        VoltageBuilding = 1,
        Synchronized = 2
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
        public double ReactivePower { get; set; }        // 无功功率(kvar, 约定: 正=升压支撑, 负=降压作用)
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

        /// <summary>EMS 下发的孤岛电压设定（V，线电压幅值，0–交流额定）。</summary>
        public double IslandVoltageCommandV { get; set; }

        /// <summary>PCS 内部跟随后的有效孤岛电压（V），用于遥测反馈与 V/f 输出。</summary>
        public double IslandVoltageEffectiveV { get; set; }

        /// <summary>黑启动模式是否激活（与 EMS 黑启动开启点位一致）。</summary>
        public bool BlackStartEnabled { get; set; }

        /// <summary>黑启动子状态：建压 / 同步并联 / 未激活。</summary>
        public BlackStartPhase BlackStartPhase { get; set; }
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
        private volatile bool _rampStopRequested;
        private readonly double _slope; // 爬坡斜率
        private readonly int _interval; // 间隔时间(ms)
        private readonly int _delay; // 初始延迟(ms)
        private RampCurve _activeRampCurve = RampCurve.Linear;
        private RampCurve _reactiveRampCurve = RampCurve.Linear;
        private readonly double _gridLossCoefficient; // 电网损耗系数，用于简化计算

        private readonly object _islandVfLock = new();
        private double _islandVfCommandV;
        private double _islandVfEffectiveV;
        /// <summary>孤岛电压有效值向命令值过渡的最长仿真时间（秒）；建压/降压均在此时间内完成，便于快 dV/dt 触发励磁涌流。</summary>
        private readonly double _islandVoltageRampDurationSec;
        private bool _blackStartEnabled;
        private readonly double _blackStartActivePowerGainKwPerVolt;
        private readonly double _blackStartMaxActivePowerKw;
        private readonly double _blackStartMagnetizingPowerFraction;
        private readonly double _blackStartBusEnergizedFraction;
        private double _unitBusVoltageV;
        private BlackStartPhase _blackStartPhase = BlackStartPhase.Inactive;
        private double _transformerMagnetizingReactiveKvar;
        private double _blackStartSharedLossActivePowerKw;
        /// <summary>EMS 负荷有功/无功（爬坡线程），与站用电叠加后受额定功率限制。</summary>
        private double _loadActivePowerKw;
        private double _loadReactivePowerKvar;
        private ushort _latchedFaultType;
        private string? _latchedFaultMessage;
        private bool _faultTripLatched;
        private bool _externalRunCommand;
        private bool _externalRunRisingEdge;
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

        public PCSSimulator(
            PcsConfiguration config,
            double speedup = 1.0,
            double ambientTemp = 25.0,
            double gridLossCoefficient = 0.01,
            double slope = 1,
            int intervalMs = 100,
            int delayMs = 0,
            double islandVoltageRampDurationMs = 100,
            double blackStartActivePowerGainKwPerVolt = 2.174,
            double blackStartMaxActivePowerKw = 200,
            double blackStartMagnetizingPowerFraction = 0.02,
            double blackStartBusEnergizedFraction = 0.85)
        {
            _config = config;
            _speedup = speedup > 0 ? speedup : 1.0;
            _ambientTemperature = ambientTemp;
            _gridLossCoefficient = Math.Clamp(gridLossCoefficient, 0, 0.95);
            _islandVoltageRampDurationSec = Math.Max(0.001, islandVoltageRampDurationMs / 1000.0);
            _blackStartActivePowerGainKwPerVolt = Math.Max(0, blackStartActivePowerGainKwPerVolt);
            _blackStartMaxActivePowerKw = Math.Max(0, blackStartMaxActivePowerKw);
            _blackStartMagnetizingPowerFraction = Math.Clamp(blackStartMagnetizingPowerFraction, 0, 0.2);
            _blackStartBusEnergizedFraction = Math.Clamp(blackStartBusEnergizedFraction, 0.5, 1.0);
            _slope = slope;
            _interval = Math.Max(1, intervalMs);
            _delay = Math.Max(0, delayMs);
            _currentState = new PcsState
            {
                Mode = OperationMode.Off,
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
            _rampStopRequested = false;
        }

        // 获取当前状态（返回内部引用，调用方只应读取，不应直接赋值属性）
        public PcsState GetCurrentState() => _currentState;

        /// <summary>网侧是否视为带电可用（单元高压分闸或主网失电时为 false）。与 EMS 启停配合时用于避免与主循环 Standby 对打。</summary>
        public bool IsGridElectricallyAvailable => _gridState.IsAvailable;

        /// <summary>外部启停命令（Modbus pcsOnOffSwitch）。运行模式仅在外部写 1（0→1 边沿）或已非停机时可进入 Normal/Standby。</summary>
        public bool IsExternalRunCommand => _externalRunCommand;

        /// <summary>本周期是否检测到外部启停 0→1 边沿（每周期在 SyncExternalRunCommand 后读取一次）。</summary>
        public bool ExternalRunRisingEdge => _externalRunRisingEdge;

        /// <summary>已发生故障跳闸并锁存，需外部启停先写 0 再写 1 方可清除后重新启动。</summary>
        public bool HasLatchedFaultTrip => _faultTripLatched;

        /// <summary>
        /// 同步 EMS/Modbus 启停位。停机仅由外部写 0、联锁或故障触发；禁止网侧恢复后自动离开 Off。
        /// </summary>
        public void SyncExternalRunCommand(bool run)
        {
            _externalRunRisingEdge = run && !_externalRunCommand;
            if (!run)
            {
                ClearLatchedFault();
                ApplyIslandVoltageCommand(0);
                ApplyBlackStartEnabled(false);
                if (_currentState.Mode != OperationMode.Off)
                    TransitionToMode(OperationMode.Off);
            }
            else if (_externalRunRisingEdge)
            {
                // 外部重新发启动脉冲时清除历史故障；保持启停=1 时不自动复归
                ClearLatchedFault();
            }

            _externalRunCommand = run;
        }

        private void ClearLatchedFault()
        {
            _faultTripLatched = false;
            _latchedFaultType = 0;
            _latchedFaultMessage = null;
            _currentState.FaultType = 0;
            _currentState.FaultMessage = null;
        }

        private void LatchFault(ushort faultType, string message)
        {
            if (faultType == 0)
                return;
            _faultTripLatched = true;
            _latchedFaultType = faultType;
            if (string.IsNullOrEmpty(_latchedFaultMessage))
                _latchedFaultMessage = message;
            else if (!string.IsNullOrEmpty(message) && _latchedFaultMessage.IndexOf(message, StringComparison.Ordinal) < 0)
                _latchedFaultMessage += message;
            _currentState.FaultType = _latchedFaultType;
            _currentState.FaultMessage = _latchedFaultMessage;
        }

        /// <summary>
        /// 计算网侧有功功率（kW）。
        /// 约定：正值表示向电网送电，负值表示从电网取电。
        /// - 放电时（P>=0）按线损效率折减；
        /// - 充电时（P<0）按线损效率反推网侧取电。
        /// </summary>
        public double GetGridSideActivePower()
        {
            var p = _currentState.ActivePower;
            var lineEfficiency = 1.0 - _gridLossCoefficient;
            return p >= 0 ? p * lineEfficiency : p / lineEfficiency;
        }

        /// <summary>EMS/Modbus 写入孤岛电压设定（V）。黑启动同步模式下忽略。</summary>
        public void ApplyIslandVoltageCommand(double voltageV)
        {
            if (_blackStartEnabled && _blackStartPhase == BlackStartPhase.Synchronized)
                return;

            double maxV = _config.AcVoltageNominal;
            voltageV = Math.Clamp(voltageV, 0, maxV);
            lock (_islandVfLock)
            {
                _islandVfCommandV = voltageV;
                _currentState.IslandVoltageCommandV = voltageV;
            }
        }

        /// <summary>EMS 写入黑启动开启。</summary>
        public void ApplyBlackStartEnabled(bool enabled)
        {
            _blackStartEnabled = enabled;
            _currentState.BlackStartEnabled = enabled;
            if (!enabled)
            {
                _blackStartPhase = BlackStartPhase.Inactive;
                _currentState.BlackStartPhase = BlackStartPhase.Inactive;
                return;
            }

            // 黑启动构网不接收外部功率模式设定，切入时清空并停止爬坡线程残留
            lock (_setpointLock)
            {
                _rampStopRequested = true;
                _pendingActiveSetpoint = 0;
                _pendingReactiveSetpoint = 0;
            }
            _loadActivePowerKw = 0;
            _loadReactivePowerKvar = 0;
        }

        private bool CanAcceptPowerCommand =>
            _currentState.Mode == OperationMode.Normal &&
            _gridState.IsAvailable;

        private static void ClampApparentPower(ref double activeKw, ref double reactiveKvar, double ratedKva, double overload = 1.1)
        {
            double s = Math.Sqrt(activeKw * activeKw + reactiveKvar * reactiveKvar);
            double maxS = ratedKva * overload;
            if (s <= maxS || s < 1e-6)
                return;
            double scale = maxS / s;
            activeKw *= scale;
            reactiveKvar *= scale;
        }

        /// <summary>主循环刷新同单元 690V 母线电压，判定建压/同步阶段。</summary>
        public void RefreshBlackStartBusContext(double unitBusVoltageV)
        {
            _unitBusVoltageV = Math.Max(0, unitBusVoltageV);
            if (!_blackStartEnabled)
            {
                _blackStartPhase = BlackStartPhase.Inactive;
                _currentState.BlackStartPhase = BlackStartPhase.Inactive;
                return;
            }

            double nom = Math.Max(_config.AcVoltageNominal, 1.0);
            double energizedV = nom * _blackStartBusEnergizedFraction;
            bool busEnergized = _unitBusVoltageV >= energizedV;

            if (busEnergized)
                _blackStartPhase = BlackStartPhase.Synchronized;
            else
                _blackStartPhase = BlackStartPhase.VoltageBuilding;

            _currentState.BlackStartPhase = _blackStartPhase;

            if (_blackStartPhase == BlackStartPhase.Synchronized)
            {
                double v = Math.Clamp(_unitBusVoltageV, 0, nom);
                lock (_islandVfLock)
                {
                    _islandVfEffectiveV = v;
                    _currentState.IslandVoltageEffectiveV = v;
                }
            }
        }

        public BlackStartPhase GetBlackStartPhase() => _blackStartPhase;

        public bool IsBlackStartSynchronized =>
            _blackStartEnabled && _blackStartPhase == BlackStartPhase.Synchronized;

        public bool IsBlackStartActive =>
            _blackStartEnabled &&
            _currentState.Mode == OperationMode.Normal &&
            _currentState.GMode == GridMode.Islanded;

        /// <summary>单元变二次侧励磁/涌流折算的无功需求（kvar，由主循环在每步变压器更新后写入）。</summary>
        public void SetTransformerMagnetizingReactiveKvar(double reactiveKvar) =>
            _transformerMagnetizingReactiveKvar = Math.Max(0, reactiveKvar);

        /// <summary>黑启动：分摊的站用电有功（铁损+线损，kW）。</summary>
        public void SetBlackStartSharedLossActivePowerKw(double activeKw) =>
            _blackStartSharedLossActivePowerKw = Math.Max(0, activeKw);

        /// <summary>
        /// 黑启动构网：站用电（励磁无功+铁损/线损+建压有功）+ EMS 负荷 P/Q，总输出受额定功率与 MaxPower 限制。
        /// </summary>
        private void ApplyBlackStartPowerControl(TimeSpan timeStep)
        {
            if (!IsBlackStartActive)
                return;

            double loadP = _loadActivePowerKw;
            double loadQ = _loadReactivePowerKvar;
            double stationP = _blackStartSharedLossActivePowerKw;
            double stationQ = _transformerMagnetizingReactiveKvar;

            if (_blackStartPhase == BlackStartPhase.VoltageBuilding)
            {
                double cmdV;
                double effV;
                lock (_islandVfLock)
                {
                    cmdV = _islandVfCommandV;
                    effV = _islandVfEffectiveV;
                }

                double gapV = Math.Max(0, cmdV - effV);
                double buildP = _blackStartActivePowerGainKwPerVolt * gapV;
                stationP = Math.Max(stationP, buildP);

                double maxStep = _blackStartMaxActivePowerKw * timeStep.TotalSeconds;
                if (maxStep < 1.0)
                    maxStep = 1.0;
                double buildTarget = stationP + loadP;
                double currentP = _currentState.ActivePower;
                if (buildTarget > currentP + maxStep)
                    buildTarget = currentP + maxStep;
                else if (buildTarget < currentP - maxStep)
                    buildTarget = buildTarget < 0 ? 0 : buildTarget;
                stationP = Math.Max(0, buildTarget - loadP);
            }

            double targetP = stationP + loadP;
            double targetQ = stationQ + loadQ;
            targetP = Math.Clamp(targetP, -_config.MaxPower, _config.MaxPower);
            targetQ = Math.Clamp(targetQ, -_config.MaxPower, _config.MaxPower);
            ClampApparentPower(ref targetP, ref targetQ, _config.RatedPower);

            _currentState.ActivePower = targetP;
            _currentState.ReactivePower = targetQ;
        }

        /// <summary>
        /// 有效孤岛电压在 <see cref="_islandVoltageRampDurationSec"/> 内线性趋近命令值（默认 100ms），
        /// 替代原 V/s 慢爬坡，使 0→690V 等阶跃能产生足够快的一次侧 dV/dt 与励磁涌流。
        /// </summary>
        private void UpdateIslandVoltageEffectiveTowardCommand(TimeSpan timeStep)
        {
            if (_blackStartEnabled && _blackStartPhase == BlackStartPhase.Synchronized)
                return;

            lock (_islandVfLock)
            {
                if (_currentState.Mode is OperationMode.Off or OperationMode.Standby)
                    _islandVfEffectiveV = 0;
                else
                {
                    double target = _islandVfCommandV;
                    double eff = _islandVfEffectiveV;
                    double gap = target - eff;
                    if (Math.Abs(gap) < 1e-6)
                        _islandVfEffectiveV = target;
                    else
                    {
                        double dt = Math.Max(timeStep.TotalSeconds, 1e-6);
                        double rampFrac = Math.Min(1.0, dt / _islandVoltageRampDurationSec);
                        _islandVfEffectiveV = eff + gap * rampFrac;
                    }
                }

                _currentState.IslandVoltageCommandV = _islandVfCommandV;
                _currentState.IslandVoltageEffectiveV = _islandVfEffectiveV;
            }
        }

        /// <summary>
        /// 更新网侧电压/频率。<paramref name="isUtilityGridAvailable"/> 仅表示 220kV/35kV 主网带电；
        /// 同单元另一台 PCS 建压的 690V 母线不等同于主网，不得置 true（否则黑启动会报「电网可用」故障）。
        /// </summary>
        public void UpdateGridState(double voltage, double frequency, bool isUtilityGridAvailable)
        {
            _gridState.Voltage = voltage / (1 - _gridLossCoefficient);
            _gridState.Frequency = frequency;
            _gridState.IsAvailable = isUtilityGridAvailable;

            if (!isUtilityGridAvailable && _currentState.GMode == GridMode.GridConnected)
                TransitionToGMode(GridMode.Islanded);
            else if (isUtilityGridAvailable && _currentState.GMode == GridMode.Islanded && !_blackStartEnabled)
                TransitionToGMode(GridMode.GridConnected);

            if (!isUtilityGridAvailable && !_blackStartEnabled)
                StopRampsAndZeroPower();
            else if (IsBlackStartActive && _currentState.Mode == OperationMode.Normal)
                _rampStopRequested = false;
        }

        // 模式切换
        public void TransitionToMode(OperationMode newMode)
        {
            if (_currentState.Mode == newMode) return;
            _currentState.Mode = newMode;

            // 非 Normal 模式不应继续执行功率指令
            if (newMode != OperationMode.Normal)
            {
                StopRampsAndZeroPower();
            }
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

            // 黑启动构网：功率由站用电模型自动计算，不响应 EMS/外部功率指令
            if (IsBlackStartActive)
                return;

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

            if (_faultTripLatched)
                return;

            if (!CanAcceptPowerCommand)
            {
                StopRampsAndZeroPower();
                return;
            }

            lock (_setpointLock)
            {
                // 恢复可用后重新允许爬坡线程运行
                _rampStopRequested = false;
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

        private void StopRampsAndZeroPower()
        {
            // 1) 清零设定值
            lock (_setpointLock)
            {
                _pendingActiveSetpoint = 0;
                _pendingReactiveSetpoint = 0;
                _rampStopRequested = true;
            }
            // 2) 清零当前输出（避免 UI/电池模型继续按非零功率推进）
            _currentState.ActivePower = 0;
            _currentState.ReactivePower = 0;
            _loadActivePowerKw = 0;
            _loadReactivePowerKvar = 0;
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
                // 停止请求：退出线程
                if (_rampStopRequested || !CanAcceptPowerCommand)
                {
                    _loadActivePowerKw = 0;
                    if (!IsBlackStartActive)
                        _currentState.ActivePower = 0;
                    lock (_setpointLock)
                    {
                        _activeRampThreadRunning = false;
                        _activeRampThread = null;
                    }
                    return;
                }

                double desiredActive;
                lock (_setpointLock)
                {
                    desiredActive = _pendingActiveSetpoint;
                }

                double currentActive = IsBlackStartActive ? _loadActivePowerKw : _currentState.ActivePower;
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
                    if (_rampStopRequested || !CanAcceptPowerCommand)
                    {
                        _loadActivePowerKw = 0;
                        if (!IsBlackStartActive)
                            _currentState.ActivePower = 0;
                        lock (_setpointLock)
                        {
                            _activeRampThreadRunning = false;
                            _activeRampThread = null;
                        }
                        return;
                    }

                    double latestActive;
                    lock (_setpointLock)
                    {
                        latestActive = _pendingActiveSetpoint;
                    }
                    if (Math.Abs(latestActive - desiredActive) > 0)
                        break;

                    Thread.Sleep(Math.Max(1, (int)(stage.DelayMs / _speedup)));

                    _loadActivePowerKw = stage.Target;
                    if (!IsBlackStartActive)
                        _currentState.ActivePower = stage.Target;
                }
            }
        }

        private void ReactiveRampWorker()
        {
            while (true)
            {
                // 停止请求：退出线程
                if (_rampStopRequested || !CanAcceptPowerCommand)
                {
                    _loadReactivePowerKvar = 0;
                    if (!IsBlackStartActive)
                        _currentState.ReactivePower = 0;
                    lock (_setpointLock)
                    {
                        _reactiveRampThreadRunning = false;
                        _reactiveRampThread = null;
                    }
                    return;
                }

                double desiredReactive;
                lock (_setpointLock)
                {
                    desiredReactive = _pendingReactiveSetpoint;
                }

                double currentReactive = IsBlackStartActive ? _loadReactivePowerKvar : _currentState.ReactivePower;
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
                    if (_rampStopRequested || !CanAcceptPowerCommand)
                    {
                        _loadReactivePowerKvar = 0;
                        if (!IsBlackStartActive)
                            _currentState.ReactivePower = 0;
                        lock (_setpointLock)
                        {
                            _reactiveRampThreadRunning = false;
                            _reactiveRampThread = null;
                        }
                        return;
                    }

                    double latestReactive;
                    lock (_setpointLock)
                    {
                        latestReactive = _pendingReactiveSetpoint;
                    }
                    if (Math.Abs(latestReactive - desiredReactive) > 0)
                        break;

                    Thread.Sleep(Math.Max(1, (int)(stage.DelayMs / _speedup)));

                    _loadReactivePowerKvar = stage.Target;
                    if (!IsBlackStartActive)
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

            UpdateIslandVoltageEffectiveTowardCommand(timeStep);

            // 1) 先确定本步目标功率，再基于该功率计算电气量，避免保护判断滞后一拍
            if (IsBlackStartActive)
                ApplyBlackStartPowerControl(timeStep);
            else
            {
                _currentState.ActivePower = _loadActivePowerKw;
                _currentState.ReactivePower = _loadReactivePowerKvar;
            }

            // 2) 根据模式更新电气量（过流等保护基于本步 P/Q 与 AcCurrent）
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
                        UpdateGridConnectedState();
                    else if (_currentState.GMode == GridMode.Islanded)
                        UpdateIslandedState();
                    else
                        UpdateStandbyState();
                    break;
                default:
                    UpdateStandbyState();
                    break;
            }

            UpdateTemperatureModel(timeStep);

            CheckFaultConditions();
            if (isBmsFault != 0)
                LatchFault(isBmsFault, $"BMS fault ({isBmsFault}); ");

            if (_currentState.FaultType != 0)
            {
                if (_currentState.FaultType == 3 ||
                    (_currentState.FaultType == 1 && _currentState.ActivePower < 0) ||
                    (_currentState.FaultType == 2 && _currentState.ActivePower > 0))
                {
                    TransitionToMode(OperationMode.Off);
                    UpdateStandbyState();
                }
            }

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
            var gridSideActivePower = GetGridSideActivePower();
            double apparentPower = Math.Sqrt(
                Math.Pow(gridSideActivePower, 2) +
                Math.Pow(_currentState.ReactivePower, 2));
            double denomUg = Math.Max(_currentState.AcVoltage, 10.0);
            double acCurrentMag = apparentPower * 1000 / (denomUg * Math.Sqrt(3));
            _currentState.AcCurrent = gridSideActivePower >= 0 ? -acCurrentMag : acCurrentMag;
        }

        private void UpdateIslandedState()
        {
            // 电流方向约定：正放负充（离网模式作为电压源向负载供电）
            double dcPower = _currentState.ActivePower > 0
                ? _currentState.ActivePower / _config.Efficiency
                : _currentState.ActivePower * _config.Efficiency;
            _currentState.DcCurrent = dcPower * 1000 / _currentState.DcVoltage;

            // 交流侧参数：V/f 幅值 = 有效孤岛电压（V）
            _currentState.AcVoltage = Math.Max(_currentState.IslandVoltageEffectiveV, 1.0);
            _currentState.Frequency = _config.FrequencyNominal;

            // 计算交流电流（带符号，正=放电，负=充电）
            double apparentPower = Math.Sqrt(
                Math.Pow(_currentState.ActivePower, 2) +
                Math.Pow(_currentState.ReactivePower, 2));
            double denomU = Math.Max(_currentState.AcVoltage, 10.0);
            double acCurrentMag = apparentPower * 1000 / (denomU * Math.Sqrt(3));
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
            if (_faultTripLatched)
            {
                _currentState.FaultType = _latchedFaultType;
                _currentState.FaultMessage = _latchedFaultMessage;
                return;
            }

            ushort instantFault = 0;
            var msg = new StringBuilder();

            if (_currentState.DcVoltage < _config.DcVoltageRangeMin * 0.9 ||
                _currentState.DcVoltage > _config.DcVoltageRangeMax * 1.1)
            {
                instantFault = 3;
                msg.Append($"DC voltage fault: {_currentState.DcVoltage:F1}V; ");
            }

            if (Math.Abs(_currentState.AcCurrent) > _config.MaxCurrent)
            {
                instantFault = 3;
                msg.Append($"Over current: {_currentState.AcCurrent:F1}A (limit {_config.MaxCurrent:F0}A); ");
            }

            if (_currentState.Temperature > 70.0)
            {
                instantFault = 3;
                msg.Append($"Over temperature: {_currentState.Temperature:F1}°C; ");
            }

            if (_currentState.GMode == GridMode.GridConnected && !_gridState.IsAvailable)
            {
                instantFault = 3;
                msg.Append("Islanding detected; ");
            }

            if (_blackStartEnabled && _gridState.IsAvailable)
            {
                instantFault = 3;
                msg.Append("Black start enabled while utility grid is available; ");
            }

            if (instantFault != 0)
                LatchFault(instantFault, msg.ToString());
            else
            {
                _currentState.FaultType = 0;
                _currentState.FaultMessage = null;
            }
        }
    }
}
