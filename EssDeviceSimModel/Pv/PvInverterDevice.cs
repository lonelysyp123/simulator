using EssSimulator.EssDeviceSimModel;

namespace EssSimulator.EssDeviceSimModel.Pv
{
    /// <summary>
    /// 光伏组串逆变器：控制面参考储能 PCS（启停、有功/无功、爬坡、并网），
    /// 直流侧为 16 簇光伏而非电池，有功只能向电网送电（不能充电）。
    /// </summary>
    public sealed class PvInverterDevice
    {
        public const int DefaultStringCount = 16;

        /// <summary>6 路 MPPT 上的组串分配：3+3+3+3+2+2 = 16。</summary>
        private static readonly int[] StringsPerMppt = { 3, 3, 3, 3, 2, 2 };

        private readonly PvInverterConfig _config;
        private readonly PvStringSimulator[] _strings;
        private readonly PcsState _state = new();
        private readonly GridState _grid = new();
        private readonly double[] _stringCurrents;
        private readonly double[] _mpptVoltage;
        private readonly double[] _mpptCurrent;

        private bool _runCommand;
        private double _pendingActiveKw;
        private double _pendingReactiveKvar;
        private double _rampedActiveKw;
        private double _rampedReactiveKvar;
        private bool _rampStop;
        private bool _dcOvervoltage;
        private bool _dcUndervoltage;

        public PvInverterDevice(string deviceId, PvInverterConfig config, PvModuleSimulator? module = null)
        {
            DeviceId = deviceId;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            var cell = module ?? PvModuleSimulator.CreateNeg21c20q();
            _strings = new PvStringSimulator[_config.StringCount];
            for (int i = 0; i < _strings.Length; i++)
                _strings[i] = new PvStringSimulator(cell, _config.ModulesPerString);
            _stringCurrents = new double[_config.StringCount];
            _mpptVoltage = new double[StringsPerMppt.Length];
            _mpptCurrent = new double[StringsPerMppt.Length];
            _pendingActiveKw = _config.RatedPowerKw;
            DisplayLabel = deviceId;
        }

        public static PvInverterDevice Create320kW(string deviceId) =>
            new(deviceId, new PvInverterConfig());

        public string DeviceId { get; }
        public string DisplayLabel { get; set; }
        public int StringCount => _config.StringCount;
        public int ModulesPerString => _config.ModulesPerString;
        public int TotalModuleCount => StringCount * ModulesPerString;
        public double RatedPowerKw => _config.RatedPowerKw;
        public double AvailableDcPowerKw { get; private set; }
        public IReadOnlyList<double> StringCurrentsA => _stringCurrents;
        public IReadOnlyList<double> MpptVoltageV => _mpptVoltage;
        public IReadOnlyList<double> MpptCurrentA => _mpptCurrent;
        public string LimitReason { get; private set; } = "停机";

        public static readonly string[] LimitReasonPriority =
        {
            "停机", "电网断开", "辐照不足", "直流过压", "直流欠压", "低温降额", "有功设定", "已达额定", "正常"
        };

        public static string WorstLimitReason(IEnumerable<string> reasons)
        {
            int best = LimitReasonPriority.Length;
            string found = "正常";
            foreach (var r in reasons)
            {
                int i = Array.IndexOf(LimitReasonPriority, r);
                if (i >= 0 && i < best)
                {
                    best = i;
                    found = r;
                }
            }
            return found;
        }

        public PcsState GetCurrentState() => _state;

        public void SyncExternalRunCommand(bool run)
        {
            bool rising = run && !_runCommand;
            if (!run && _state.Mode != OperationMode.Off)
                TransitionToMode(OperationMode.Off);
            if (rising)
            {
                _pendingActiveKw = _config.RatedPowerKw;
                _rampStop = false;
            }
            _runCommand = run;
        }

        public void TransitionToMode(OperationMode newMode)
        {
            if (_state.Mode == newMode)
                return;
            _state.Mode = newMode;
            if (newMode != OperationMode.Normal)
                StopRampsAndZeroPower();
        }

        public void UpdateGridState(double voltage, double frequency, bool isUtilityGridAvailable)
        {
            _grid.Voltage = voltage / (1 - _config.GridLossCoefficient);
            _grid.Frequency = frequency;
            _grid.IsAvailable = isUtilityGridAvailable;
            if (!isUtilityGridAvailable)
                StopRampsAndZeroPower();
        }

