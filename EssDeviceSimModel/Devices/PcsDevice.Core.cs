using System;
using System.Collections.Generic;
using System.Text;
using EssSimulator.EssDeviceSimModel.Interface;
using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Devices
{
    /// <summary>
    /// PCS 设备模型（阶段 5）：跟网/离网、功率爬坡、黑启动、保护、AC/DC 端口。
    /// PCS 设备模型：爬坡、黑启动、保护、跟网/离网；ESS 与电气网络共用同一实例。
    /// </summary>
    public sealed partial class PcsDevice : IPcsDevice
    {
        private readonly PcsDeviceConfig _deviceConfig;
        public PcsConfiguration _config { get; }
        public string DeviceId { get; }
        public ElectricalDeviceKind Kind => ElectricalDeviceKind.Pcs;
        public ElectricalPort Ac { get; }
        public ElectricalPort Dc { get; }
        public IReadOnlyList<ElectricalPort> Ports => new[] { Ac, Dc };
        public bool GridAvailable => _gridState.IsAvailable;
        public DeviceFaultState Fault => new()
        {
            FaultCode = _currentState.FaultType,
            FaultMessage = _currentState.FaultMessage
        };
        public PcsState _currentState { get; set; }
        private GridState _gridState;
        private double _ambientTemperature;
        private readonly Random _random = new Random();
        private readonly object _setpointLock = new object();
        private double _pendingActiveSetpoint;
        private double _pendingReactiveSetpoint;
        private volatile bool _rampStopRequested;
        private double _rampDelayRemainingSec;
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
        private readonly double _blackStartPrechargeDelaySec;
        private readonly double _blackStartVoltageRampVs;
        private readonly double _blackStartFrequencyStartHz;
        private readonly double _blackStartFrequencyRampHzPerSec;
        private readonly double _blackStartReactiveVoltageGainKvarPerV;
        private readonly double _blackStartCurrentLimitFraction;
        private double _unitBusVoltageV;
        private BlackStartPhase _blackStartPhase = BlackStartPhase.Inactive;
        private double _blackStartPrepareRemainingSec;
        private double _blackStartSoftCapV;
        private double _blackStartIslandFreqHz;
        private double _transformerMagnetizingReactiveKvar;
        private double _blackStartSharedLossActivePowerKw;
        private double _blackStartInrushActiveKw;
        private double _blackStartInrushReactiveKvar;
        /// <summary>EMS 负荷有功/无功（爬坡线程），与站用电叠加后受额定功率限制。</summary>
        private double _loadActivePowerKw;
        private double _loadReactivePowerKvar;
        private ushort _latchedFaultType;
        private string? _latchedFaultMessage;
        private bool _faultTripLatched;
        private bool _externalRunCommand;
        private bool _externalRunRisingEdge;

        public PcsDevice(string deviceId, PcsDeviceConfig deviceConfig, double ambientTemp = 25.0)
        {
            DeviceId = deviceId;
            _deviceConfig = deviceConfig;
            _config = new PcsConfiguration
            {
                RatedPower = deviceConfig.RatedPowerKw,
                MaxPower = deviceConfig.MaxPowerKw,
                Efficiency = deviceConfig.Efficiency,
                DcVoltageRangeMin = deviceConfig.DcVoltageRangeMinV,
                DcVoltageRangeMax = deviceConfig.DcVoltageRangeMaxV,
                AcVoltageNominal = deviceConfig.AcNominalLineVoltageV,
                FrequencyNominal = deviceConfig.FrequencyHz,
                MaxCurrent = deviceConfig.MaxCurrentA
            };
            _ambientTemperature = ambientTemp;
            _gridLossCoefficient = Math.Clamp(deviceConfig.GridLossCoefficient, 0, 0.95);
            _islandVoltageRampDurationSec = Math.Max(0.001, deviceConfig.IslandVoltageRampDurationMs / 1000.0);
            _blackStartActivePowerGainKwPerVolt = Math.Max(0, deviceConfig.BlackStartActivePowerGainKwPerVolt);
            _blackStartMaxActivePowerKw = Math.Max(0, deviceConfig.BlackStartMaxActivePowerKw);
            _blackStartMagnetizingPowerFraction = Math.Clamp(deviceConfig.BlackStartMagnetizingPowerFraction, 0, 0.2);
            _blackStartBusEnergizedFraction = Math.Clamp(deviceConfig.BlackStartBusEnergizedFraction, 0.5, 1.0);
            _blackStartPrechargeDelaySec = Math.Max(0, deviceConfig.BlackStartPrechargeDelayMs / 1000.0);
            _blackStartVoltageRampVs = Math.Max(1, deviceConfig.BlackStartVoltageRampVs);
            _blackStartFrequencyStartHz = Math.Clamp(deviceConfig.BlackStartFrequencyStartHz, 40, deviceConfig.FrequencyHz);
            _blackStartFrequencyRampHzPerSec = Math.Max(0.1, deviceConfig.BlackStartFrequencyRampHzPerSec);
            _blackStartReactiveVoltageGainKvarPerV = Math.Max(0, deviceConfig.BlackStartReactiveVoltageGainKvarPerV);
            _blackStartCurrentLimitFraction = Math.Clamp(deviceConfig.BlackStartCurrentLimitFraction, 0.1, 1.0);
            _blackStartIslandFreqHz = _blackStartFrequencyStartHz;
            _slope = deviceConfig.RampSlope;
            _interval = Math.Max(1, deviceConfig.RampIntervalMs);
            _delay = Math.Max(0, deviceConfig.RampDelayMs);
            _rampDelayRemainingSec = _delay / 1000.0;
            Ac = CreateAcPort(deviceConfig.AcConnection);
            Dc = CreateDcPort();
            _currentState = new PcsState
            {
                Mode = OperationMode.Off,
                Temperature = ambientTemp,
                Timestamp = DateTime.Now
            };
            _gridState = new GridState
            {
                Voltage = _config.AcVoltageNominal / (1 - _gridLossCoefficient),
                Frequency = _config.FrequencyNominal,
                IsAvailable = false
            };

            _pendingActiveSetpoint = 0;
            _pendingReactiveSetpoint = 0;
            _rampStopRequested = false;
        }

        public void ApplyCommand(DeviceCommand command)
        {
            switch (command.Kind)
            {
                case DeviceCommandKind.PcsStartStop:
                    SyncExternalRunCommand(command.BoolValue);
                    break;
                case DeviceCommandKind.PcsActivePower:
                    SetPowerCommand(command.NumericValue, _pendingReactiveSetpoint);
                    break;
                case DeviceCommandKind.PcsReactivePower:
                    SetPowerCommand(_pendingActiveSetpoint, command.NumericValue);
                    break;
            }
        }

        public void SetGridAvailable(bool available) =>
            _gridState.IsAvailable = available;

        /// <summary>NetworkSolver 步进：将当前电气状态写入 AC/DC 端口（物理在 <see cref="Update"/> 中完成）。</summary>
        public void Step(DeviceStepContext context, TimeSpan step)
        {
            PublishPortsFromState();
        }

        private void PublishPortsFromState()
        {
            if (_currentState.Mode is OperationMode.Off or OperationMode.Standby)
            {
                WriteIdleOutputs();
                return;
            }

            double gridSideP = GetGridSideActivePower();
            double q = _currentState.ReactivePower;
            double acV = _currentState.GMode == GridMode.GridConnected
                ? _currentState.AcVoltage
                : Math.Max(_currentState.IslandVoltageEffectiveV, _currentState.AcVoltage);

            var acOut = AcQuantityConverter.FromLineVoltageAndPower(
                Math.Max(acV, 1.0),
                gridSideP,
                q,
                _deviceConfig.AcConnection,
                _currentState.Frequency > 1 ? _currentState.Frequency : _config.FrequencyNominal);

            AcPortHelper.WriteAcOutput(Ac, acOut);
            AcPortHelper.WriteDcOutput(Dc, new DcSnapshot
            {
                VoltageV = _currentState.DcVoltage,
                CurrentA = _currentState.DcCurrent
            });
        }

        private void WriteIdleOutputs()
        {
            AcPortHelper.WriteAcOutput(Ac, new AcInternalQuantities
            {
                Connection = _deviceConfig.AcConnection,
                LineVoltageV = 0,
                LineCurrentA = 0,
                FrequencyHz = 0
            });
            AcPortHelper.WriteDcOutput(Dc, new DcSnapshot());
        }

        private static ElectricalPort CreateAcPort(ThreePhaseConnection connection)
        {
            var empty = new AcInternalQuantities { Connection = connection };
            return new ElectricalPort
            {
                PortId = "ac",
                Kind = PortKind.BusConnected,
                Input = ElectricalPortSnapshot.FromAc(empty),
                Output = ElectricalPortSnapshot.FromAc(empty)
            };
        }

        private static ElectricalPort CreateDcPort() =>
            new()
            {
                PortId = "dc",
                Kind = PortKind.DcLink,
                Input = ElectricalPortSnapshot.FromDc(new DcSnapshot()),
                Output = ElectricalPortSnapshot.FromDc(new DcSnapshot())
            };

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
                _rampStopRequested = false;
                _pendingActiveSetpoint = activePower;
                _pendingReactiveSetpoint = reactivePower;
                if (_rampDelayRemainingSec <= 0 && _delay > 0)
                    _rampDelayRemainingSec = _delay / 1000.0;
            }
        }

        public void SetControlStrategy(PcsControlStrategy strategy, double setpoint)
        {
            switch (strategy)
            {
                case PcsControlStrategy.ConstantPower:
                    // 实现恒功率控制
                    break;
                case PcsControlStrategy.ConstantCurrent:
                    // 实现恒电流控制
                    break;
                case PcsControlStrategy.VoltageDroop:
                    // 实现电压下垂控制
                    break;
                case PcsControlStrategy.FrequencyDroop:
                    // 实现频率下垂控制
                    break;
            }
        }

        // 更新PCS状态
        public void Update(
            double dcVoltage,
            ushort isBmsFault,
            DateTime timeStamp,
            TimeSpan timeStep,
            TimeSpan? integrationStep = null)
        {
            var intStep = integrationStep ?? timeStep;
            // 判断时间戳是否不再同一天，若是则重置日统计
            if (_currentState.Timestamp.Date != timeStamp.Date)
            {
                _currentState.DailyChargeEnergy = 0;
                _currentState.DailyDischargeEnergy = 0;
            }
            _currentState.Timestamp = timeStamp;
            _currentState.DcVoltage = dcVoltage;

            AdvancePowerRamps(timeStep);
            if (_blackStartEnabled)
                AdvanceBlackStartPhase(timeStep);
            UpdateIslandVoltageEffectiveTowardCommand(timeStep);

            // 1) 先确定本步目标功率，再基于该功率计算电气量，避免保护判断滞后一拍
            if (_blackStartEnabled)
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

            PublishPortsFromState();

            // 4. 更新充放电能量统计（正放负充：ActivePower>0为放电，<0为充电）
            double energyChange = _currentState.ActivePower * intStep.TotalHours; // kWh
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
            _currentState.DcCurrent = _currentState.DcVoltage > 1.0
                ? dcPower * 1000 / _currentState.DcVoltage
                : 0;

            if (_blackStartEnabled && _blackStartPhase == BlackStartPhase.Preparing)
            {
                _currentState.AcVoltage = 0;
                _currentState.Frequency = 0;
                _currentState.AcCurrent = 0;
                return;
            }

            double nom = Math.Max(_config.AcVoltageNominal, 1.0);
            double acV = _unitBusVoltageV > nom * 0.08
                ? _unitBusVoltageV
                : Math.Max(_currentState.IslandVoltageEffectiveV, 1.0);
            _currentState.AcVoltage = acV;
            _currentState.Frequency = _blackStartEnabled
                ? _blackStartIslandFreqHz
                : _config.FrequencyNominal;

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
