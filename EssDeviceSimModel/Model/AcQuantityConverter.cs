namespace EssSimulator.EssDeviceSimModel.Model
{
    public static class AcQuantityConverter
    {
        public static AcTerminalQuantities ToTerminal(AcInternalQuantities internalQty)
        {
            double uLine = internalQty.LineVoltageV;
            double iLine = internalQty.LineCurrentA;

            return internalQty.Connection switch
            {
                ThreePhaseConnection.Star => new AcTerminalQuantities
                {
                    Connection = ThreePhaseConnection.Star,
                    Vab = uLine,
                    Vbc = uLine,
                    Vca = uLine,
                    Van = uLine / ElectricalConventions.Sqrt3,
                    Vbn = uLine / ElectricalConventions.Sqrt3,
                    Vcn = uLine / ElectricalConventions.Sqrt3,
                    Ia = iLine,
                    Ib = iLine,
                    Ic = iLine
                },
                ThreePhaseConnection.Delta => new AcTerminalQuantities
                {
                    Connection = ThreePhaseConnection.Delta,
                    Vab = uLine,
                    Vbc = uLine,
                    Vca = uLine,
                    Van = uLine,
                    Vbn = uLine,
                    Vcn = uLine,
                    Ia = iLine / ElectricalConventions.Sqrt3,
                    Ib = iLine / ElectricalConventions.Sqrt3,
                    Ic = iLine / ElectricalConventions.Sqrt3
                },
                _ => throw new ArgumentOutOfRangeException(nameof(internalQty.Connection))
            };
        }

        public static AcInternalQuantities FromTerminal(AcTerminalQuantities terminal)
        {
            double uLine = terminal.Vab;
            double iLine = terminal.Connection switch
            {
                ThreePhaseConnection.Star => terminal.Ia,
                ThreePhaseConnection.Delta => terminal.Ia * ElectricalConventions.Sqrt3,
                _ => throw new ArgumentOutOfRangeException(nameof(terminal.Connection))
            };

            return new AcInternalQuantities
            {
                Connection = terminal.Connection,
                LineVoltageV = uLine,
                LineCurrentA = Math.Abs(iLine),
                PhaseAngleDeg = iLine < 0 ? 180.0 : 0.0
            };
        }

        /// <summary>由 P/Q 控制意图换算线电流幅值与相位（用于设备边界或母线汇总前的意图转换）。</summary>
        public static AcInternalQuantities FromLineVoltageAndPower(
            double lineVoltageV,
            double activePowerKw,
            double reactivePowerKvar,
            ThreePhaseConnection connection,
            double frequencyHz = 50.0)
        {
            var (lineCurrentA, phaseAngleDeg) = FromPowerToPhasor(
                lineVoltageV, activePowerKw, reactivePowerKvar);

            return new AcInternalQuantities
            {
                Connection = connection,
                LineVoltageV = lineVoltageV,
                LineCurrentA = lineCurrentA,
                PhaseAngleDeg = phaseAngleDeg,
                FrequencyHz = frequencyHz
            };
        }

        public static AcInternalQuantities FromLineVoltageAndCurrent(
            double lineVoltageV,
            double lineCurrentA,
            ThreePhaseConnection connection,
            double frequencyHz = 50.0)
        {
            return new AcInternalQuantities
            {
                Connection = connection,
                LineVoltageV = lineVoltageV,
                LineCurrentA = Math.Abs(lineCurrentA),
                PhaseAngleDeg = lineCurrentA < 0 ? 180.0 : 0.0,
                FrequencyHz = frequencyHz
            };
        }

        public static (double LineCurrentA, double PhaseAngleDeg) FromPowerToPhasor(
            double lineVoltageV,
            double activePowerKw,
            double reactivePowerKvar)
        {
            if (lineVoltageV <= 1e-9)
                return (0, 0);

            double apparentKva = Math.Sqrt(
                activePowerKw * activePowerKw + reactivePowerKvar * reactivePowerKvar);
            if (apparentKva <= 1e-9)
                return (0, 0);

            double lineCurrentA = LineCurrentFromApparent(lineVoltageV, apparentKva);
            double phaseAngleDeg = Math.Atan2(reactivePowerKvar, activePowerKw) * 180.0 / Math.PI;
            return (lineCurrentA, phaseAngleDeg);
        }

        public static double LineCurrentFromApparent(double lineVoltageV, double apparentKva)
        {
            if (lineVoltageV <= 1e-9 || apparentKva <= 0)
                return 0;

            return apparentKva * 1000.0 / (lineVoltageV * ElectricalConventions.Sqrt3);
        }

        public static double ComputeActivePowerKw(double lineVoltageV, double lineCurrentA, double phaseAngleDeg)
        {
            if (lineVoltageV <= 1e-9 || Math.Abs(lineCurrentA) <= 1e-9)
                return 0;

            return lineVoltageV * lineCurrentA * ElectricalConventions.Sqrt3 / 1000.0
                   * Math.Cos(phaseAngleDeg * Math.PI / 180.0);
        }

        public static double ComputeReactivePowerKvar(double lineVoltageV, double lineCurrentA, double phaseAngleDeg)
        {
            if (lineVoltageV <= 1e-9 || Math.Abs(lineCurrentA) <= 1e-9)
                return 0;

            return lineVoltageV * lineCurrentA * ElectricalConventions.Sqrt3 / 1000.0
                   * Math.Sin(phaseAngleDeg * Math.PI / 180.0);
        }

        public static double ComputeApparentPowerKva(double lineVoltageV, double lineCurrentA) =>
            Math.Abs(ComputeActivePowerKw(lineVoltageV, lineCurrentA, 0))
            > 1e-9 || Math.Abs(lineCurrentA) > 1e-9
                ? Math.Abs(lineVoltageV * lineCurrentA * ElectricalConventions.Sqrt3) / 1000.0
                : 0;

        public static double ComputePowerFactor(double lineVoltageV, double lineCurrentA, double phaseAngleDeg)
        {
            double apparentKva = ComputeApparentPowerKva(lineVoltageV, lineCurrentA);
            if (apparentKva <= 1e-9)
                return 1.0;

            return ComputeActivePowerKw(lineVoltageV, lineCurrentA, phaseAngleDeg) / apparentKva;
        }
    }
}
