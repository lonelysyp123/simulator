using EssSimulator.Protocol.Modbus;
using NModbus;
using NModbus.Data;
using NModbus.Device;
using NModbus.IO;

namespace EssSimulator
{
    /// <summary>
    /// Modbus Slave TCP
    /// </summary>
    public class ModbusTCPSlave : ModbusSlave, IModbusSlave
    {
        private readonly int rackCount;

        public ModbusTCPSlave(DeviceInfoDto deviceInfoDto, List<MapEntry[]> pointMaps, TCPCommunicator tcpCommunicator, int rackCount = 0) : base(deviceInfoDto, pointMaps, tcpCommunicator, rackCount)
        {
            this.rackCount = rackCount;
        }

        public override void DeviceConnect()
        {
            base.DeviceConnect();

            ModbusFactory modbusFactory = new ModbusFactory();
            modbusSlaveNetwork = modbusFactory.CreateSlaveNetwork((communicator as TCPCommunicator)!.listener);

            var bankIndex = new ModbusControlAddressIndex(pointMap);
            var modbusSlave = CreateSlaveWithHooks(modbusFactory, deviceInfoDto.slaveId, bankIndex);
            modbusSlaveNetwork.AddSlave(modbusSlave);

            if (rackCount > 0 && rackPointMap != null)
            {
                var rackIndex = new ModbusControlAddressIndex(rackPointMap);
                for (byte i = deviceInfoDto.slaveId; i < rackCount; i++)
                {
                    var rackSlave = CreateSlaveWithHooks(modbusFactory, (byte)(i + 1), rackIndex);
                    modbusSlaveNetwork.AddSlave(rackSlave);
                }
            }

            modbusSlaveNetwork.ListenAsync();
        }

        private NModbus.IModbusSlave CreateSlaveWithHooks(ModbusFactory factory, byte slaveId, ModbusControlAddressIndex index)
        {
            var dataStore = new SlaveDataStore();
            AttachControlWriteHooks(dataStore, slaveId, index);
            return factory.CreateSlave(slaveId, dataStore);
        }

        private void AttachControlWriteHooks(SlaveDataStore dataStore, byte slaveId, ModbusControlAddressIndex index)
        {
            if (dataStore.CoilDiscretes is PointSource<bool> coils)
            {
                coils.AfterWrite += (_, e) =>
                {
                    if (!ShouldNotifyExternalControlWrite)
                        return;
                    if (index.TouchesCoilWrite(e.StartAddress, e.NumberOfPoints))
                        NotifyExternalControlWrite(slaveId);
                };
            }

            if (dataStore.HoldingRegisters is PointSource<ushort> holding)
            {
                holding.AfterWrite += (_, e) =>
                {
                    if (!ShouldNotifyExternalControlWrite)
                        return;
                    if (index.TouchesHoldingWrite(e.StartAddress, e.NumberOfPoints))
                        NotifyExternalControlWrite(slaveId);
                };
            }
        }

        public override void DeviceDisconnect()
        {
            base.DeviceDisconnect();
            if (modbusSlaveNetwork != null)
            {
                modbusSlaveNetwork.RemoveSlave(deviceInfoDto.slaveId);
                if (rackCount > 0)
                {
                    for (byte i = deviceInfoDto.slaveId; i < rackCount; i++)
                        modbusSlaveNetwork.RemoveSlave(i);
                }

                modbusSlaveNetwork.Dispose();
                modbusSlaveNetwork = null;
            }
        }
    }
}
