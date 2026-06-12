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
                LineCurrentA = iLine
            };
        }

        public static AcInternalQuantities FromLineVoltageAndPower(
            double lineVoltageV,
            double activePowerKw,
            double reactivePowerKvar,
            ThreePhaseConnection connection,
            double frequencyHz = 50.0)
        {
            double apparentKva = Math.Sqrt(activePowerKw * activePowerKw + reactivePowerKvar * reactivePowerKvar);
            double iLine = LineCurrentFromApparent(lineVoltageV, apparentKva);

            if (Math.Abs(activePowerKw) > 1e-9 && iLine > 0)
            {
                double sign = Math.Sign(activePowerKw);
                iLine *= sign;
            }

            return new AcInternalQuantities
            {
                Connection = connection,
                LineVoltageV = lineVoltageV,
                LineCurrentA = iLine,
                ActivePowerKw = activePowerKw,
                ReactivePowerKvar = reactivePowerKvar,
                FrequencyHz = frequencyHz
            };
        }

        public static AcInternalQuantities FromLineVoltageAndCurrent(
            double lineVoltageV,
            double lineCurrentA,
            ThreePhaseConnection connection,
            double frequencyHz = 50.0)
        {
            double apparentKva = Math.Abs(lineVoltageV) > 1e-9
                ? Math.Abs(lineVoltageV * lineCurrentA * ElectricalConventions.Sqrt3) / 1000.0
                : 0;

            return new AcInternalQuantities
            {
                Connection = connection,
                LineVoltageV = lineVoltageV,
                LineCurrentA = lineCurrentA,
                ActivePowerKw = apparentKva,
                ReactivePowerKvar = 0,
                FrequencyHz = frequencyHz
            };
        }

        public static double LineCurrentFromApparent(double lineVoltageV, double apparentKva)
        {
            if (lineVoltageV <= 1e-9 || apparentKva <= 0)
                return 0;

            return apparentKva * 1000.0 / (lineVoltageV * ElectricalConventions.Sqrt3);
        }
    }
}
