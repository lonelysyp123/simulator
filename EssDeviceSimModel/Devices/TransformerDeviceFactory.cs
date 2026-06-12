using EssSimulator.Configuration;
using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Devices
{
    public static class TransformerDeviceFactory
    {
        public static TransformerDeviceConfig CreateConfig(TransformerConfig cfg) =>
            CreateConfig(
                cfg.RatedPower,
                cfg.PrimaryVoltage,
                cfg.SecondaryVoltage,
                cfg.NoLoadLoss,
                cfg.LoadLoss,
                cfg.ImpedancePercent,
                cfg.ReactiveVoltageInfluenceCoefficient,
                cfg.NoLoadCurrentPercent,
                cfg.MagnetizingInrushEnabled,
                cfg.MagnetizingInrushDvDtThresholdPuPerSec,
                cfg.MagnetizingInrushPeakExtraMultipleOfRatedPrimary,
                cfg.MagnetizingInrushDecayTimeConstantSec,
                cfg.MagnetizingInrushMaxExtraMultipleOfRatedPrimary);

        public static TransformerDeviceConfig CreateConfig(UnitTransformerConfig cfg) =>
            CreateConfig(
                cfg.RatedPower,
                cfg.PrimaryVoltage,
                cfg.SecondaryVoltage,
                cfg.NoLoadLoss,
                cfg.LoadLoss,
                cfg.ImpedancePercent,
                cfg.ReactiveVoltageInfluenceCoefficient,
                cfg.NoLoadCurrentPercent,
                cfg.MagnetizingInrushEnabled,
                cfg.MagnetizingInrushDvDtThresholdPuPerSec,
                cfg.MagnetizingInrushPeakExtraMultipleOfRatedPrimary,
                cfg.MagnetizingInrushDecayTimeConstantSec,
                cfg.MagnetizingInrushMaxExtraMultipleOfRatedPrimary);

        private static TransformerDeviceConfig CreateConfig(
            double ratedPowerKva,
            double primaryVoltage,
            double secondaryVoltage,
            double noLoadLossW,
            double loadLossW,
            double impedancePercent,
            double reactiveVoltageInfluenceCoefficient,
            double noLoadCurrentPercent,
            bool magnetizingInrushEnabled,
            double magnetizingInrushDvDtThresholdPuPerSec,
            double magnetizingInrushPeakExtraMultipleOfRatedPrimary,
            double magnetizingInrushDecayTimeConstantSec,
            double magnetizingInrushMaxExtraMultipleOfRatedPrimary) =>
            new()
            {
                RatedPowerKva = ratedPowerKva,
                PrimaryNominalLineVoltageV = primaryVoltage,
                SecondaryNominalLineVoltageV = secondaryVoltage,
                NoLoadLossKw = noLoadLossW / 1000.0,
                LoadLossKw = loadLossW / 1000.0,
                ImpedancePercent = impedancePercent,
                ReactiveVoltageInfluenceCoefficient = reactiveVoltageInfluenceCoefficient,
                NoLoadCurrentPercent = noLoadCurrentPercent,
                MagnetizingInrushEnabled = magnetizingInrushEnabled,
                MagnetizingInrushDvDtThresholdPuPerSec = magnetizingInrushDvDtThresholdPuPerSec,
                MagnetizingInrushPeakExtraMultipleOfRatedPrimary = magnetizingInrushPeakExtraMultipleOfRatedPrimary,
                MagnetizingInrushDecayTimeConstantSec = magnetizingInrushDecayTimeConstantSec,
                MagnetizingInrushMaxExtraMultipleOfRatedPrimary = magnetizingInrushMaxExtraMultipleOfRatedPrimary
            };

        public static TransformerDevice Create(string deviceId, TransformerDeviceConfig config) =>
            new(deviceId, config);
    }
}
