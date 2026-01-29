using System.Net;
using System.Net.Sockets;
using NModbus;
using NModbus.Data;
using NModbus.Device;
using NModbus.IO;

namespace IEC61850_simulatorServer2
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

            // Modbus Slave
            ModbusFactory modbusFactory = new ModbusFactory();
            modbusSlaveNetwork = modbusFactory.CreateSlaveNetwork((communicator as TCPCommunicator)!.listener);
            var modbusSlave = modbusFactory.CreateSlave(deviceInfoDto.slaveId);
            modbusSlaveNetwork.AddSlave(modbusSlave);
            if (rackCount > 0)
            {
                //rack的从站id是N+1
                for (byte i = deviceInfoDto.slaveId; i < rackCount; i++)
                {
                    var rackSlave = modbusFactory.CreateSlave((byte)(i + 1));
                    modbusSlaveNetwork.AddSlave(rackSlave);
                }
            }

            modbusSlaveNetwork.ListenAsync();
        }

        public override void DeviceDisconnect()
        {
            base.DeviceDisconnect();
            // Modbus Slave
            if (modbusSlaveNetwork != null)
            {
                modbusSlaveNetwork.RemoveSlave(deviceInfoDto.slaveId);
                if (rackCount > 0)
                {
                    for (byte i = deviceInfoDto.slaveId; i < rackCount; i++)
                    {
                        modbusSlaveNetwork.RemoveSlave(i);
                    }
                }
                modbusSlaveNetwork.Dispose();
                modbusSlaveNetwork = null;
            }
        }
    }
}