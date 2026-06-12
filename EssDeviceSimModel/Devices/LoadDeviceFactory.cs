using EssSimulator.Configuration;
using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Devices
{
    public static class LoadDeviceFactory
    {
        public static LoadDevice Create(string deviceId, LoadConfig loadCfg) =>
            new(
                deviceId,
                loadCfg.ActivePowerPlan,
                loadCfg.ReactivePowerPlan,
                new[]
                {
                    new LoadWindow
                    {
                        Start = TimeSpan.Zero,
                        ActivePowerPlan = loadCfg.ActivePowerPlan,
                        ReactivePowerPlan = loadCfg.ReactivePowerPlan
                    }
                });
    }
}
