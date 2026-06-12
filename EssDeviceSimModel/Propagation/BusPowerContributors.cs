using EssSimulator.EssDeviceSimModel.Devices;
using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Propagation
{
    internal sealed class LoadBusContributor : IBusPowerContributor
    {
        private readonly LoadDevice _load;

        public LoadBusContributor(LoadDevice load) => _load = load;

        public string ContributorId => _load.DeviceId;

        public BusPowerContribution GetBusPowerContribution(DeviceStepContext context)
        {
            _load.RefreshSchedule(context.SimulationTime);
            return new BusPowerContribution(_load.ActivePower, _load.ReactivePower);
        }
    }

    internal sealed class PcsBusContributor : IBusPowerContributor
    {
        private readonly PcsDevice _pcs;

        public PcsBusContributor(PcsDevice pcs) => _pcs = pcs;

        public string ContributorId => _pcs.DeviceId;

        public BusPowerContribution GetBusPowerContribution(DeviceStepContext context)
        {
            var st = _pcs.GetCurrentState();
            return new BusPowerContribution(_pcs.GetGridSideActivePower(), st.ReactivePower);
        }
    }
}
