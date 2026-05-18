namespace EssSimulator.EssDeviceSimModel
{
    /// <summary>PCS 仿真运行相位的界面/点表展示文案（停机、待机、充电、放电等）。</summary>
    public static class PcsDisplayLabels
    {
        public const double ActivePowerThresholdKw = 10.0;

        public static string GetRunPhaseLabel(OperationMode mode, double activePowerKw, bool blackStartEnabled, ushort faultType)
        {
            if (mode == OperationMode.Off)
                return "停机";
            if (faultType != 0)
                return "故障";
            if (blackStartEnabled)
                return "黑启动";
            if (mode == OperationMode.Standby)
                return "待机";
            if (mode == OperationMode.Normal)
            {
                if (activePowerKw > ActivePowerThresholdKw)
                    return "放电";
                if (activePowerKw < -ActivePowerThresholdKw)
                    return "充电";
                return "待机";
            }

            return mode.ToString();
        }

        /// <summary>与 emu 点表 OperationStatus 对齐：0停机 1待机 2故障 3充电 4放电。</summary>
        public static int ToOperationStatusCode(OperationMode mode, bool externalRunCommand,
            double activePowerKw, bool blackStartEnabled, ushort faultType, bool hasAlarm)
        {
            if (hasAlarm || faultType != 0)
                return 2;
            if (mode == OperationMode.Off || !externalRunCommand)
                return 0;
            if (blackStartEnabled)
                return 1;
            if (activePowerKw > ActivePowerThresholdKw)
                return 4;
            if (activePowerKw < -ActivePowerThresholdKw)
                return 3;
            return 1;
        }
    }
}
