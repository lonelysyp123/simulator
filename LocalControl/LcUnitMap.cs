namespace EssSimulator.LocalControl
{
    /// <summary>
    /// 10MW 单元片段点名：与 <c>unit_10MW/lc.csv</c> 对齐。
    /// 寄存器地址 <c>2600 + 300*(n-1)</c>；点名 <c>unit_param{offset + 600*(n-1)}</c>。
    /// <c>n</c> 是组序号；模块 1/2 对应组内第 1/2 条 PCS 支路。
    /// 组内超过 4 条支路时该片段不拼装（见 model.json <c>maxPcsPerGroup</c>）。
    /// </summary>
    internal static class LcUnitMap
    {
        public const int AddressBase = 2600;
        public const int AddressStride = 300;
        public const int ParamStride = 600;

        public static int Address(int n, int offset) => AddressBase + AddressStride * (n - 1) + offset;

        public static string Param(int n, int offset) => $"unit_param{offset + ParamStride * (n - 1)}";

        public static string BatteryVoltage(int n, int module) => Param(n, module == 0 ? 0 : 6);
        public static string BatteryCurrent(int n, int module) => Param(n, module == 0 ? 1 : 7);
        /// <summary>模块交流母线电压：PCS 交流侧三线电压的等效线电压。</summary>
        public static string BusVoltage(int n, int module) => Param(n, module == 0 ? 2 : 8);
        /// <summary>模块电感电流 R/S/T：PCS 交流侧 A/B/C 相电流幅值。</summary>
        public static string InductorR(int n, int module) => Param(n, module == 0 ? 3 : 9);
        public static string InductorS(int n, int module) => Param(n, module == 0 ? 4 : 10);
        public static string InductorT(int n, int module) => Param(n, module == 0 ? 5 : 11);
        public static string BatteryPower(int n, int module) => Param(n, module == 0 ? 16 : 17);
        public static string WarningWord1(int n, int module) => Param(n, module == 0 ? 231 : 245);
        public static string WarningWord2(int n, int module) => Param(n, module == 0 ? 232 : 246);

        /// <summary>组交流电流 R/S/T：模块1+模块2 电感电流合计。</summary>
        public static string AcCurrentR(int n) => Param(n, 12);
        public static string AcCurrentS(int n) => Param(n, 13);
        public static string AcCurrentT(int n) => Param(n, 14);
        public static string BatteryPowerTotal(int n) => Param(n, 15);
        /// <summary>点表名「电网*」，实为两模块 PCS 交流侧合计：P/Q 相加，线电压取共母线平均。</summary>
        public static string GridActivePower(int n) => Param(n, 18);
        public static string GridReactivePower(int n) => Param(n, 19);
        public static string GridVoltageRs(int n) => Param(n, 20);
        public static string GridVoltageSt(int n) => Param(n, 21);
        public static string GridVoltageTr(int n) => Param(n, 22);
        public static string OverTempNtc(int n) => Param(n, 23);
        public static string AvailableCapacity(int n) => Param(n, 24);
        public static string RatedCapacity(int n) => Param(n, 25);
    }

    /// <summary>组内一条 PCS 支路（10MW 点表「模块1/2」）快照。</summary>
    internal readonly record struct LcUnitModuleSnap(
        object BatteryVoltage,
        object BatteryCurrent,
        object LineVoltageAb,
        object LineVoltageBc,
        object LineVoltageCa,
        object InductorR,
        object InductorS,
        object InductorT,
        object BatteryPower,
        object IgbtMaxTemp,
        object AvailableCapacity,
        object RatedPower,
        object WarningWord1,
        object WarningWord2,
        object ActivePower,
        object ReactivePower);

    /// <summary>把组内前两条 PCS 支路折成 10MW 单元片段寄存器值。</summary>
    internal static class LcUnitTelemetry
    {
        public static List<(string Param, object Value)> Collect(
            int n,
            LcUnitModuleSnap? module1,
            LcUnitModuleSnap? module2)
        {
            var pts = new List<(string, object)>(32);
            WriteModule(pts, n, 0, module1);
            WriteModule(pts, n, 1, module2);

            double p1 = Num(module1?.BatteryPower);
            double p2 = Num(module2?.BatteryPower);
            pts.Add((LcUnitMap.BatteryPowerTotal(n), p1 + p2));
            pts.Add((LcUnitMap.OverTempNtc(n), MaxPresent(module1?.IgbtMaxTemp, module2?.IgbtMaxTemp)));
            pts.Add((LcUnitMap.AvailableCapacity(n), Num(module1?.AvailableCapacity) + Num(module2?.AvailableCapacity)));
            pts.Add((LcUnitMap.RatedCapacity(n), Num(module1?.RatedPower) + Num(module2?.RatedPower)));

            pts.Add((LcUnitMap.AcCurrentR(n), Abs(module1?.InductorR) + Abs(module2?.InductorR)));
            pts.Add((LcUnitMap.AcCurrentS(n), Abs(module1?.InductorS) + Abs(module2?.InductorS)));
            pts.Add((LcUnitMap.AcCurrentT(n), Abs(module1?.InductorT) + Abs(module2?.InductorT)));
            pts.Add((LcUnitMap.GridActivePower(n), Num(module1?.ActivePower) + Num(module2?.ActivePower)));
            pts.Add((LcUnitMap.GridReactivePower(n), Num(module1?.ReactivePower) + Num(module2?.ReactivePower)));
            pts.Add((LcUnitMap.GridVoltageRs(n), MeanPresent(module1?.LineVoltageAb, module2?.LineVoltageAb)));
            pts.Add((LcUnitMap.GridVoltageSt(n), MeanPresent(module1?.LineVoltageBc, module2?.LineVoltageBc)));
            pts.Add((LcUnitMap.GridVoltageTr(n), MeanPresent(module1?.LineVoltageCa, module2?.LineVoltageCa)));
            return pts;
        }

        private static void WriteModule(
            List<(string, object)> pts,
            int n,
            int module,
            LcUnitModuleSnap? snap)
        {
            pts.Add((LcUnitMap.BatteryVoltage(n, module), snap?.BatteryVoltage ?? 0));
            pts.Add((LcUnitMap.BatteryCurrent(n, module), snap?.BatteryCurrent ?? 0));
            pts.Add((LcUnitMap.BusVoltage(n, module), snap == null ? 0 : EquivalentLineVoltage(snap.Value)));
            pts.Add((LcUnitMap.InductorR(n, module), Abs(snap?.InductorR)));
            pts.Add((LcUnitMap.InductorS(n, module), Abs(snap?.InductorS)));
            pts.Add((LcUnitMap.InductorT(n, module), Abs(snap?.InductorT)));
            pts.Add((LcUnitMap.BatteryPower(n, module), snap?.BatteryPower ?? 0));
            pts.Add((LcUnitMap.WarningWord1(n, module), snap?.WarningWord1 ?? 0));
            pts.Add((LcUnitMap.WarningWord2(n, module), snap?.WarningWord2 ?? 0));
        }

        /// <summary>三线电压等效线电压 √((Uab²+Ubc²+Uca²)/3)；平衡时等于任一线电压。</summary>
        internal static double EquivalentLineVoltage(LcUnitModuleSnap snap)
        {
            double ab = Num(snap.LineVoltageAb);
            double bc = Num(snap.LineVoltageBc);
            double ca = Num(snap.LineVoltageCa);
            return Math.Sqrt((ab * ab + bc * bc + ca * ca) / 3.0);
        }

        private static double Abs(object? value) => Math.Abs(Num(value));

        private static double Num(object? value) =>
            value == null ? 0 : ModbusValueConverter.ToDouble(value);

        private static double MaxPresent(object? a, object? b)
        {
            bool hasA = a != null;
            bool hasB = b != null;
            if (!hasA && !hasB)
                return 0;
            if (!hasA)
                return Num(b);
            if (!hasB)
                return Num(a);
            return Math.Max(Num(a), Num(b));
        }

        /// <summary>两模块共母线：线电压取在场模块的算术平均，不把两台 690V 加总。</summary>
        private static double MeanPresent(object? a, object? b)
        {
            bool hasA = a != null;
            bool hasB = b != null;
            if (!hasA && !hasB)
                return 0;
            if (!hasA)
                return Num(b);
            if (!hasB)
                return Num(a);
            return (Num(a) + Num(b)) / 2.0;
        }
    }
}
