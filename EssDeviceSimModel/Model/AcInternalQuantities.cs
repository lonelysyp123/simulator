namespace EssSimulator.EssDeviceSimModel.Model
{
    /// <summary>
    /// AC 内部求解量：平衡三相下线电压 + 线电流 + 功率。
    /// </summary>
    public sealed class AcInternalQuantities
    {
        public ThreePhaseConnection Connection { get; init; } = ThreePhaseConnection.Star;
        public double LineVoltageV { get; init; }
        public double LineCurrentA { get; init; }
        public double FrequencyHz { get; init; } = 50.0;
        public double ActivePowerKw { get; init; }
        public double ReactivePowerKvar { get; init; }

        public double ApparentPowerKva =>
            Math.Sqrt(ActivePowerKw * ActivePowerKw + ReactivePowerKvar * ReactivePowerKvar);

        public bool IsEnergized(double voltageThresholdV = 1.0) => LineVoltageV > voltageThresholdV;
    }
}
