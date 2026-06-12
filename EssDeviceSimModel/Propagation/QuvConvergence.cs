namespace EssSimulator.EssDeviceSimModel.Propagation
{
    /// <summary>Q-U / 电压传播迭代收敛判定。</summary>
    internal static class QuvConvergence
    {
        public static bool IsLineVoltageConverged(
            double previousLineVoltageV,
            double currentLineVoltageV,
            double nominalLineVoltageV,
            double tolerancePu)
        {
            if (nominalLineVoltageV <= 0 || tolerancePu <= 0)
                return false;

            if (previousLineVoltageV <= 1.0 && currentLineVoltageV <= 1.0)
                return true;

            double deltaPu = Math.Abs(currentLineVoltageV - previousLineVoltageV) / nominalLineVoltageV;
            return deltaPu <= tolerancePu;
        }
    }
}
