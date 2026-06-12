using EssSimulator.Configuration;
using ModelPcsDeviceConfig = EssSimulator.EssDeviceSimModel.Model.PcsDeviceConfig;

namespace EssSimulator.EssDeviceSimModel.Devices
{
    public static class PcsDeviceFactory
    {
        public static ModelPcsDeviceConfig CreateConfig(
            PcsPhysicalConfig pcsCfg,
            PcsRampConfig rampCfg,
            double speedup)
        {
            return new ModelPcsDeviceConfig
            {
                RatedPowerKw = pcsCfg.RatedPower,
                MaxPowerKw = pcsCfg.MaxPower,
                Efficiency = pcsCfg.Efficiency,
                DcVoltageRangeMinV = pcsCfg.DcVoltageRangeMin,
                DcVoltageRangeMaxV = pcsCfg.DcVoltageRangeMax,
                AcNominalLineVoltageV = pcsCfg.AcVoltageNominal,
                FrequencyHz = pcsCfg.FrequencyNominal,
                MaxCurrentA = pcsCfg.MaxCurrent,
                GridLossCoefficient = pcsCfg.GridLossCoefficient,
                Speedup = speedup,
                RampSlope = rampCfg.Slope,
                RampIntervalMs = rampCfg.IntervalMs,
                RampDelayMs = rampCfg.DelayMs,
                IslandVoltageRampDurationMs = pcsCfg.IslandVoltageRampDurationMs,
                BlackStartActivePowerGainKwPerVolt = pcsCfg.BlackStartActivePowerGainKwPerVolt,
                BlackStartMaxActivePowerKw = pcsCfg.BlackStartMaxActivePowerKw,
                BlackStartMagnetizingPowerFraction = pcsCfg.BlackStartMagnetizingPowerFraction,
                BlackStartBusEnergizedFraction = pcsCfg.BlackStartBusEnergizedFraction,
                BlackStartPrechargeDelayMs = pcsCfg.BlackStartPrechargeDelayMs,
                BlackStartVoltageRampVs = pcsCfg.BlackStartVoltageRampVs,
                BlackStartFrequencyStartHz = pcsCfg.BlackStartFrequencyStartHz,
                BlackStartFrequencyRampHzPerSec = pcsCfg.BlackStartFrequencyRampHzPerSec,
                BlackStartReactiveVoltageGainKvarPerV = pcsCfg.BlackStartReactiveVoltageGainKvarPerV,
                BlackStartCurrentLimitFraction = pcsCfg.BlackStartCurrentLimitFraction
            };
        }

        public static PcsDevice Create(string deviceId, ModelPcsDeviceConfig config) =>
            new(deviceId, config);
    }
}