        /// <summary>
        /// 有功为限发值（kW），不能为负；实际出力 = min(限值, 阵列可发, 额定)。
        /// 无功可正可负，受视在功率约束。
        /// </summary>
        public void SetPowerCommand(double activePowerKw, double reactivePowerKvar = 0)
        {
            _pendingActiveKw = Math.Clamp(activePowerKw, 0, _config.MaxPowerKw);
            _pendingReactiveKvar = Math.Clamp(reactivePowerKvar, -_config.MaxPowerKw, _config.MaxPowerKw);
            _rampStop = false;
        }

        public void Update(double gFrontWm2, double cellTempC, DateTime timeStamp, TimeSpan timeStep, double gRearWm2 = 0)
        {
            if (_state.Timestamp.Date != timeStamp.Date)
                _state.DailyDischargeEnergy = 0;
            _state.Timestamp = timeStamp;

            EvaluateArray(gFrontWm2, cellTempC, gRearWm2);
            AvailableDcPowerKw *= ColdTemperatureScale(cellTempC);

            bool canRun = _runCommand && _grid.IsAvailable && _state.Mode == OperationMode.Normal;
            double availableAcKw = AvailableDcPowerKw * _config.Efficiency;
            double pTarget = canRun ? Math.Min(_pendingActiveKw, Math.Min(availableAcKw, _config.RatedPowerKw)) : 0;
            double qTarget = canRun ? _pendingReactiveKvar : 0;
            ClampApparent(ref pTarget, ref qTarget, _config.RatedPowerKw * 1.1);

            AdvanceRamp(pTarget, qTarget, timeStep);

            _state.ActivePower = canRun ? _rampedActiveKw : 0;
            _state.ReactivePower = canRun ? _rampedReactiveKvar : 0;
            if (_state.ActivePower < 0)
                _state.ActivePower = 0;

            ApplyElectrical(canRun, cellTempC);
            AccumulateEnergy(timeStep);
            LimitReason = ClassifyLimitReason(gFrontWm2, cellTempC, availableAcKw);
        }

        private string ClassifyLimitReason(double gFrontWm2, double cellTempC, double availableAcKw)
        {
            if (!_runCommand || _state.Mode != OperationMode.Normal)
                return "停机";
            if (!_grid.IsAvailable)
                return "电网断开";
            if (gFrontWm2 < 2)
                return "辐照不足";
            if (_dcOvervoltage)
                return "直流过压";
            if (_dcUndervoltage)
                return "直流欠压";
            if (ColdTemperatureScale(cellTempC) < 0.999)
                return "低温降额";
            double rated = _config.RatedPowerKw;
            if (_pendingActiveKw + 0.5 < Math.Min(availableAcKw, rated) && _pendingActiveKw < rated - 0.5)
                return "有功设定";
            if (availableAcKw > rated + 1 && _pendingActiveKw >= rated - 0.5)
                return "已达额定";
            return "正常";
        }

        private void EvaluateArray(double gFront, double cellTemp, double gRear)
        {
            _dcOvervoltage = false;
            _dcUndervoltage = false;
            double pDcW = 0;
            double vOpSum = 0;
            int live = 0;
            for (int i = 0; i < _strings.Length; i++)
            {
                var s = _strings[i].Evaluate(gFront, cellTemp, gRear);
                var (pW, vOp, iOp) = ConstrainStringToDcWindow(s, gFront, cellTemp, gRear);
                _stringCurrents[i] = iOp;
                pDcW += pW;
                if (vOp > 1)
                {
                    vOpSum += vOp;
                    live++;
                }
            }

            AvailableDcPowerKw = pDcW / 1000.0;
            _state.DcVoltage = live > 0 ? vOpSum / live : 0;
            FillMppt(gFront, cellTemp, gRear);
        }

        private (double PowerW, double VoltageV, double CurrentA) ConstrainStringToDcWindow(
            PvModuleOperatingPoint s, double gFront, double cellTemp, double gRear)
        {
            double vMin = _config.DcVoltageRangeMinV;
            double vMax = _config.DcVoltageRangeMaxV;
            double vocLock = vMax * 1.20;
            if (s.VocV <= 1e-9)
                return (0, 0, 0);
            if (s.VocV > vocLock)
            {
                _dcOvervoltage = true;
                return (0, 0, 0);
            }

            double vOp = s.VmpV;
            if (vOp > vMax)
                vOp = vMax;
            else if (vOp < vMin)
            {
                if (s.VocV <= vMin)
                {
                    _dcUndervoltage = true;
                    return (0, 0, 0);
                }
                vOp = vMin;
            }

            if (Math.Abs(vOp - s.VmpV) < 1e-6)
                return (s.PmpW, s.VmpV, s.ImpA);

            double iOp = _strings[0].CurrentAtVoltage(vOp, gFront, cellTemp, gRear);
            return (Math.Max(0, vOp * iOp), vOp, iOp);
        }

