using System;
using EssSimulator.EssDeviceSimModel.Devices;
using EssSimulator.EssDeviceSimModel.Interface;
using EssSimulator.EssDeviceSimModel.Thermal;

namespace EssSimulator.EssDeviceSimModel.Plant
{
    /// <summary>
    /// PCS↔BMS 直流耦合边：环境温、V/I 交换、损耗登记，以及高温降额反馈。
    /// </summary>
    public sealed class PcsBmsDcCouplingLink
    {
        public PcsBmsDcCouplingLink(PcsDevice pcs, BmsRackDevice bms, int thermalCabinetIndex)
        {
            Pcs = pcs ?? throw new ArgumentNullException(nameof(pcs));
            Bms = bms ?? throw new ArgumentNullException(nameof(bms));
            ThermalCabinetIndex = thermalCabinetIndex;
        }

        public PcsDevice Pcs { get; }
        public BmsRackDevice Bms { get; }
        public int ThermalCabinetIndex { get; }

        public void Step(
            PlantThermalSystem thermal,
            DateTime simTime,
            TimeSpan elapsed,
            TimeSpan integrationElapsed)
        {
            ArgumentNullException.ThrowIfNull(thermal);

            double cabinetAir = thermal.GetBmsAmbientCelsius(ThermalCabinetIndex);
            double pcsAmbient = thermal.Enabled ? cabinetAir : thermal.OutdoorCelsius;

            // 降额基于上一热步后的电池节点/柜温（本步物理更新前）
            double senseTemp = cabinetAir;
            if (ThermalCabinetIndex >= 0 && ThermalCabinetIndex < thermal.Cabinets.Count)
                senseTemp = Math.Max(
                    thermal.Cabinets[ThermalCabinetIndex].BatteryNodeCelsius,
                    thermal.Cabinets[ThermalCabinetIndex].CabinetAirCelsius);

            double derate = TemperatureDerating.ComputePowerFactor(senseTemp, thermal.Feedback);
            Pcs.ApplyThermalPowerDerating(derate);
            Bms.ApplyThermalPowerDerating(derate);
            if (ThermalCabinetIndex >= 0 && ThermalCabinetIndex < thermal.Cabinets.Count)
                thermal.Cabinets[ThermalCabinetIndex].LastPowerDeratingFactor = derate;

            ((ITemperatureAware)Pcs).ApplyAmbientTemperature(pcsAmbient);
            ((ITemperatureAware)Bms).ApplyAmbientTemperature(cabinetAir);

            if (Bms.IsLinked)
            {
                var rackState = Bms.Rack.GetRackState();
                if (rackState == null)
                    return;

                Pcs.Update(rackState.TotalVoltage, rackState.IsFault, simTime, elapsed, integrationElapsed);
                double rackCurrent = -Pcs.GetCurrentState().DcCurrent;
                Bms.UpdatePhysics(rackCurrent, cabinetAir, simTime, integrationElapsed);
            }
            else
            {
                Pcs.Update(0, 0, simTime, elapsed, integrationElapsed);
                Bms.UpdatePhysics(0, cabinetAir, simTime, integrationElapsed);
            }

            thermal.RecordCabinetHeatWatts(
                ThermalCabinetIndex,
                ((IElectricalLossSource)Bms).GetElectricalLossWatts());
        }
    }
}
