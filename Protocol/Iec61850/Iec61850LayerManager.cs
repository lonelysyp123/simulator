using EssSimulator.Configuration;
using EssSimulator.Core;
using EssSimulator.LocalControl;
using log4net;

namespace EssSimulator.Protocol.Iec61850
{
    public sealed class Iec61850DeviceSnapshot
    {
        public string ServerName { get; set; } = string.Empty;
        public string IedName { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool Online { get; set; }
        public bool Enabled { get; set; }
        public int AssociatedClients { get; set; }
        public bool GoosePublishing { get; set; }
        public IReadOnlyList<string> Protocols { get; set; } = Array.Empty<string>();
    }

    /// <summary>IEC 61850 IED 编排器：一台 PCS 一个 IED，与 ProtocolLayerManager 并列。</summary>
    public sealed class Iec61850LayerManager
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Iec61850LayerManager));
        private static readonly Lazy<Iec61850LayerManager> InstanceHolder = new(() => new Iec61850LayerManager());

        public static Iec61850LayerManager Instance => InstanceHolder.Value;

        private readonly object _gate = new();
        private readonly List<Iec61850IedServer> _servers = new();
        private Iec61850Mapping? _mapping;
        private ProtocolBindings _bindings = new();

        public IReadOnlyList<Iec61850IedServer> Servers
        {
            get { lock (_gate) { return _servers.ToList(); } }
        }

        public ProtocolBindings Bindings
        {
            get { lock (_gate) { return _bindings; } }
        }

        public string? BindingsError
        {
            get { lock (_gate) { return _bindings.LoadError; } }
        }

        public void StartAll(SimulatorConfig cfg)
        {
            lock (_gate)
            {
                StopAllUnlocked();
                _bindings = ProtocolBindings.Load();
                if (_bindings.LoadError != null)
                    Log.Warn(_bindings.LoadError);

                try
                {
                    _mapping = Iec61850Mapping.Load();
                }
                catch (Exception ex)
                {
                    Log.Error("加载 IEC 61850 mapping.csv 失败，跳过 IED 启动", ex);
                    return;
                }

                string icdPath;
                try { icdPath = Iec61850Mapping.ResolveIcdPath(); }
                catch { icdPath = string.Empty; }

                var store = SimulatorHost.Instance;
                    var endpoints = EmuProtocolLayout.Enumerate(cfg);
                int basePort = cfg.Protocol.BaseEmuIec61850Port;
                int step = Math.Max(1, cfg.Protocol.EmuIec61850PortStep);

                for (int i = 0; i < endpoints.Count; i++)
                {
                    var ep = endpoints[i];
                    if (!_bindings.Allows(ep.ServerName, Iec61850Protocols.Iec61850))
                        continue;

                    var emu = store.Get<ModbusSimServer>(ep.ServerName);
                    if (emu == null)
                    {
                        Log.Warn($"[IEC61850] 找不到 {ep.ServerName}，跳过 IED");
                        continue;
                    }

                    int port = _bindings.PortOverride(ep.ServerName)
                               ?? ProtocolBindings.DefaultIec61850Port(ep.SimIndex1Based, basePort, step);

                    var ied = new Iec61850IedServer(ep.ServerName, port, _mapping)
                    {
                        IcdPath = icdPath,
                        GooseInterfaceId = cfg.Protocol.EmuIec61850GooseInterface
                    };
                    ied.Attach(emu.PointStore, (name, value) => emu.SetDataObjectByMesurePointName(name, value));
                    if (!ied.Start())
                        continue;

                    _servers.Add(ied);
                    SimServer.serverListenInfo[$"{ep.ServerName}.iec61850"] =
                        $"IEC 61850 MMS {ied.IedName} 端口 {ied.Port}";
                }

                Log.Info($"[IEC61850] 已启动 {_servers.Count} 个 PCS IED");
            }
        }

        public void StopAll()
        {
            lock (_gate)
                StopAllUnlocked();
        }

        public List<Iec61850DeviceSnapshot> GetSnapshot()
        {
            lock (_gate)
            {
                return _servers.Select(s => new Iec61850DeviceSnapshot
                {
                    ServerName = s.ServerName,
                    IedName = s.IedName,
                    Port = s.Port,
                    Online = s.IsOnline,
                    Enabled = true,
                    AssociatedClients = s.AssociatedClients,
                    GoosePublishing = s.GoosePublishing,
                    Protocols = _bindings.ProtocolsFor(s.ServerName)
                }).ToList();
            }
        }

        public bool TrySetOnline(string serverName, bool online)
        {
            lock (_gate)
            {
                var ied = _servers.FirstOrDefault(s =>
                    string.Equals(s.ServerName, serverName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(s.IedName, serverName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals($"{s.ServerName}.iec61850", serverName, StringComparison.OrdinalIgnoreCase));
                if (ied == null)
                    return false;

                if (online)
                    return ied.IsOnline || ied.Start();

                if (ied.IsOnline)
                    ied.Stop();
                SimServer.serverListenInfo.Remove($"{ied.ServerName}.iec61850");
                return true;
            }
        }

        public void ReloadBindingsAndRestart(SimulatorConfig cfg)
        {
            StartAll(cfg);
        }

        private void StopAllUnlocked()
        {
            foreach (var ied in _servers)
            {
                try
                {
                    ied.Stop();
                    SimServer.serverListenInfo.Remove($"{ied.ServerName}.iec61850");
                }
                catch (Exception ex)
                {
                    Log.Warn($"停止 {ied.ServerName} IED 时异常", ex);
                }
            }

            _servers.Clear();
        }
    }
}
