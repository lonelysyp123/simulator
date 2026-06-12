namespace EssSimulator.EssDeviceSimModel.Model
{
    /// <summary>
    /// AC 对外终端量：线电压 + 相电压 + 相电流（平衡三相）。
    /// </summary>
    public sealed class AcTerminalQuantities
    {
        public ThreePhaseConnection Connection { get; init; } = ThreePhaseConnection.Star;

        public double Vab { get; init; }
        public double Vbc { get; init; }
        public double Vca { get; init; }

        public double Van { get; init; }
        public double Vbn { get; init; }
        public double Vcn { get; init; }

        public double Ia { get; init; }
        public double Ib { get; init; }
        public double Ic { get; init; }

        public double LineVoltageV => Vab;
        public double LineCurrentA => Connection == ThreePhaseConnection.Star
            ? Ia
            : Ia * ElectricalConventions.Sqrt3;
    }
}
