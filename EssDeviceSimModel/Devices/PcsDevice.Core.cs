using System;
using System.Collections.Generic;
using System.Text;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssDeviceSimModel.Diagnostics;
using EssSimulator.EssDeviceSimModel.Interface;
using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Devices
{
    /// <summary>
    /// PCS 设备模型（阶段 5）：跟网/离网、功率爬坡、黑启动、保护、AC/DC 端口。
    /// PCS 设备模型：爬坡、黑启动、保护、跟网/离网；ESS 与电气网络共用同一实例。
    /// </summary>
    public sealed partial class PcsDevice : IPcsDevice, IElectricalLossSource, ITemperatureAware
    {
        private readonly PcsDeviceConfig _deviceConfig;
        public PcsConfiguration _config { get; }
        public string DeviceId { get; }
        /// <summary>日志/界面友好名，如 pcs1。</summary>
        public string DisplayLabel { get; set; }
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

        // 暂态建模
        private readonly double _transientSubStepSec;
        private readonly double _voltageControllerTauSec;
        private double _transientAcVoltageV;       // PI 滞后后的瞬时电压
        private double _prevSubStepAcVoltageV;     // 上一子步电压(算 dV/dt)
        private double _dvDt;                      // 当前 dV/dt(V/s)
        private readonly double _dvDtTripThresholdVPerSec;
        private readonly double _dvDtRideThroughLimitMs;
        private double _dvDtRideThroughMs;         // dV/dt 越限累计时间
        // 涌流波形
        private readonly double _inrushPeakA;
        private readonly double _inrushTauSec;
        private readonly double _inrushTriggerFrac;
        private bool _inrushTriggered;
        private double _inrushElapsedSec;
        private double _inrushCurrentA;
        private double _inrushPeakThisTick;

        public PcsDevice(string deviceId, PcsDeviceConfig deviceConfig, double ambientTemp = 25.0)
        {
            DeviceId = deviceId;
            DisplayLabel = deviceId;
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
                MaxCurrent = deviceConfig.MaxCurrentA,
                TransientSubStepMs = deviceConfig.TransientSubStepMs,
                VoltageControllerTauMs = deviceConfig.VoltageControllerTauMs,
                InrushPeakMultiplier = deviceConfig.InrushPeakMultiplier,
                InrushDecayTauMs = deviceConfig.InrushDecayTauMs,
                InrushTriggerVoltageFrac = deviceConfig.InrushTriggerVoltageFrac,
                DvDtTripThresholdVPerSec = deviceConfig.DvDtTripThresholdVPerSec,
                DvDtRideThroughMs = deviceConfig.DvDtRideThroughMs
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
            // 暂态参数初始化
            _transientSubStepSec = Math.Max(0.001, _config.TransientSubStepMs / 1000.0);
            _voltageControllerTauSec = Math.Max(0.001, _config.VoltageControllerTauMs / 1000.0);
            _inrushPeakA = _config.MaxCurrent * Math.Max(1.0, _config.InrushPeakMultiplier);
            _inrushTauSec = Math.Max(0.01, _config.InrushDecayTauMs / 1000.0);
            _inrushTriggerFrac = Math.Clamp(_config.InrushTriggerVoltageFrac, 0.01, 0.5);
            _dvDtTripThresholdVPerSec = Math.Max(1.0, _config.DvDtTripThresholdVPerSec);
            _dvDtRideThroughLimitMs = Math.Max(1.0, _config.DvDtRideThroughMs);
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

            double acP = _currentState.ActivePower;
            double q = _currentState.ReactivePower;
            double acV = _currentState.GMode == GridMode.GridConnected
                ? _currentState.AcVoltage
                : Math.Max(_currentState.IslandVoltageEffectiveV, _currentState.AcVoltage);

            var acOut = AcQuantityConverter.FromLineVoltageAndPower(
                Math.Max(acV, 1.0),
                acP,
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

        /// <summary>当前待爬坡有功设定（kW），供边沿同步测试断言命令是否被覆盖。</summary>
        internal double PendingActiveSetpoint => _pendingActiveSetpoint;

        /// <summary>网侧是否视为带电可用（单元高压分闸或主网失电时为 false）。与 EMS 启停配合时用于避免与主循环 Standby 对打。</summary>
        public bool IsGridElectricallyAvailable => _gridState.IsAvailable;

        /// <summary>外部启停命令（Modbus pcsOnOffSwitch）。运行模式仅在外部写 1（0→1 边沿）或已非停机时可进入 Normal/Standby。</summary>
        public bool IsExternalRunCommand => _externalRunCommand;

        /// <summary>本周期是否检测到外部启停 0→1 边沿（每周期在 SyncExternalRunCommand 后读取一次）。</summary>
        public bool ExternalRunRisingEdge => _externalRunRisingEdge;

        /// <summary>已发生故障跳闸并锁存，启停线圈已自动回 0；EMS 写 1 可清除故障并重启。</summary>
        public bool HasLatchedFaultTrip => _faultTripLatched;

        /// <summary>故障跳闸后撤回启停令（不清除故障锁存，区别于 EMS 写 0 的 <see cref="SyncExternalRunCommand"/>）。</summary>
        public void WithdrawExternalRunCommand()
        {
            if (!_externalRunCommand)
                return;
            _externalRunCommand = false;
            _externalRunRisingEdge = false;
        }

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
                    TransitionToMode(OperationMode.Off, "EMS/Modbus 启停写 0");
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
            if (!_faultTripLatched && _currentState.FaultType == 0)
                return;
            _faultTripLatched = false;
            _latchedFaultType = 0;
            _latchedFaultMessage = null;
            _currentState.FaultType = 0;
            _currentState.FaultMessage = null;
            SimStateChangeLogger.PcsFaultChanged(DisplayLabel, 0, null, cleared: true);
        }

        private void LatchFault(ushort faultType, string message)
        {
            if (faultType == 0)
                return;
            bool wasLatched = _faultTripLatched;
            ushort prevType = _latchedFaultType;
            _faultTripLatched = true;
            _latchedFaultType = faultType;
            if (string.IsNullOrEmpty(_latchedFaultMessage))
                _latchedFaultMessage = message;
            else if (!string.IsNullOrEmpty(message) && _latchedFaultMessage.IndexOf(message, StringComparison.Ordinal) < 0)
                _latchedFaultMessage += message;
            _currentState.FaultType = _latchedFaultType;
            _currentState.FaultMessage = _latchedFaultMessage;
            if (!wasLatched || prevType != _latchedFaultType)
                SimStateChangeLogger.PcsFaultChanged(DisplayLabel, _latchedFaultType, _latchedFaultMessage, cleared: false);
        }

        /// <summary>
        /// 计算网侧有功功率（kW）。
        /// 约定：正值表示向电网送电，负值表示从电网取电。
        /// - 放电时（P>=0）按线损效率折减；
        /// - 充电时（P<0）按线损效率反推网侧取电。
        /// 用于潮流/母线汇总与端口传播，不用于 PCS 交流过流保护。
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
                TransitionToGMode(GridMode.Islanded, "主网/母线失电");
            else if (isUtilityGridAvailable && _currentState.GMode == GridMode.Islanded && !_blackStartEnabled)
                TransitionToGMode(GridMode.GridConnected, "主网/母线恢复");

            if (!isUtilityGridAvailable && !_blackStartEnabled)
                StopRampsAndZeroPower();
            else if (IsBlackStartActive && _currentState.Mode == OperationMode.Normal)
                _rampStopRequested = false;
        }

        // 模式切换
        public void TransitionToMode(OperationMode newMode, string? reason = null)
        {
            if (_currentState.Mode == newMode) return;
            var from = _currentState.Mode;
            _currentState.Mode = newMode;
            SimStateChangeLogger.PcsModeChanged(DisplayLabel, from, newMode, reason ?? "未指定");

            // 非 Normal 模式不应继续执行功率指令
            if (newMode != OperationMode.Normal)
            {
                StopRampsAndZeroPower();
            }
        }

        public void TransitionToGMode(GridMode newMode, string? reason = null)
        {
            if (_currentState.GMode == newMode) return;

            // 模式切换前检查
            if (newMode == GridMode.GridConnected && !_gridState.IsAvailable)
            {
                throw new InvalidOperationException("Cannot connect to grid when grid is not available");
            }

            var from = _currentState.GMode;
            _currentState.GMode = newMode;
            SimStateChangeLogger.PcsGridModeChanged(DisplayLabel, from, newMode, reason ?? "未指定");
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

            double prevP;
            double prevQ;
            lock (_setpointLock)
            {
                prevP = _pendingActiveSetpoint;
                prevQ = _pendingReactiveSetpoint;
                _rampStopRequested = false;
                _pendingActiveSetpoint = activePower;
                _pendingReactiveSetpoint = reactivePower;
                if (_rampDelayRemainingSec <= 0 && _delay > 0)
                    _rampDelayRemainingSec = _delay / 1000.0;
            }

            SimStateChangeLogger.PcsPowerSetpointChanged(DisplayLabel, prevP, activePower, prevQ, reactivePower);
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

            // 子步暂态建模:10ms 粒度的电压 PI、dV/dt、涌流
            int subSteps = Math.Max(1, (int)Math.Round(
                timeStep.TotalMilliseconds / (_transientSubStepSec * 1000)));
            TimeSpan subStep = TimeSpan.FromTicks(timeStep.Ticks / subSteps);
            _inrushPeakThisTick = 0;
            for (int i = 0; i < subSteps; i++)
            {
                UpdateTransientVoltage(subStep);
                UpdateInrushCurrent(subStep);
            }
            // 发布暂态结果到 PcsState
            _currentState.DvDt = _dvDt;
            _currentState.InrushCurrentA = _inrushCurrentA;
            _currentState.InrushPeakA = _inrushPeakThisTick;
            _currentState.ProtectionFlags = ComputeProtectionFlags();

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
                    _currentState.FaultType == 4 ||
                    _currentState.FaultType == 5 ||
                    (_currentState.FaultType == 1 && _currentState.ActivePower < 0) ||
                    (_currentState.FaultType == 2 && _currentState.ActivePower > 0))
                {
                    var tripReason = _currentState.FaultMessage ?? $"FaultType={_currentState.FaultType}";
                    TransitionToMode(OperationMode.Off, $"故障跳闸: {tripReason.Trim()}");
                    UpdateStandbyState();
                    WithdrawExternalRunCommand();
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

            // 过流/遥测：按 PCS 交流口 P/Q 与端电压算电流（线损不改变交流口电流）
            ApplyGridConnectedAcCurrent();
        }

        /// <summary>
        /// 并网模式下交流电流：由交流口 ActivePower/ReactivePower 与 AcVoltage 推算。
        /// 线损仅体现在 <see cref="GetGridSideActivePower"/> 的电网侧功率计量，不参与过流判定。
        /// </summary>
        private void ApplyGridConnectedAcCurrent()
        {
            double acCurrentMag = ComputeAcCurrentMagnitude(
                _currentState.ActivePower,
                _currentState.ReactivePower,
                _currentState.AcVoltage);
            // 正=从网侧取电（充电），负=向网侧送电（放电）
            _currentState.AcCurrent = _currentState.ActivePower >= 0 ? -acCurrentMag : acCurrentMag;
        }

        private static double ComputeAcCurrentMagnitude(double activeKw, double reactiveKvar, double lineVoltageV)
        {
            double apparentKva = Math.Sqrt(activeKw * activeKw + reactiveKvar * reactiveKvar);
            double denomU = Math.Max(lineVoltageV, 10.0);
            return apparentKva * 1000 / (denomU * Math.Sqrt(3));
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
            // 暂态建模:优先使用 PI 滞后后的瞬时电压
            _currentState.AcVoltage = _transientAcVoltageV > 1.0
                ? _transientAcVoltageV
                : acV;
            _currentState.Frequency = _blackStartEnabled
                && EssIslandBusLogic.IsPcsIslandVoltageBuilding(_currentState)
                ? _blackStartIslandFreqHz
                : 0;

            // 计算交流电流（带符号，正=放电，负=充电），叠加涌流电流
            double acCurrentMag = ComputeAcCurrentMagnitude(
                _currentState.ActivePower,
                _currentState.ReactivePower,
                _currentState.AcVoltage);
            double totalCurrentMag = acCurrentMag + _inrushCurrentA;
            _currentState.AcCurrent = _currentState.ActivePower >= 0 ? totalCurrentMag : -totalCurrentMag;
        }

        /// <summary>子步暂态:电压一阶滞后 + dV/dt 计算。</summary>
        private void UpdateTransientVoltage(TimeSpan subStep)
        {
            double dt = subStep.TotalSeconds;
            double vTarget = _currentState.IslandVoltageEffectiveV;

            // 一阶滞后:模拟电压环 PI 控制器响应
            double alpha = 1.0 - Math.Exp(-dt / _voltageControllerTauSec);
            _transientAcVoltageV += (vTarget - _transientAcVoltageV) * alpha;

            // dV/dt 计算(子步粒度)
            if (dt > 1e-9)
                _dvDt = (_transientAcVoltageV - _prevSubStepAcVoltageV) / dt;
            _prevSubStepAcVoltageV = _transientAcVoltageV;

            // dV/dt 穿越计时
            if (Math.Abs(_dvDt) > _dvDtTripThresholdVPerSec)
                _dvDtRideThroughMs += dt * 1000;
            else
                _dvDtRideThroughMs = Math.Max(0, _dvDtRideThroughMs - dt * 1000 * 2); // 2倍速衰减
        }

        /// <summary>子步暂态:变压器励磁涌流电流触发/峰值/指数衰减。</summary>
        private void UpdateInrushCurrent(TimeSpan subStep)
        {
            double dt = subStep.TotalSeconds;
            double nomV = Math.Max(_config.AcVoltageNominal, 1.0);

            // 触发条件:电压首次穿越门槛值(变压器铁芯开始磁化)
            if (!_inrushTriggered && _transientAcVoltageV > nomV * _inrushTriggerFrac)
            {
                _inrushTriggered = true;
                _inrushElapsedSec = 0;
            }

            if (_inrushTriggered)
            {
                _inrushElapsedSec += dt;
                double decay = Math.Exp(-_inrushElapsedSec / _inrushTauSec);
                _inrushCurrentA = _inrushPeakA * decay;
                _inrushPeakThisTick = Math.Max(_inrushPeakThisTick, _inrushCurrentA);

                // 涌流衰减到可忽略
                if (_inrushCurrentA < 1.0)
                {
                    _inrushTriggered = false;
                    _inrushCurrentA = 0;
                }
            }
        }

        /// <summary>计算保护标志位(bit0=dV/dt越限, bit1=dV/dt跳闸, bit2=涌流激活, bit3=涌流过流)。</summary>
        private ushort ComputeProtectionFlags()
        {
            ushort flags = 0;
            if (Math.Abs(_dvDt) > _dvDtTripThresholdVPerSec) flags |= 0x01;
            if (_dvDtRideThroughMs > _dvDtRideThroughLimitMs) flags |= 0x02;
            if (_inrushTriggered) flags |= 0x04;
            if (_inrushCurrentA > _config.MaxCurrent) flags |= 0x08;
            return flags;
        }

        private void UpdateTemperatureModel(TimeSpan timeStep)
        {
            // 计算功率损耗 (简化模型)；ActivePower 为 kW
            double powerLossKw = Math.Abs(_currentState.ActivePower) * (1 - _config.Efficiency);

            // 温度变化计算（沿用历史单位约定）
            double cooling = (_currentState.Temperature - _ambientTemperature) * 50; // 假设冷却系数50W/°C
            double tempChange = (powerLossKw - cooling) * timeStep.TotalHours / 10.0; // 假设热容10kWh/°C

            _currentState.Temperature = Math.Max(_ambientTemperature, _currentState.Temperature + tempChange);
        }

        /// <inheritdoc />
        public void ApplyAmbientTemperature(double ambientCelsius) => _ambientTemperature = ambientCelsius;

        /// <inheritdoc />
        public double TemperatureCelsius => _currentState.Temperature;

        /// <inheritdoc />
        public double GetElectricalLossWatts() =>
            Math.Abs(_currentState.ActivePower) * (1.0 - _config.Efficiency) * 1000.0;

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

            // dV/dt 保护(带穿越时间)
            if (_dvDtRideThroughMs > _dvDtRideThroughLimitMs)
            {
                instantFault = 4;
                msg.Append($"dV/dt protection: {_dvDt:F0}V/s (limit {_dvDtTripThresholdVPerSec:F0}V/s), " +
                           $"ride-through {_dvDtRideThroughMs:F0}ms; ");
            }

            // 涌流过流保护:瞬时峰值允许超过额定,仅极端情况(超 3 倍额定)才跳闸
            if (_inrushTriggered && _inrushCurrentA > _config.MaxCurrent * 3.0)
            {
                instantFault = 5;
                msg.Append($"Inrush overcurrent: {_inrushCurrentA:F0}A " +
                           $"(limit {_config.MaxCurrent * 3.0:F0}A); ");
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
