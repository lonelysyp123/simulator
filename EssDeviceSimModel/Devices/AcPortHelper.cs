using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Devices
{
    internal static class AcPortHelper
    {
        public static AcInternalQuantities ReadAcInput(ElectricalPort port)
        {
            if (port.Input.Ac == null)
                return new AcInternalQuantities();

            return port.Input.Ac.Internal;
        }

        public static void WriteAcOutput(ElectricalPort port, AcInternalQuantities internalQty)
        {
            port.Output = ElectricalPortSnapshot.FromAc(internalQty);
        }

        public static void WriteDcOutput(ElectricalPort port, DcSnapshot dc)
        {
            port.Output = ElectricalPortSnapshot.FromDc(dc);
        }

        public static DcSnapshot ReadDcInput(ElectricalPort port) =>
            port.Input.Dc ?? new DcSnapshot();
    }
}
