using EssSimulator.EssDeviceSimModel.Devices;
using EssSimulator.EssDeviceSimModel.Model;
using EssSimulator.EssDeviceSimModel.Solver;

namespace EssSimulator.EssDeviceSimModel.Propagation
{
    /// <summary>双耳单元支路：35kV → 一次 Step → 左右 690V 母线。</summary>
    internal sealed class DualEarUnitBranchCoupler : IBusCoupler
    {
        private readonly BreakerSimulator _unitBreaker;
        private readonly DualEarTransformerDevice _dualEar;
        private readonly ElectricalBusNode _busLeft;
        private readonly ElectricalBusNode _busRight;

        public DualEarUnitBranchCoupler(
            int unitIndex,
            BreakerSimulator unitBreaker,
            DualEarTransformerDevice dualEar,
            ElectricalBusNode upstreamBus35,
            ElectricalBusNode busLeft,
            ElectricalBusNode busRight)
        {
            _unitBreaker = unitBreaker;
            _dualEar = dualEar;
            UpstreamBus = upstreamBus35;
            DownstreamBus = busLeft;
            _busLeft = busLeft;
            _busRight = busRight;
            CouplerId = $"unit_branch_dual_u{unitIndex}";
        }

        public string CouplerId { get; }
        public ElectricalBusNode UpstreamBus { get; }
        public ElectricalBusNode DownstreamBus { get; }

        public void Attach() =>
            UpstreamBus.RegisterVoltageHandler(OnUpstreamVoltageChanged);

        private void OnUpstreamVoltageChanged(BusVoltageChangedEventArgs args)
        {
            bool unitClosed = _unitBreaker.SwitchState.IsClosed && !_unitBreaker.SwitchState.IsTripped;
            double bus35V = args.LineVoltageV;
            double primaryV = unitClosed ? bus35V : 0;
            double freq = args.Sweep.SystemFrequencyHz;

            var leftCurrent = EarCurrent(_busLeft, primaryV, freq);
            var rightCurrent = EarCurrent(_busRight, primaryV, freq);
            var combined = AcQuantityConverter.FromLineVoltageAndPower(
                _busLeft.LineVoltageV > 1.0 ? _busLeft.LineVoltageV
                    : (_busRight.LineVoltageV > 1.0 ? _busRight.LineVoltageV : 0),
                _busLeft.TotalActivePowerKw + _busRight.TotalActivePowerKw,
                _busLeft.TotalReactivePowerKvar + _busRight.TotalReactivePowerKvar,
                ThreePhaseConnection.Star,
                freq);

            PropagationPortBinding.SetAcVoltageInput(_unitBreaker.Primary, bus35V, ThreePhaseConnection.Star);
            if (unitClosed)
                PropagationPortBinding.SetAcQuantitiesInput(_unitBreaker.Secondary, _unitBreaker.ReferToRated(combined));
            else
            {
                PropagationPortBinding.SetAcQuantitiesInput(
                    _unitBreaker.Secondary,
                    new AcInternalQuantities { Connection = ThreePhaseConnection.Star });
            }

            _unitBreaker.Step(args.Sweep.DeviceContext, args.Sweep.Step);

            PropagationPortBinding.SetAcVoltageInput(_dualEar.Primary, primaryV, ThreePhaseConnection.Star);
            PropagationPortBinding.SetAcQuantitiesInput(_dualEar.SecondaryLeft, leftCurrent);
            PropagationPortBinding.SetAcQuantitiesInput(_dualEar.SecondaryRight, rightCurrent);
            _dualEar.Step(args.Sweep.DeviceContext, args.Sweep.Step);

            double leftV = unitClosed ? _dualEar.SecondaryLeft.Output.Ac?.Internal.LineVoltageV ?? 0 : 0;
            double rightV = unitClosed ? _dualEar.SecondaryRight.Output.Ac?.Internal.LineVoltageV ?? 0 : 0;
            _busLeft.SetVoltage(leftV, leftV > 1.0 ? args.FrequencyHz : 0, args.Sweep, notifyCouplers: false);
            _busRight.SetVoltage(rightV, rightV > 1.0 ? args.FrequencyHz : 0, args.Sweep, notifyCouplers: false);
        }

        private static AcInternalQuantities EarCurrent(ElectricalBusNode bus, double primaryV, double freq)
        {
            if (primaryV > 1.0
                && (bus.TotalLineCurrentA > 1e-6
                    || Math.Abs(bus.TotalActivePowerKw) > 1e-3
                    || Math.Abs(bus.TotalReactivePowerKvar) > 1e-3))
            {
                return AcQuantityConverter.FromLineVoltageAndPower(
                    bus.LineVoltageV,
                    bus.TotalActivePowerKw,
                    bus.TotalReactivePowerKvar,
                    ThreePhaseConnection.Star,
                    freq);
            }

            return new AcInternalQuantities
            {
                LineVoltageV = primaryV > 1.0 && bus.LineVoltageV > 1.0 ? bus.LineVoltageV : 0,
                FrequencyHz = primaryV > 1.0 && bus.LineVoltageV > 1.0 ? freq : 0
            };
        }
    }
}
