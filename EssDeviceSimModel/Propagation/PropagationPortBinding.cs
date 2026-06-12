using EssSimulator.EssDeviceSimModel.Devices;
using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Propagation
{
    internal static class PropagationPortBinding
    {
        public static void SetAcVoltageInput(
            ElectricalPort port,
            double lineVoltageV,
            ThreePhaseConnection connection,
            double frequencyHz = 50)
        {
            var cur = AcPortHelper.ReadAcInput(port);
            port.Input = ElectricalPortSnapshot.FromAc(new AcInternalQuantities
            {
                Connection = connection,
                LineVoltageV = lineVoltageV,
                LineCurrentA = cur.LineCurrentA,
                PhaseAngleDeg = cur.PhaseAngleDeg,
                FrequencyHz = lineVoltageV > 1.0 ? frequencyHz : 0
            });
        }

        /// <summary>写入电流意图；若意图含线电压则一并更新（供 Coupler 下游 P/Q 推导）。</summary>
        public static void SetAcCurrentInput(ElectricalPort port, AcInternalQuantities currentIntent)
        {
            var cur = AcPortHelper.ReadAcInput(port);
            double v = currentIntent.LineVoltageV > 1.0 ? currentIntent.LineVoltageV : cur.LineVoltageV;
            double f = currentIntent.FrequencyHz > 1.0 ? currentIntent.FrequencyHz : cur.FrequencyHz;
            port.Input = ElectricalPortSnapshot.FromAc(new AcInternalQuantities
            {
                Connection = cur.Connection,
                LineVoltageV = v,
                LineCurrentA = currentIntent.LineCurrentA,
                PhaseAngleDeg = currentIntent.PhaseAngleDeg,
                FrequencyHz = f
            });
        }

        /// <summary>一次性写入 V/I/φ（传播链 Coupler 推荐用法）。</summary>
        public static void SetAcQuantitiesInput(ElectricalPort port, AcInternalQuantities quantities)
        {
            var cur = AcPortHelper.ReadAcInput(port);
            port.Input = ElectricalPortSnapshot.FromAc(new AcInternalQuantities
            {
                Connection = quantities.Connection != default ? quantities.Connection : cur.Connection,
                LineVoltageV = quantities.LineVoltageV,
                LineCurrentA = quantities.LineCurrentA,
                PhaseAngleDeg = quantities.PhaseAngleDeg,
                FrequencyHz = quantities.FrequencyHz > 1.0 ? quantities.FrequencyHz : cur.FrequencyHz
            });
        }
    }
}
