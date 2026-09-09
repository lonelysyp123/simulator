using System.Globalization;
using System.Net;
using System.Net.Sockets;
using EssSimulator.DataExchange.Adapters;
using IEC61850.Common;
using IEC61850.GOOSE.Subscriber;
using IEC61850.Server;
using log4net;

namespace EssSimulator.Protocol.Iec61850
{
    public sealed class Iec61850ReportEventArgs : EventArgs
    {
        public required string RptId { get; init; }
        public required string Reason { get; init; }
        public required int SeqNum { get; init; }
        public required IReadOnlyList<Iec61850ReportEntry> Entries { get; init; }
    }

    public sealed class Iec61850ReportEntry
    {
        public string Ref { get; set; } = string.Empty;
        public object? Value { get; set; }
    }

    /// <summary>
    /// 一台 PCS 一个 libiec61850 MMS IED：动态模型、Get/Set、Direct-operate、URCB；yk/yt 只订阅入向 GOOSE 遥控，不发布。
    /// </summary>
    public sealed class Iec61850IedServer : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Iec61850IedServer));

        private readonly Iec61850Mapping _mapping;
        private readonly Iec61850PcsModel _model;
        private readonly Iec61850GooseIngress _ingress;
        private readonly Dictionary<IntPtr, Iec61850MapEntry> _attrToMap = new();
        private IedServer? _server;
        private IProtocolPointStore? _store;
        private Action<string, object>? _writeControl;
        private Func<string, object?>? _readPoint;
        private GooseSubscriber? _gooseSubscriber;
        private GooseListener? _gooseListener;
        private Iec61850GooseReceiverHost? _gooseHost;
        private int _seqNum;
        private int _inNativeCallback;
        private bool _disposed;

        public Iec61850IedServer(string serverName, int port, Iec61850Mapping mapping)
        {
            Iec61850Native.EnsureLoaded();
            ServerName = serverName;
            Port = port;
            IedName = Iec61850Mapping.IedNameFor(serverName);
            _mapping = mapping;
            _model = Iec61850PcsModel.Build(IedName, mapping);
            _ingress = new Iec61850GooseIngress(_mapping.GooseEntries, IedName);
            foreach (var pair in _model.LeafByParam)
            {
                if (_mapping.ByParam.TryGetValue(pair.Key, out var entry))
                    _attrToMap[pair.Value.self] = entry;
            }

            foreach (var pair in _model.SetpointByParam)
            {
                if (_mapping.ByParam.TryGetValue(pair.Key, out var entry))
                    _attrToMap[pair.Value.self] = entry;
            }
        }

        public string ServerName { get; }
        public string IedName { get; }
        public int Port { get; private set; }
        public bool IsOnline { get; private set; }
        public string? IcdPath { get; init; }
        /// <summary>空=按操作系统选网卡供入向订阅；<c>none</c> 不收二层 GOOSE。</summary>
        public string? GooseInterfaceId { get; init; }
        public bool GooseSubscribing { get; private set; }
        public ushort? GooseSubscribeAppId { get; private set; }
        public uint? LastGooseStNum => _ingress.LastStNum;
        public DateTime? LastGooseUtc => _ingress.LastAcceptedUtc;

        public int AssociatedClients => _server?.GetNumberOfOpenConnections() ?? 0;
        internal int SpcHandlerCount => _model.ControlDoByParam.Count;
        internal int GooseEntryCount => _model.GooseEntryCount;

        public event EventHandler<Iec61850ReportEventArgs>? ReportGenerated;

        public void Attach(IProtocolPointStore store, Action<string, object> writeControl)
        {
            if (_store != null)
                _store.PointsChanged -= OnPointsChanged;
            _store = store;
            _writeControl = writeControl;
            _readPoint = store.GetCachedValue;
            store.PointsChanged += OnPointsChanged;
            PushAllFromShadow();
        }

        public void Attach(Func<string, object?> readPoint, Action<string, object> writeControl)
        {
            _readPoint = readPoint;
            _writeControl = writeControl;
            PushAllFromShadow();
        }

        public bool Start()
        {
            if (IsOnline && _server is { } running && running.IsRunning())
                return true;

            try
            {
                if (Port <= 0)
                    Port = PickFreePort();

                if (_server == null)
                {
                    var config = new IedServerConfig
                    {
                        Edition = Iec61850Edition.EDITION_2,
                        FileServiceEnabled = false,
                        LogServiceEnabled = false,
                        ReportBufferSizeForURCBs = 65535,
                        MaxMmsConnections = 8,
                        UseIntegratedGoosePublisher = false
                    };
                    _server = new IedServer(_model.Model, config);
                    _server.SetServerIdentity("TrinaStorage", "EssSimulator-PCS", "1.0");
                    _server.SetWriteAccessPolicy(FunctionalConstraint.SP, AccessPolicy.ACCESS_POLICY_ALLOW);
                    RegisterHandlers(_server);
                }

                PushAllFromShadow();
                _server.Start(Port);
                IsOnline = _server.IsRunning();
                if (IsOnline)
                    Log.Info($"[IEC61850] {ServerName} IED {IedName} 监听 MMS 端口 {Port}");
                else
                    Log.Error($"[IEC61850] {ServerName} IedServer.Start({Port}) 后未处于运行状态");
                return IsOnline;
            }
            catch (Exception ex)
            {
                Log.Error($"[IEC61850] {ServerName} 启动失败", ex);
                Stop();
                return false;
            }
        }

        public void Stop()
        {
            UnbindGooseSubscribe();
            IsOnline = false;
            try { _server?.Stop(); }
            catch (Exception ex) { Log.Warn($"[IEC61850] {ServerName} Stop 异常", ex); }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            if (_store != null)
                _store.PointsChanged -= OnPointsChanged;
            Stop();
            try { _server?.Destroy(); }
            catch { /* native already gone */ }
            _server = null;
            _model.Dispose();
        }

        public IReadOnlyList<string> GetServerDirectory() => _mapping.LogicalDevices();

        public IReadOnlyList<string> GetLogicalDeviceDirectory(string? ld)
        {
            if (!string.IsNullOrWhiteSpace(ld)
                && !string.Equals(ld, Iec61850Mapping.LogicalDeviceName, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(ld, IedName + Iec61850Mapping.LogicalDeviceName, StringComparison.OrdinalIgnoreCase))
            {
                return Array.Empty<string>();
            }

            return _mapping.LogicalNodes();
        }

        public IReadOnlyList<string> GetLogicalNodeDirectory(string ln) =>
            _mapping.DataObjects(StripIed(ln));

        public bool TryGetDataValues(IReadOnlyList<string> refs, out Dictionary<string, object?> values, out string error)
        {
            values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            error = string.Empty;
            foreach (var r in refs)
            {
                if (!_mapping.TryResolve(r, IedName, out var entry))
                {
                    error = $"未知对象引用: {r}";
                    return false;
                }

                values[Iec61850Mapping.AbsoluteRef(IedName, entry.ObjectRef)] = ReadEngineering(entry);
            }

            return true;
        }

        public bool TrySetDataValues(IReadOnlyDictionary<string, object?> values, out string error)
        {
            error = string.Empty;
            foreach (var pair in values)
            {
                if (!_mapping.TryResolve(pair.Key, IedName, out var entry))
                {
                    error = $"未知对象引用: {pair.Key}";
                    return false;
                }

                if (!entry.IsControllable)
                {
                    error = $"{entry.ObjectRef} 不可写（ctlModel=0）";
                    return false;
                }

                if (!TryWrite(entry, pair.Value, out error))
                    return false;
            }

            return true;
        }

        public bool TryOperate(string objectRef, object? ctlVal, out string error)
        {
            error = string.Empty;
            if (!_mapping.TryResolve(objectRef, IedName, out var entry))
            {
                error = $"未知对象引用: {objectRef}";
                return false;
            }

            if (!entry.IsControllable)
            {
                error = $"{entry.ObjectRef} 不支持 Operate（ctlModel=0）";
                return false;
            }

            return TryWrite(entry, ctlVal, out error);
        }

        public bool TryEnableUrcb(string? name, bool enabled, int? integrityPeriodMs, out string error)
        {
            error = string.Empty;
            if (!string.IsNullOrWhiteSpace(name)
                && !name.Contains("URCB", StringComparison.OrdinalIgnoreCase)
                && !name.Contains("LLN0", StringComparison.OrdinalIgnoreCase)
                && !name.Contains(".RP.", StringComparison.OrdinalIgnoreCase))
            {
                error = $"未知 URCB: {name}";
                return false;
            }

            return true;
        }

        internal bool TryBindGooseSubscribe(
            Iec61850GooseReceiverHost host,
            string iface,
            ushort appId,
            string? goCbRefFilter,
            out string error)
        {
            error = string.Empty;
            UnbindGooseSubscribe();
            if (_model.GooseEntryCount == 0)
            {
                error = "无 GOOSE 点";
                return false;
            }

            try
            {
                // 绝不可传 null（macOS 上 GooseSubscriber_create(null) 会 SIGSEGV）。
                // 空串 + setObserver = 只按 AppID 匹配（libiec61850 goose_observer 用法）。
                bool matchAnyGoCb = string.IsNullOrWhiteSpace(goCbRefFilter);
                string goCbRef = matchAnyGoCb ? "" : goCbRefFilter!.Trim();
                var subscriber = new GooseSubscriber(goCbRef);
                GC.SuppressFinalize(subscriber);
                if (matchAnyGoCb && !Iec61850Native.TrySetGooseSubscriberObserver(subscriber))
                {
                    error = "无法启用 GooseSubscriber observer";
                    subscriber.Dispose();
                    return false;
                }
                subscriber.SetAppId(appId);
                _gooseListener = OnGooseReceived;
                subscriber.SetListener(_gooseListener, this);
                if (!host.TryAdd(iface, subscriber, out error))
                {
                    subscriber.Dispose();
                    _gooseListener = null;
                    return false;
                }

                _gooseSubscriber = subscriber;
                _gooseHost = host;
                GooseSubscribeAppId = appId;
                GooseSubscribing = host.IsRunning;
                Log.Info($"[IEC61850] {ServerName} GOOSE 订阅 AppID=0x{appId:X4} iface={iface}");
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Log.Warn($"[IEC61850] {ServerName} GOOSE 订阅未挂上", ex);
                UnbindGooseSubscribe();
                return false;
            }
        }

        internal void MarkGooseSubscribing(bool running) => GooseSubscribing = running && _gooseSubscriber != null;

        internal void UnbindGooseSubscribe()
        {
            GooseSubscribing = false;
            GooseSubscribeAppId = null;
            var sub = _gooseSubscriber;
            var host = _gooseHost;
            _gooseSubscriber = null;
            _gooseHost = null;
            _gooseListener = null;
            host?.Remove(sub);
        }

        internal bool TryApplyGoose(
            uint stNum,
            bool isTest,
            string? goCbRef,
            IReadOnlyList<object?> values,
            out string skipReason)
        {
            if (!_ingress.TryAccept(stNum, isTest, goCbRef, values, out var writes, out skipReason))
                return false;

            foreach (var pair in writes)
            {
                if (!_mapping.ByParam.TryGetValue(pair.Key, out var entry))
                    continue;
                TryWrite(entry, pair.Value, out _);
            }

            return true;
        }

        private void OnGooseReceived(GooseSubscriber subscriber, object parameter)
        {
            Interlocked.Increment(ref _inNativeCallback);
            try
            {
                var values = Iec61850GooseIngress.FromDataset(subscriber.GetDataSetValues());
                uint stNum = subscriber.GetStNum();
                uint sqNum = subscriber.GetSqNum();
                bool isTest = subscriber.IsTest();
                string? goCbRef = subscriber.GetGoCbRef();
                bool ok = _ingress.TryAccept(stNum, isTest, goCbRef, values, out var writes, out var reason);
                if (ok)
                {
                    foreach (var pair in writes)
                    {
                        if (!_mapping.ByParam.TryGetValue(pair.Key, out var entry))
                            continue;
                        TryWrite(entry, pair.Value, out _);
                    }
                }

                string result = ok ? "applied" : (string.IsNullOrEmpty(reason) ? "error" : $"skip:{reason}");
                string writeSummary = writes.Count > 0
                    ? string.Join(" ", writes.Select(kv => $"{kv.Key}={FormatTrafficValue(kv.Value)}"))
                    : "";
                string summary = ok
                    ? $"GOOSE stNum={stNum} sqNum={sqNum} {writeSummary}".Trim()
                    : $"GOOSE stNum={stNum} sqNum={sqNum} {reason}";

                var valueMap = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                var goose = _mapping.GooseEntries;
                for (int i = 0; i < values.Count && i < goose.Count; i++)
                    valueMap[goose[i].ParamName] = values[i];

                Iec61850TrafficLog.Append(new Iec61850TrafficMessage
                {
                    Direction = "ingress",
                    Protocol = "goose",
                    ServerName = ServerName,
                    IedName = IedName,
                    AppId = GooseSubscribeAppId,
                    GoCbRef = goCbRef,
                    StNum = stNum,
                    SqNum = sqNum,
                    IsTest = isTest,
                    Result = result,
                    Summary = summary,
                    Writes = writes.Count > 0
                        ? writes.ToDictionary(kv => kv.Key, kv => (object?)kv.Value, StringComparer.OrdinalIgnoreCase)
                        : null,
                    Values = valueMap.Count > 0 ? valueMap : null
                });

                if (!ok && !string.IsNullOrEmpty(reason) && reason is not "stNum")
                    Log.Info($"[IEC61850] {ServerName} 忽略入向 GOOSE: {reason} stNum={stNum} goCb={goCbRef}");
                else if (ok)
                    Log.Info($"[IEC61850] {ServerName} 入向 GOOSE 已应用 stNum={stNum}");
            }
            catch (Exception ex)
            {
                Log.Warn($"[IEC61850] {ServerName} 入向 GOOSE 处理失败", ex);
                Iec61850TrafficLog.Append(new Iec61850TrafficMessage
                {
                    Direction = "ingress",
                    Protocol = "goose",
                    ServerName = ServerName,
                    IedName = IedName,
                    AppId = GooseSubscribeAppId,
                    Result = "error",
                    Summary = $"GOOSE 处理异常: {ex.Message}"
                });
            }
            finally
            {
                Interlocked.Decrement(ref _inNativeCallback);
            }
        }

        private static string FormatTrafficValue(object value) =>
            value switch
            {
                bool b => b ? "true" : "false",
                float f => f.ToString("G", CultureInfo.InvariantCulture),
                double d => d.ToString("G", CultureInfo.InvariantCulture),
                IFormattable fmt => fmt.ToString(null, CultureInfo.InvariantCulture) ?? "",
                _ => value?.ToString() ?? ""
            };

        internal static string? ResolveGooseInterface(string? configured)
        {
            if (string.Equals(configured, "none", StringComparison.OrdinalIgnoreCase)
                || string.Equals(configured, "off", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(configured))
                return configured.Trim();

            if (OperatingSystem.IsWindows())
                return "0";
            if (OperatingSystem.IsMacOS())
                return "en0";
            return "eth0";
        }

        private void RegisterHandlers(IedServer server)
        {
            foreach (var pair in _model.ControlDoByParam)
            {
                if (!_mapping.ByParam.TryGetValue(pair.Key, out var entry))
                    continue;
                if (!Iec61850PcsModel.TryParseRef(entry.ObjectRef, out var lnName, out var doName, out _))
                    continue;
                var controlDo = _model.Model.GetModelNodeByShortObjectReference(
                                    Iec61850Mapping.LogicalDeviceName + "/" + lnName + "." + doName) as DataObject
                                ?? pair.Value;
                server.SetControlHandler(controlDo, OnControl, entry);
            }

            foreach (var pair in _model.SetpointByParam)
            {
                if (!_mapping.ByParam.TryGetValue(pair.Key, out var entry))
                    continue;
                server.HandleWriteAccessForComplexAttribute(pair.Value, OnWriteAccess, entry);
            }
        }

        private ControlHandlerResult OnControl(ControlAction action, object parameter, MmsValue ctlVal, bool test)
        {
            if (test)
                return ControlHandlerResult.OK;
            if (parameter is not Iec61850MapEntry entry)
                return ControlHandlerResult.FAILED;
            Interlocked.Increment(ref _inNativeCallback);
            try
            {
                return TryWrite(entry, FromMms(ctlVal), out _)
                    ? ControlHandlerResult.OK
                    : ControlHandlerResult.FAILED;
            }
            catch (Exception ex)
            {
                Log.Warn($"[IEC61850] {ServerName} Operate {entry.ParamName} 失败", ex);
                return ControlHandlerResult.FAILED;
            }
            finally
            {
                Interlocked.Decrement(ref _inNativeCallback);
            }
        }

        private MmsDataAccessError OnWriteAccess(DataAttribute dataAttr, MmsValue value, ClientConnection connection, object parameter)
        {
            if (!_attrToMap.TryGetValue(dataAttr.self, out var entry))
                entry = parameter as Iec61850MapEntry;
            if (entry == null)
                return MmsDataAccessError.OBJECT_UNDEFINED;
            Interlocked.Increment(ref _inNativeCallback);
            try
            {
                return TryWrite(entry, FromMms(value), out _)
                    ? MmsDataAccessError.SUCCESS
                    : MmsDataAccessError.OBJECT_VALUE_INVALID;
            }
            finally
            {
                Interlocked.Decrement(ref _inNativeCallback);
            }
        }

        private bool TryWrite(Iec61850MapEntry entry, object? raw, out string error)
        {
            error = string.Empty;
            if (_writeControl == null)
            {
                error = "IED 未绑定控制回写";
                return false;
            }

            object value = CoerceWrite(entry, raw);
            _writeControl(entry.ParamName, value);
            PushOne(entry, value, lockModel: _inNativeCallback == 0);
            return true;
        }

        private object? ReadEngineering(Iec61850MapEntry entry)
        {
            var raw = _readPoint?.Invoke(entry.ParamName);
            if (raw == null)
                return DefaultValue(entry);

            if (string.Equals(entry.Cdc, "SPC", StringComparison.OrdinalIgnoreCase))
                return ToBool(raw);

            return raw;
        }

        private void OnPointsChanged(object? sender, ProtocolPointChangedEventArgs e)
        {
            if (e.Values.Count == 0)
                return;

            var entries = new List<Iec61850ReportEntry>();
            bool locked = false;
            try
            {
                if (_server != null && _inNativeCallback == 0)
                {
                    _server.LockDataModel();
                    locked = true;
                }

                foreach (var pair in e.Values)
                {
                    if (!_mapping.ByParam.TryGetValue(pair.Key, out var map))
                        continue;
                    PushOne(map, pair.Value, lockModel: false);
                    if (!map.IsStatusOrMeas)
                        continue;
                    entries.Add(new Iec61850ReportEntry
                    {
                        Ref = Iec61850Mapping.AbsoluteRef(IedName, map.ObjectRef),
                        Value = pair.Value
                    });
                }
            }
            finally
            {
                if (locked)
                    _server?.UnlockDataModel();
            }

            if (entries.Count == 0)
                return;

            int seq = Interlocked.Increment(ref _seqNum);
            ReportGenerated?.Invoke(this, new Iec61850ReportEventArgs
            {
                RptId = $"{IedName}PCS/LLN0.{Iec61850PcsModel.UrcbName}",
                Reason = "dchg",
                SeqNum = seq,
                Entries = entries
            });
        }

        private void PushAllFromShadow()
        {
            if (_server == null && _model.LeafByParam.Count == 0)
                return;
            bool locked = false;
            try
            {
                if (_server != null)
                {
                    _server.LockDataModel();
                    locked = true;
                }

                foreach (var entry in _mapping.Entries)
                    PushOne(entry, ReadEngineering(entry), lockModel: false);
            }
            finally
            {
                if (locked)
                    _server?.UnlockDataModel();
            }
        }

        private void PushOne(Iec61850MapEntry entry, object? raw, bool lockModel)
        {
            if (!_model.LeafByParam.TryGetValue(entry.ParamName, out var attr))
                return;

            bool locked = false;
            try
            {
                if (lockModel && _server != null)
                {
                    _server.LockDataModel();
                    locked = true;
                }

                ApplyAttribute(attr, entry, raw);
            }
            finally
            {
                if (locked)
                    _server?.UnlockDataModel();
            }
        }

        private void ApplyAttribute(DataAttribute attr, Iec61850MapEntry entry, object? raw)
        {
            if (_server == null)
                return;

            if (string.Equals(entry.Cdc, "SPC", StringComparison.OrdinalIgnoreCase))
            {
                _server.UpdateBooleanAttributeValue(attr, ToBool(raw));
                return;
            }

            if (string.Equals(entry.Cdc, "INS", StringComparison.OrdinalIgnoreCase))
            {
                _server.UpdateInt32AttributeValue(attr, ToInt32(raw));
                return;
            }

            if (string.Equals(entry.Cdc, "BCR", StringComparison.OrdinalIgnoreCase))
            {
                _server.UpdateInt64AttributeValue(attr, ToInt64(raw));
                return;
            }

            _server.UpdateFloatAttributeValue(attr, ToFloat(raw));
        }

        private static object CoerceWrite(Iec61850MapEntry entry, object? raw)
        {
            if (string.Equals(entry.Cdc, "SPC", StringComparison.OrdinalIgnoreCase))
                return ToBool(raw) ? 1 : 0;
            if (raw == null)
                return 0;
            return raw;
        }

        private static object DefaultValue(Iec61850MapEntry entry) =>
            string.Equals(entry.Cdc, "SPC", StringComparison.OrdinalIgnoreCase) ? false : 0;

        private static object? FromMms(MmsValue? value)
        {
            if (value == null)
                return null;
            return value.GetType() switch
            {
                MmsType.MMS_BOOLEAN => value.GetBoolean(),
                MmsType.MMS_FLOAT => value.ToFloat(),
                MmsType.MMS_INTEGER => value.ToInt64(),
                MmsType.MMS_UNSIGNED => (long)value.ToUint32(),
                MmsType.MMS_STRUCTURE when value.Size() > 0 => FromMms(value.GetElement(0)),
                _ => value.ToString()
            };
        }

        private static bool ToBool(object? raw) =>
            raw switch
            {
                null => false,
                bool b => b,
                string s when bool.TryParse(s, out var bv) => bv,
                _ => Convert.ToDouble(raw, CultureInfo.InvariantCulture) != 0
            };

        private static float ToFloat(object? raw) =>
            raw == null ? 0f : Convert.ToSingle(raw, CultureInfo.InvariantCulture);

        private static int ToInt32(object? raw) =>
            raw == null ? 0 : Convert.ToInt32(raw, CultureInfo.InvariantCulture);

        private static long ToInt64(object? raw) =>
            raw == null ? 0 : Convert.ToInt64(raw, CultureInfo.InvariantCulture);

        private string StripIed(string ln)
        {
            ln = Iec61850Mapping.NormalizeRef(ln);
            if (ln.StartsWith(IedName, StringComparison.OrdinalIgnoreCase))
                return ln[IedName.Length..];
            return ln;
        }

        private static int PickFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
