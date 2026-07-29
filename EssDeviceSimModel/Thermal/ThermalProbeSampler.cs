using System;

namespace EssSimulator.EssDeviceSimModel.Thermal
{
    /// <summary>
    /// 柜内温度探针挂点（相对热网络节点的几何/混合读数）。
    /// </summary>
    public enum ThermalProbeKind
    {
        Outdoor,
        Shell,
        Air,
        /// <summary>柜内上部：偏电池热源。</summary>
        AirTop,
        /// <summary>柜内下部：偏外壳/冷壁。</summary>
        AirBottom,
        Battery,
        /// <summary>空调盘管附近：空气与外壳混合。</summary>
        Coil,
        /// <summary>冷凝侧：偏室外。</summary>
        Condenser
    }

    /// <summary>从柜体热区读取探针温度（含偏置）。</summary>
    public static class ThermalProbeSampler
    {
        public static double ReadCelsius(BmsCabinetThermalZone zone, ThermalProbeKind kind, double biasCelsius = 0)
        {
            ArgumentNullException.ThrowIfNull(zone);

            double air = zone.CabinetAirCelsius;
            double shell = zone.ShellCelsius;
            double battery = zone.BatteryNodeCelsius;
            double outdoor = zone.OutdoorCelsius;

            double raw = kind switch
            {
                ThermalProbeKind.Outdoor => outdoor,
                ThermalProbeKind.Shell => shell,
                ThermalProbeKind.Air => air,
                ThermalProbeKind.AirTop => air + 0.45 * (battery - air),
                ThermalProbeKind.AirBottom => air + 0.35 * (shell - air),
                ThermalProbeKind.Battery => battery,
                ThermalProbeKind.Coil => 0.65 * air + 0.35 * shell,
                ThermalProbeKind.Condenser => 0.75 * outdoor + 0.25 * shell,
                _ => air
            };

            return raw + biasCelsius;
        }
    }
}
