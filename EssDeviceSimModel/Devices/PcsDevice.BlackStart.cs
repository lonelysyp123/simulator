using System;

namespace EssSimulator.EssDeviceSimModel.Devices
{
    public sealed partial class PcsDevice
    {
        /// <summary>EMS/Modbus 写入孤岛电压设定（V）。稳态同步模式下忽略。</summary>
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
                ResetBlackStartRuntime();
                return;
            }

            lock (_setpointLock)
            {
                _rampStopRequested = true;
                _pendingActiveSetpoint = 0;
                _pendingReactiveSetpoint = 0;
            }

            _loadActivePowerKw = 0;
            _loadReactivePowerKvar = 0;
            _blackStartPhase = BlackStartPhase.Preparing;
            _currentState.BlackStartPhase = BlackStartPhase.Preparing;
            _blackStartPrepareRemainingSec = _blackStartPrechargeDelaySec;
            _blackStartSoftCapV = 0;
            _blackStartIslandFreqHz = _blackStartFrequencyStartHz;
            _blackStartInrushActiveKw = 0;
            _blackStartInrushReactiveKvar = 0;
        }

        /// <summary>主循环刷新同单元 690V 母线电压，判定同步/跌落。</summary>
        public void RefreshBlackStartBusContext(double unitBusVoltageV)
        {
            _unitBusVoltageV = Math.Max(0, unitBusVoltageV);
            if (!_blackStartEnabled)
            {
                ResetBlackStartRuntime();
                return;
            }

            double nom = Math.Max(_config.AcVoltageNominal, 1.0);
            double energizedV = nom * _blackStartBusEnergizedFraction;

            if (_blackStartPhase is BlackStartPhase.VoltageRegulating or BlackStartPhase.Synchronized
                && _unitBusVoltageV >= energizedV)
            {
                _blackStartPhase = BlackStartPhase.Synchronized;
                _blackStartIslandFreqHz = _config.FrequencyNominal;
            }
            else if (_blackStartPhase == BlackStartPhase.Synchronized && _unitBusVoltageV < energizedV * 0.92)
            {
                _blackStartPhase = BlackStartPhase.VoltageRegulating;
            }

            _currentState.BlackStartPhase = _blackStartPhase;
            PublishBlackStartEffectiveVoltage();
        }

        public BlackStartPhase GetBlackStartPhase() => _blackStartPhase;

        public bool IsBlackStartSynchronized =>
            _blackStartEnabled && _blackStartPhase == BlackStartPhase.Synchronized;

        public bool IsBlackStartActive =>
            _blackStartEnabled &&
            _currentState.Mode == OperationMode.Normal &&
            _currentState.GMode == GridMode.Islanded &&
            _blackStartPhase is BlackStartPhase.SoftStarting
                or BlackStartPhase.VoltageRegulating
                or BlackStartPhase.Synchronized;

        public void SetTransformerMagnetizingReactiveKvar(double reactiveKvar) =>
            _transformerMagnetizingReactiveKvar = Math.Max(0, reactiveKvar);

        public void SetBlackStartSharedLossActivePowerKw(double activeKw) =>
            _blackStartSharedLossActivePowerKw = Math.Max(0, activeKw);

        public void SetBlackStartInrushDemand(double activeKw, double reactiveKvar)
        {
            _blackStartInrushActiveKw = Math.Max(0, activeKw);
            _blackStartInrushReactiveKvar = Math.Max(0, reactiveKvar);
        }

        private void AdvanceBlackStartPhase(TimeSpan timeStep)
        {
            if (!_blackStartEnabled)
                return;

            double dt = Math.Max(timeStep.TotalSeconds, 1e-6);
            double nom = Math.Max(_config.AcVoltageNominal, 1.0);
            double vCmd;
            lock (_islandVfLock)
                vCmd = _islandVfCommandV;

            switch (_blackStartPhase)
            {
                case BlackStartPhase.Preparing:
                    _blackStartPrepareRemainingSec -= dt;
                    if (_blackStartPrepareRemainingSec <= 0 && IsDcBusReadyForBlackStart())
                        _blackStartPhase = BlackStartPhase.SoftStarting;
                    break;

                case BlackStartPhase.SoftStarting:
                    _blackStartSoftCapV = Math.Min(vCmd, _blackStartSoftCapV + _blackStartVoltageRampVs * dt);
                    AdvanceBlackStartFrequency(dt, nom);
                    if (_blackStartSoftCapV >= vCmd - 1.0 || _unitBusVoltageV >= nom * 0.35)
                        _blackStartPhase = BlackStartPhase.VoltageRegulating;
                    break;

                case BlackStartPhase.VoltageRegulating:
                    _blackStartSoftCapV = Math.Min(vCmd, _blackStartSoftCapV + _blackStartVoltageRampVs * dt);
                    AdvanceBlackStartFrequency(dt, nom);
                    break;

                case BlackStartPhase.Synchronized:
                    _blackStartIslandFreqHz = _config.FrequencyNominal;
                    break;
            }

            _currentState.BlackStartPhase = _blackStartPhase;
        }

        private void AdvanceBlackStartFrequency(double dt, double nominalVoltageV)
        {
            double fTarget = _config.FrequencyNominal;
            double vFrac = nominalVoltageV > 1.0
                ? Math.Clamp(_blackStartSoftCapV / nominalVoltageV, 0, 1)
                : 0;
            double fFromVoltage = _blackStartFrequencyStartHz + (fTarget - _blackStartFrequencyStartHz) * vFrac;
            double maxStep = _blackStartFrequencyRampHzPerSec * dt;
            if (_blackStartIslandFreqHz < fFromVoltage)
                _blackStartIslandFreqHz = Math.Min(fFromVoltage, _blackStartIslandFreqHz + maxStep);
            else if (_blackStartIslandFreqHz > fFromVoltage)
                _blackStartIslandFreqHz = Math.Max(fFromVoltage, _blackStartIslandFreqHz - maxStep);
        }

        private bool IsDcBusReadyForBlackStart() =>
            _currentState.DcVoltage >= _config.DcVoltageRangeMin * 0.95;

        private void ApplyBlackStartPowerControl(TimeSpan timeStep)
        {
            if (!IsBlackStartActive)
            {
                if (_blackStartEnabled && _blackStartPhase == BlackStartPhase.Preparing)
                {
                    _currentState.ActivePower = 0;
                    _currentState.ReactivePower = 0;
                }
                return;
            }

            double nom = Math.Max(_config.AcVoltageNominal, 1.0);
            double vCmd;
            lock (_islandVfLock)
                vCmd = _islandVfCommandV;

            double vTarget = _blackStartPhase == BlackStartPhase.Synchronized
                ? vCmd
                : Math.Min(vCmd, _blackStartSoftCapV);

            double vControl = SelectBlackStartControlVoltage(nom, vTarget);
            double loadP = _loadActivePowerKw;
            double loadQ = _loadReactivePowerKvar;
            double stationP = _blackStartSharedLossActivePowerKw;
            double stationQ = _transformerMagnetizingReactiveKvar;

            double pBuild = _blackStartActivePowerGainKwPerVolt * Math.Max(0, vTarget - vControl);
            double qBuild = _blackStartReactiveVoltageGainKvarPerV * Math.Max(0, nom - vControl);

            double targetP = stationP + loadP + pBuild + _blackStartInrushActiveKw;
            double targetQ = stationQ + loadQ + qBuild + _blackStartInrushReactiveKvar;

            if (_blackStartPhase != BlackStartPhase.Synchronized)
            {
                double maxStep = _blackStartMaxActivePowerKw * Math.Max(timeStep.TotalSeconds, 1e-6);
                maxStep = Math.Max(maxStep, 0.5);
                double desiredP = targetP;
                double currentP = _currentState.ActivePower;
                if (desiredP > currentP + maxStep)
                    targetP = currentP + maxStep;
                else if (desiredP < currentP - maxStep)
                    targetP = Math.Max(0, desiredP);
            }

            targetP = Math.Clamp(targetP, -_config.MaxPower, _config.MaxPower);
            targetQ = Math.Clamp(targetQ, -_config.MaxPower, _config.MaxPower);
            ClampApparentPower(ref targetP, ref targetQ, _config.RatedPower);
            ApplyBlackStartCurrentLimit(ref targetP, ref targetQ, vControl);

            _currentState.ActivePower = targetP;
            _currentState.ReactivePower = targetQ;
        }

        private double SelectBlackStartControlVoltage(double nominalVoltageV, double vTarget)
        {
            if (_unitBusVoltageV > nominalVoltageV * 0.08)
                return _unitBusVoltageV;

            double freqRatio = Math.Clamp(_blackStartIslandFreqHz / Math.Max(_config.FrequencyNominal, 1.0), 0, 1);
            return Math.Max(vTarget * freqRatio, 1.0);
        }

        private void ApplyBlackStartCurrentLimit(ref double activeKw, ref double reactiveKvar, double voltageV)
        {
            if (_blackStartPhase == BlackStartPhase.Synchronized)
                return;

            double iLimit = _config.MaxCurrent * _blackStartCurrentLimitFraction;
            double denomV = Math.Max(voltageV, 10.0);
            double apparentKva = Math.Sqrt(activeKw * activeKw + reactiveKvar * reactiveKvar);
            double currentMag = apparentKva * 1000.0 / (denomV * Math.Sqrt(3.0));
            if (currentMag <= iLimit || currentMag < 1e-6)
                return;

            double scale = iLimit / currentMag;
            activeKw *= scale;
            reactiveKvar *= scale;
        }

        private void UpdateIslandVoltageEffectiveTowardCommand(TimeSpan timeStep)
        {
            if (_blackStartEnabled)
            {
                PublishBlackStartEffectiveVoltage();
                lock (_islandVfLock)
                    _currentState.IslandVoltageCommandV = _islandVfCommandV;
                return;
            }

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

        private void PublishBlackStartEffectiveVoltage()
        {
            double nom = Math.Max(_config.AcVoltageNominal, 1.0);
            double vDisplay = 0;
            if (_blackStartPhase != BlackStartPhase.Preparing && _blackStartPhase != BlackStartPhase.Inactive)
            {
                if (_unitBusVoltageV > nom * 0.08)
                    vDisplay = _unitBusVoltageV;
                else
                {
                    double freqRatio = Math.Clamp(
                        _blackStartIslandFreqHz / Math.Max(_config.FrequencyNominal, 1.0), 0, 1);
                    vDisplay = _blackStartSoftCapV * freqRatio;
                }
            }

            lock (_islandVfLock)
            {
                _islandVfEffectiveV = vDisplay;
                _currentState.IslandVoltageEffectiveV = vDisplay;
            }
        }

        private void ResetBlackStartRuntime()
        {
            _blackStartPhase = BlackStartPhase.Inactive;
            _currentState.BlackStartPhase = BlackStartPhase.Inactive;
            _blackStartPrepareRemainingSec = 0;
            _blackStartSoftCapV = 0;
            _blackStartIslandFreqHz = _blackStartFrequencyStartHz;
            _blackStartInrushActiveKw = 0;
            _blackStartInrushReactiveKvar = 0;
        }
    }
}
