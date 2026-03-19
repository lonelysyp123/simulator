using System.Collections.Concurrent;

namespace EssSimulator.Core
{
    public sealed class SimulatorHost
    {
        private static readonly Lazy<SimulatorHost> _instance = new(() => new SimulatorHost());
        public static SimulatorHost Instance => _instance.Value;
        private readonly ConcurrentDictionary<string, object> _store = new();
        private SimulatorHost() { }

        public void Register<T>(string key, T obj) where T : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(obj);
            _store[key] = obj;
        }

        public T? Get<T>(string key) where T : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return _store.TryGetValue(key, out var v) ? v as T : null;
        }

        public bool Contains(string key) => _store.ContainsKey(key);
    }
}
