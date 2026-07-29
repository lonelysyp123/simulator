using System;

namespace EssSimulator.EssSimModelApi.BatteryManagementSystem
{
    /// <summary>
    /// 最大允许充/放电流（功率）多约束降额：SOC × 温度 × 电压逼近，再与柜体热降额取 min。
    /// 默认值为大储 LFP ~0.5C 联调建议值（非某厂家金标准），可整体替换 <see cref="Active"/>。
    /// </summary>
    public sealed class BmsCurrentLimitDeratingConfig
    {
        public bool Enabled { get; set; } = true;

        // ── 温度：分段表 (T°C → 因子)，线性插值；充电看最冷/最热电芯取更严 ──

        /// <summary>充电温度降额表。建议：&lt;0 禁充；0→0.1；5→0.2；15~35 满额；45→0.5；50→0。</summary>
        public (float Celsius, float Factor)[] ChargeTempCurve { get; set; } =
        {
            (-10f, 0.00f),
            (0f, 0.10f),
            (5f, 0.20f),
            (15f, 1.00f),
            (35f, 1.00f),
            (40f, 0.80f),
            (45f, 0.50f),
            (50f, 0.00f),
        };

        /// <summary>放电温度降额表。建议：-20→0.3；0→0.7；10~35 满额；45→0.7；55→0.3；60→0。</summary>
        public (float Celsius, float Factor)[] DischargeTempCurve { get; set; } =
        {
            (-20f, 0.30f),
            (0f, 0.70f),
            (10f, 1.00f),
            (35f, 1.00f),
            (45f, 0.70f),
            (55f, 0.30f),
            (60f, 0.00f),
        };

        // ── 电压逼近：foxBMS 风格线性 start→end；相对保护阈值留裕度 ──

        /// <summary>充电：最高单体电压达到此值开始降额（建议 3.40 V，对齐一级过压保护附近）。</summary>
        public float ChargeVoltageStartV { get; set; } = 3.40f;

        /// <summary>充电：最高单体电压达到此值允许充电电流=0（建议 3.55 V，早于二级告警 3.60 V）。</summary>
        public float ChargeVoltageEndV { get; set; } = 3.55f;

        /// <summary>放电：最低单体电压降到此值开始降额（建议 3.10 V，对齐一级欠压保护）。</summary>
        public float DischargeVoltageStartV { get; set; } = 3.10f;

        /// <summary>放电：最低单体电压降到此值允许放电电流=0（建议 2.80 V，早于二级告警 2.90 V）。</summary>
        public float DischargeVoltageEndV { get; set; } = 2.80f;

        // ── SOC 分段（保持原仿真曲线，便于联调禁充/禁放）──

        /// <summary>充电 SOC：&lt;0.80→1；0.85→0.5；≥0.90→0。</summary>
        public float ChargeSocFullBelow { get; set; } = 0.80f;
        public float ChargeSocHalfAt { get; set; } = 0.85f;
        public float ChargeSocZeroAt { get; set; } = 0.90f;

        /// <summary>放电 SOC：&gt;0.20→1；0.15→0.5；≤0.10→0。</summary>
        public float DischargeSocFullAbove { get; set; } = 0.20f;
        public float DischargeSocHalfAt { get; set; } = 0.15f;
        public float DischargeSocZeroAt { get; set; } = 0.10f;
    }

    /// <summary>堆/簇最大允许功率共用的降额计算。</summary>
    public static class BmsCurrentLimitDerating
    {
        /// <summary>运行时生效的配置；默认即建议值表。</summary>
        public static BmsCurrentLimitDeratingConfig Active { get; set; } = new();

        public static float ChargeLimitFactor(
            float soc,
            float? minCellTempC,
            float? maxCellTempC,
            float? maxCellVoltageV,
            float thermalFactor = 1f)
        {
            var cfg = Active;
            if (cfg == null || !cfg.Enabled)
                return Math.Clamp(thermalFactor, 0f, 1f);

            float socF = ChargeSocFactor(soc, cfg);
            float tempF = ChargeTempFactor(minCellTempC, maxCellTempC, cfg);
            float voltF = ChargeVoltageFactor(maxCellVoltageV, cfg);
            float thermF = Math.Clamp(thermalFactor, 0f, 1f);
            return Min4(socF, tempF, voltF, thermF);
        }

        public static float DischargeLimitFactor(
            float soc,
            float? minCellTempC,
            float? maxCellTempC,
            float? minCellVoltageV,
            float thermalFactor = 1f)
        {
            var cfg = Active;
            if (cfg == null || !cfg.Enabled)
                return Math.Clamp(thermalFactor, 0f, 1f);

            float socF = DischargeSocFactor(soc, cfg);
            float tempF = DischargeTempFactor(minCellTempC, maxCellTempC, cfg);
            float voltF = DischargeVoltageFactor(minCellVoltageV, cfg);
            float thermF = Math.Clamp(thermalFactor, 0f, 1f);
            return Min4(socF, tempF, voltF, thermF);
        }

