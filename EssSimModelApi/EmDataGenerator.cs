using System;
using IEC61850_simulatorServer2.EssSimModelApi.ElectricMeter;

namespace IEC61850_simulatorServer2.EssSimModelApi
{
    /// <summary>
    /// 生成电表接口层示例数据，便于初始化与离线演示。
    /// </summary>
    public static class EmDataGenerator
    {
        private static readonly Random _rand = new Random();

        public static EmData GenerateSampleData()
        {
            var baseVoltage = 400f;
            var baseCurrent = 50f;

            return new EmData
            {
                PhaseAVoltage = Jitter(baseVoltage),
                PhaseBVoltage = Jitter(baseVoltage),
                PhaseCVoltage = Jitter(baseVoltage),
                LineVoltageAB = Jitter(baseVoltage * 1.1f),
                LineVoltageBC = Jitter(baseVoltage * 1.1f),
                LineVoltageCA = Jitter(baseVoltage * 1.1f),
                PhaseACurrent = Jitter(baseCurrent),
                PhaseBCurrent = Jitter(baseCurrent),
                PhaseCCurrent = Jitter(baseCurrent),
                PhaseAActivePower = Jitter(60f),
                PhaseBActivePower = Jitter(60f),
                PhaseCActivePower = Jitter(60f),
                TotalActivePower = Jitter(180f),
                PhaseAReactivePower = Jitter(10f),
                PhaseBReactivePower = Jitter(10f),
                PhaseCReactivePower = Jitter(10f),
                TotalReactivePower = Jitter(30f),
                TotalApparentPower = Jitter(190f),
                PowerFactor = (float)Math.Round(0.98 + 0.01 * _rand.NextDouble(), 3),
                Frequency = (float)Math.Round(50 + 0.1 * (_rand.NextDouble() - 0.5), 3),
                ForwardActiveEnergy = 12345f,
                ReverseActiveEnergy = 12f,
                Timestamp = DateTime.Now
            };
        }

        private static float Jitter(float value)
        {
            var delta = (float)(_rand.NextDouble() - 0.5) * 0.05f * value; // +/-5%
            return value + delta;
        }
    }
}
