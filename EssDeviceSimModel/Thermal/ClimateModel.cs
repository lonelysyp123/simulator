using System;

namespace EssSimulator.EssDeviceSimModel.Thermal
{
    /// <summary>
    /// 室外气温：按时刻的正弦日变化，或固定温度。
    /// </summary>
    public sealed class ClimateModel
    {
        private readonly Configuration.ClimateConfig _cfg;

        public ClimateModel(Configuration.ClimateConfig cfg)
        {
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
        }

        /// <summary>求取 <paramref name="simTime"/> 对应的室外气温（°C）。</summary>
        public double EvaluateOutdoorCelsius(DateTime simTime)
        {
            if (_cfg.FixedCelsius.HasValue)
                return _cfg.FixedCelsius.Value;

            double min = _cfg.MinCelsius;
            double max = _cfg.MaxCelsius;
            if (max < min)
                (min, max) = (max, min);

            double mid = 0.5 * (min + max);
            double amp = 0.5 * (max - min);
            // 峰值在 PeakHour：相位使 cos 在 PeakHour 为 1
            double hour = simTime.TimeOfDay.TotalHours;
            double phase = 2.0 * Math.PI * (hour - _cfg.PeakHour) / 24.0;
            return mid + amp * Math.Cos(phase);
        }
    }
}
