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

        /// <summary>
        /// 与 emu 点表 OperationStatus 对齐：1停机 2待机 4充电运行 5放电运行 6未知状态（正放负充）。
        /// </summary>
        public static int ToOperationStatusCode(OperationMode mode, bool externalRunCommand,
            double activePowerKw, bool blackStartEnabled, ushort faultType, bool hasAlarm)
        {
            if (faultType != 0 || hasAlarm)
                return 6;
            if (mode == OperationMode.Off || !externalRunCommand)
                return 1;
            if (blackStartEnabled || mode == OperationMode.Standby)
                return 2;
            if (activePowerKw > ActivePowerThresholdKw)
                return 5;
            if (activePowerKw < -ActivePowerThresholdKw)
                return 4;
            return 2;
        }

        public static string GetOperationStatusLabel(int code) => code switch
        {
            1 => "停机",
            2 => "待机",
            4 => "充电运行",
            5 => "放电运行",
            6 => "未知状态",
            _ => $"未知({code})"
        };
    }
}