        public static float ChargeSocFactor(float soc, BmsCurrentLimitDeratingConfig? cfg = null)
        {
            cfg ??= Active;
            float full = cfg.ChargeSocFullBelow;
            float half = cfg.ChargeSocHalfAt;
            float zero = cfg.ChargeSocZeroAt;
            if (soc < full) return 1.0f;
            if (soc < half) return 1.0f - (soc - full) / (half - full) * 0.5f;
            if (soc < zero) return 0.5f - (soc - half) / (zero - half) * 0.5f;
            return 0.0f;
        }

        public static float DischargeSocFactor(float soc, BmsCurrentLimitDeratingConfig? cfg = null)
        {
            cfg ??= Active;
            float full = cfg.DischargeSocFullAbove;
            float half = cfg.DischargeSocHalfAt;
            float zero = cfg.DischargeSocZeroAt;
            if (soc > full) return 1.0f;
            if (soc > half) return 0.5f + (soc - half) / (full - half) * 0.5f;
            if (soc > zero) return 0.0f + (soc - zero) / (half - zero) * 0.5f;
            return 0.0f;
        }

        public static float ChargeTempFactor(
            float? minCellTempC,
            float? maxCellTempC,
            BmsCurrentLimitDeratingConfig? cfg = null)
        {
            cfg ??= Active;
            if (!minCellTempC.HasValue && !maxCellTempC.HasValue)
                return 1f;

            // 低温看最冷芯，高温看最热芯，取更严
            float cold = minCellTempC ?? maxCellTempC!.Value;
            float hot = maxCellTempC ?? minCellTempC!.Value;
            return Math.Min(
                Interpolate(cold, cfg.ChargeTempCurve),
                Interpolate(hot, cfg.ChargeTempCurve));
        }

        public static float DischargeTempFactor(
            float? minCellTempC,
            float? maxCellTempC,
            BmsCurrentLimitDeratingConfig? cfg = null)
        {
            cfg ??= Active;
            if (!minCellTempC.HasValue && !maxCellTempC.HasValue)
                return 1f;

            float cold = minCellTempC ?? maxCellTempC!.Value;
            float hot = maxCellTempC ?? minCellTempC!.Value;
            return Math.Min(
                Interpolate(cold, cfg.DischargeTempCurve),
                Interpolate(hot, cfg.DischargeTempCurve));
        }

        public static float ChargeVoltageFactor(float? maxCellVoltageV, BmsCurrentLimitDeratingConfig? cfg = null)
        {
            cfg ??= Active;
            if (!maxCellVoltageV.HasValue)
                return 1f;

            float start = cfg.ChargeVoltageStartV;
            float end = Math.Max(start + 1e-4f, cfg.ChargeVoltageEndV);
            float v = maxCellVoltageV.Value;
            if (v <= start) return 1f;
            if (v >= end) return 0f;
            return 1f - (v - start) / (end - start);
        }

        public static float DischargeVoltageFactor(float? minCellVoltageV, BmsCurrentLimitDeratingConfig? cfg = null)
        {
            cfg ??= Active;
            if (!minCellVoltageV.HasValue)
                return 1f;

            float start = cfg.DischargeVoltageStartV;
            float end = Math.Min(start - 1e-4f, cfg.DischargeVoltageEndV);
            float v = minCellVoltageV.Value;
            if (v >= start) return 1f;
            if (v <= end) return 0f;
            return (v - end) / (start - end);
        }

        public static float Interpolate(float x, (float Celsius, float Factor)[] curve)
        {
            if (curve == null || curve.Length == 0)
                return 1f;
            if (x <= curve[0].Celsius)
                return Math.Clamp(curve[0].Factor, 0f, 1f);
            if (x >= curve[^1].Celsius)
                return Math.Clamp(curve[^1].Factor, 0f, 1f);

            for (int i = 1; i < curve.Length; i++)
            {
                if (x > curve[i].Celsius)
                    continue;
                float x0 = curve[i - 1].Celsius;
                float y0 = curve[i - 1].Factor;
                float x1 = curve[i].Celsius;
                float y1 = curve[i].Factor;
                float t = (x - x0) / (x1 - x0);
                return Math.Clamp(y0 + t * (y1 - y0), 0f, 1f);
            }

            return Math.Clamp(curve[^1].Factor, 0f, 1f);
        }

        private static float Min4(float a, float b, float c, float d) =>
            Math.Min(Math.Min(a, b), Math.Min(c, d));
    }
}
