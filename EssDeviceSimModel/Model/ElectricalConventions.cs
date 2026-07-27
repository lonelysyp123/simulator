namespace EssSimulator.EssDeviceSimModel.Model
{
    /// <summary>
    /// 电气量符号约定（与 GridFeedbackConventions、EmMapper 一致）。
    /// </summary>
    public static class ElectricalConventions
    {
        public const double Sqrt3 = 1.7320508075688772;

        /// <summary>有功：向电网送电（放电）为正。</summary>
        public const int ActivePowerDischargeSign = 1;

        /// <summary>无功：升压支撑（容性）为正；电表功率因数与 Q 同号。</summary>
        public const int ReactivePowerCapacitiveSign = 1;

        /// <summary>DC 电流：放电（电池流出）为正。</summary>
        public const int DcCurrentDischargeSign = 1;
    }
}
