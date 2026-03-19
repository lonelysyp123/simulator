using System.Net;
using System.Net.Sockets;
using log4net;

namespace EssSimulator
{
    /// <summary>
    /// TCP连接器
    /// @author: syp
    /// @date: 2024/6/29
    /// </summary>
    public class TCPCommunicator : CommunicatorBase
    {
        public TcpListener? listener;
        ILog log = LogManager.GetLogger(typeof(TCPCommunicator));

        public TCPCommunicator(DeviceInfoDto deviceInfoDto) : base(deviceInfoDto)
        {

        }

        public override void Connect()
        {
            try
            {
                if (deviceInfoDto.ip == null) return;
                IPAddress iPAddress = IPAddress.Parse(deviceInfoDto.ip);
                listener = new TcpListener(iPAddress, deviceInfoDto.port);
                listener.Start();
            }
            catch (Exception ex)
            {
                log.Error("[Program] 应用启动, 连接器启动失败：" + ex.Message);
            }
        }

        public override void Disconnect()
        {
            if (listener != null)
            {
                listener.Stop();
                listener.Dispose();
                listener = null;
            }
        }

        /// <summary>
        /// 连接状态0=》未连接，1=》已连接
        /// </summary>
        /// <returns></returns>
        public override int GetCommunicatorStatus()
        {
            if (listener != null)
            {
                return 1;
            }
            return 0;
        }
    }
}