using EssSimulator.Configuration;

namespace EssSimulator.EssDeviceSimModel.Battery
{
    /// <summary>从 appsettings BMS 配置构建四层电池堆模型。</summary>
    public static class BmsRackFactory
    {
        public static BatteryRackSimulator CreateRack(BmsDeviceConfig bmsCfg) =>
            new(CreateRackConfiguration(bmsCfg));

        public static RackConfiguration CreateRackConfiguration(BmsDeviceConfig bmsCfg) =>
            new()
            {
                ClusterCount = bmsCfg.ClusterCount,
                ClusterConfig = new ClusterConfiguration
                {
                    PackCount = bmsCfg.PackCount,
                    PackConfig = new PackConfiguration
                    {
                        SeriesCount = bmsCfg.CellSeriesCount,
                        ParallelCount = bmsCfg.CellParallelCount,
                        NominalVoltage = bmsCfg.CellNominalVoltage,
                        NominalCapacity = bmsCfg.CellNominalCapacity,
                        InitialSoc = bmsCfg.CellInitialSoc,
                        InitialSocRandomRange = 0,
                        PackInternalResistance = bmsCfg.PackInternalResistance
                    },
                    ClusterInternalResistance = bmsCfg.ClusterInternalResistance
                },
                RackInternalResistance = bmsCfg.RackInternalResistance,
                PassiveBalance = MapPassiveBalance(bmsCfg.PassiveBalance)
            };

        private static RackPassiveBalanceConfig MapPassiveBalance(BmsPassiveBalanceConfig? src)
        {
            src ??= new BmsPassiveBalanceConfig();
            return new RackPassiveBalanceConfig
            {
                Enabled = src.Enabled,
                StartSocDelta = src.StartSocDelta,
                StopSocDelta = src.StopSocDelta,
                BalanceCRate = src.BalanceCRate,
                IdleOnly = src.IdleOnly,
                IdleCurrentThresholdA = src.IdleCurrentThresholdA,
                BleedAboveMinMargin = src.BleedAboveMinMargin
            };
        }
    }
}
