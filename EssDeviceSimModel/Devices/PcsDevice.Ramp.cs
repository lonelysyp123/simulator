using System;

namespace EssSimulator.EssDeviceSimModel.Devices
{
    public sealed partial class PcsDevice
    {
        private void StopRampsAndZeroPower()
        {
            lock (_setpointLock)
            {
                _pendingActiveSetpoint = 0;
                _pendingReactiveSetpoint = 0;
                _rampStopRequested = true;
            }
            _currentState.ActivePower = 0;
            _currentState.ReactivePower = 0;
            _loadActivePowerKw = 0;
            _loadReactivePowerKvar = 0;
        }

        /// <summary>仿真步内功率爬坡（替代原后台线程，与主循环时间轴一致）。</summary>
        private void AdvancePowerRamps(TimeSpan timeStep)
        {
            if (_rampStopRequested || !CanAcceptPowerCommand || IsBlackStartActive)
                return;

            if (_rampDelayRemainingSec > 0)
            {
                _rampDelayRemainingSec = Math.Max(0, _rampDelayRemainingSec - timeStep.TotalSeconds);
                return;
            }

            double maxDelta = ComputeRampMaxDelta(timeStep, _activeRampCurve);
            lock (_setpointLock)
            {
                _loadActivePowerKw = MoveToward(_loadActivePowerKw, _pendingActiveSetpoint, maxDelta);
                _loadReactivePowerKvar = MoveToward(_loadReactivePowerKvar, _pendingReactiveSetpoint, maxDelta);
            }

            if (!IsBlackStartActive)
            {
                _currentState.ActivePower = _loadActivePowerKw;
                _currentState.ReactivePower = _loadReactivePowerKvar;
            }
        }

        private double ComputeRampMaxDelta(TimeSpan timeStep, RampCurve curve)
        {
            double simMs = timeStep.TotalMilliseconds;
            int fullIntervals = (int)(simMs / _interval);
            double remainderMs = simMs - fullIntervals * _interval;

            double PerInterval(RampCurve c) => c switch
            {
                RampCurve.Linear => _slope * _interval,
                RampCurve.Quadratic => _slope * _interval * _interval,
                RampCurve.SquareRoot => _slope * Math.Sqrt(_interval),
                _ => _slope * _interval
            };

            double Partial(RampCurve c, double ms) => c switch
            {
                RampCurve.Linear => _slope * ms,
                RampCurve.Quadratic => _slope * ms * ms,
                RampCurve.SquareRoot => _slope * Math.Sqrt(Math.Max(ms, 0)),
                _ => _slope * ms
            };

            return fullIntervals * PerInterval(curve) + Partial(curve, remainderMs);
        }

        private static double MoveToward(double current, double target, double maxDelta)
        {
            double diff = target - current;
            if (Math.Abs(diff) <= maxDelta)
                return target;
            return current + Math.Sign(diff) * maxDelta;
        }
    }
}
