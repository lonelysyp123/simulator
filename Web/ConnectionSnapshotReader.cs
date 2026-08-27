using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using EssSimulator.Display;

namespace EssSimulator.Web
{
    /// <summary>连接/监听信息快照（对应 TUI DrawClientConnectInfo）。</summary>
    public sealed class ConnectionSnapshotDto
    {
        public List<NetworkInterfaceDto> NetworkInterfaces { get; set; } = new();
        public List<ServerListenDto> Servers { get; set; } = new();
        public List<ClientConnectDto> Clients { get; set; } = new();
        public List<LinkStatusDto> LinkStatus { get; set; } = new();
    }

    public sealed class NetworkInterfaceDto
    {
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
    }

    public sealed class ServerListenDto
    {
        public string Server { get; set; } = "";
        public string ListenInfo { get; set; } = "";
    }

    public sealed class ClientConnectDto
    {
        public string Client { get; set; } = "";
        public string State { get; set; } = "";
    }

    public static class ConnectionSnapshotReader
    {
        public static ConnectionSnapshotDto Read()
        {
            var dto = new ConnectionSnapshotDto();

            foreach (NetworkInterface net in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (net.OperationalStatus != OperationalStatus.Up) continue;
                if (net.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                var ipProps = net.GetIPProperties();
                foreach (var addr in ipProps.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        dto.NetworkInterfaces.Add(new NetworkInterfaceDto
                        {
                            Name = net.Name,
                            Address = addr.Address.ToString()
                        });
                    }
                }
            }

            if (SimServer.serverListenInfo != null)
            {
                foreach (var kv in SimServer.serverListenInfo.OrderBy(kv => ServerSortKey(kv.Key)))
                {
                    dto.Servers.Add(new ServerListenDto { Server = kv.Key, ListenInfo = kv.Value });
                }
            }

            if (SimServer.clientConnectState != null)
            {
                foreach (var kv in SimServer.clientConnectState)
                {
                    dto.Clients.Add(new ClientConnectDto
                    {
                        Client = kv.Key,
                        State = kv.Value ? "已连接" : "未连接"
                    });
                }
            }

            dto.LinkStatus = EssCommand.BuildAllLinkStatus();
            return dto;
        }

        private static (int, int) ServerSortKey(string name)
        {
            if (name.StartsWith("simBms")) return (0, ExtractTrailingNumber(name));
            if (name.StartsWith("simEmu")) return (1, ExtractTrailingNumber(name));
            if (name == "simEm") return (2, 0);
            if (name.StartsWith("simLc")) return (3, ExtractTrailingNumber(name));
            if (name.StartsWith("simPvMeter")) return (5, ExtractTrailingNumber(name));
            if (name.StartsWith("simPv")) return (4, ExtractTrailingNumber(name));
            return (99, 0);
        }

        private static int ExtractTrailingNumber(string name)
        {
            var digits = new string(name.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
            return int.TryParse(digits, out var n) ? n : 0;
        }
    }
}
