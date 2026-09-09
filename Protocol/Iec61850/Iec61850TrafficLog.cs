namespace EssSimulator.Protocol.Iec61850
{
    /// <summary>IEC 61850 交互报文（GOOSE 入向 / 系统事件）。仅内存环形缓冲。</summary>
    public sealed class Iec61850TrafficMessage
    {
        public long Id { get; init; }
        public DateTime Utc { get; init; }
        /// <summary>ingress | system</summary>
        public string Direction { get; init; } = "ingress";
        /// <summary>goose | system</summary>
        public string Protocol { get; init; } = "goose";
        public string ServerName { get; init; } = "";
        public string IedName { get; init; } = "";
        public int? AppId { get; init; }
        public string? GoCbRef { get; init; }
        public long? StNum { get; init; }
        public long? SqNum { get; init; }
        public bool IsTest { get; init; }
        /// <summary>applied | skip:test | skip:stNum | skip:* | error | system</summary>
        public string Result { get; init; } = "";
        public string Summary { get; init; } = "";
        public Dictionary<string, object?>? Writes { get; init; }
        public Dictionary<string, object?>? Values { get; init; }
    }

    /// <summary>进程内 GOOSE/系统报文环，供 Web API / SignalR 消费。</summary>
    public static class Iec61850TrafficLog
    {
        public const int DefaultCapacity = 1000;

        private static readonly object Gate = new();
        private static readonly LinkedList<Iec61850TrafficMessage> Buffer = new();
        private static long _nextId = 1;
        private static int _capacity = DefaultCapacity;

        public static event Action<Iec61850TrafficMessage>? MessageAppended;

        public static int Capacity
        {
            get { lock (Gate) return _capacity; }
            set { lock (Gate) { _capacity = Math.Clamp(value, 100, 10000); TrimUnlocked(); } }
        }

        public static Iec61850TrafficMessage Append(Iec61850TrafficMessage draft)
        {
            Iec61850TrafficMessage msg;
            lock (Gate)
            {
                msg = new Iec61850TrafficMessage
                {
                    Id = _nextId++,
                    Utc = draft.Utc == default ? DateTime.UtcNow : draft.Utc,
                    Direction = draft.Direction,
                    Protocol = draft.Protocol,
                    ServerName = draft.ServerName,
                    IedName = draft.IedName,
                    AppId = draft.AppId,
                    GoCbRef = draft.GoCbRef,
                    StNum = draft.StNum,
                    SqNum = draft.SqNum,
                    IsTest = draft.IsTest,
                    Result = draft.Result,
                    Summary = draft.Summary,
                    Writes = draft.Writes,
                    Values = draft.Values
                };
                Buffer.AddFirst(msg);
                TrimUnlocked();
            }

            try { MessageAppended?.Invoke(msg); }
            catch { /* 订阅方异常不影响协议 */ }
            return msg;
        }

        public static IReadOnlyList<Iec61850TrafficMessage> Snapshot(string? serverName = null, int limit = 200)
        {
            limit = Math.Clamp(limit, 1, 2000);
            lock (Gate)
            {
                IEnumerable<Iec61850TrafficMessage> q = Buffer;
                if (!string.IsNullOrWhiteSpace(serverName))
                {
                    q = q.Where(m =>
                        string.Equals(m.ServerName, serverName, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(m.IedName, serverName, StringComparison.OrdinalIgnoreCase));
                }

                return q.Take(limit).ToList();
            }
        }

        public static void Clear()
        {
            lock (Gate) { Buffer.Clear(); }
        }

        private static void TrimUnlocked()
        {
            while (Buffer.Count > _capacity)
                Buffer.RemoveLast();
        }
    }
}