        private double ColdTemperatureScale(double cellTempC)
        {
            double full = _config.FullPowerMinTempC;
            double cut = _config.InhibitMinTempC;
            if (cellTempC >= full)
                return 1;
            if (cellTempC <= cut)
                return 0;
            return (cellTempC - cut) / (full - cut);
        }

        private void FillMppt(double gFront, double cellTemp, double gRear)
        {
            int offset = 0;
            for (int m = 0; m < StringsPerMppt.Length; m++)
            {
                int n = StringsPerMppt[m];
                double iSum = 0;
                double vSum = 0;
                int live = 0;
                for (int k = 0; k < n && offset + k < _strings.Length; k++)
                {
                    var s = _strings[offset + k].Evaluate(gFront, cellTemp, gRear);
                    iSum += s.ImpA;
                    if (s.VmpV > 1)
                    {
                        vSum += s.VmpV;
                        live++;
                    }
                }
                _mpptCurrent[m] = iSum;
                _mpptVoltage[m] = live > 0 ? vSum / live : 0;
                offset += n;
            }
        }

        private void ApplyElectrical(bool canRun, double cellTempC)
        {
            _state.Temperature = cellTempC;
            if (!canRun || _state.ActivePower <= 1e-6)
            {
                _state.DcCurrent = 0;
                _state.AcCurrent = 0;
                _state.AcVoltage = 0;
                _state.Frequency = 0;
                if (!canRun)
                    _state.ActivePower = 0;
                return;
            }

            double dcPowerKw = _state.ActivePower / _config.Efficiency;
            _state.DcCurrent = _state.DcVoltage > 1 ? dcPowerKw * 1000 / _state.DcVoltage : 0;
            _state.AcVoltage = _grid.Voltage * (1 - _config.GridLossCoefficient);
            _state.Frequency = _grid.Frequency;
            double s = Math.Sqrt(_state.ActivePower * _state.ActivePower + _state.ReactivePower * _state.ReactivePower);
            double iMag = s * 1000 / (Math.Max(_state.AcVoltage, 10) * Math.Sqrt(3));
            _state.AcCurrent = -iMag;
        }

        private void AdvanceRamp(double pTarget, double qTarget, TimeSpan timeStep)
        {
            if (_rampStop)
            {
                _rampedActiveKw = 0;
                _rampedReactiveKvar = 0;
                return;
            }

            double maxDelta = ComputeRampMaxDelta(timeStep);
            _rampedActiveKw = MoveToward(_rampedActiveKw, pTarget, maxDelta);
            _rampedReactiveKvar = MoveToward(_rampedReactiveKvar, qTarget, maxDelta);
        }

        private double ComputeRampMaxDelta(TimeSpan timeStep)
        {
            double simMs = timeStep.TotalMilliseconds;
            int interval = Math.Max(_config.RampIntervalMs, 1);
            int full = (int)(simMs / interval);
            double rem = simMs - full * interval;
            return full * (_config.RampSlope * interval) + _config.RampSlope * rem;
        }

        private static double MoveToward(double current, double target, double maxDelta)
        {
            double diff = target - current;
            if (Math.Abs(diff) <= maxDelta)
                return target;
            return current + Math.Sign(diff) * maxDelta;
        }

        private static void ClampApparent(ref double p, ref double q, double maxS)
        {
            double s = Math.Sqrt(p * p + q * q);
            if (s <= maxS || s < 1e-9)
                return;
            double scale = maxS / s;
            p *= scale;
            q *= scale;
        }

        private void StopRampsAndZeroPower()
        {
            _pendingActiveKw = 0;
            _pendingReactiveKvar = 0;
            _rampedActiveKw = 0;
            _rampedReactiveKvar = 0;
            _rampStop = true;
            _state.ActivePower = 0;
            _state.ReactivePower = 0;
        }

        private void AccumulateEnergy(TimeSpan step)
        {
            double kwh = Math.Max(0, _state.ActivePower) * step.TotalHours;
            _state.DailyDischargeEnergy += kwh;
            _state.TotalDischargeEnergy += kwh;
        }
    }
}
