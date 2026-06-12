using EssSimulator.EssDeviceSimModel.Interface;
using EssSimulator.EssDeviceSimModel.Model;
using log4net;

namespace EssSimulator.EssDeviceSimModel.Propagation
{
    /// <summary>
    /// 电气输出订阅路由：上游端口输出变化时同步通知已注册下游。
    /// </summary>
    public sealed class ElectricalSignalRouter
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(ElectricalSignalRouter));
        private readonly Dictionary<ElectricalPortRef, List<Action<ElectricalOutputChangedEventArgs>>> _subscribers = new();

        public void Subscribe(
            IElectricalDevice source,
            ElectricalPort port,
            Action<ElectricalOutputChangedEventArgs> handler)
        {
            var key = new ElectricalPortRef(source.DeviceId, port.PortId);
            if (!_subscribers.TryGetValue(key, out var list))
            {
                list = new List<Action<ElectricalOutputChangedEventArgs>>();
                _subscribers[key] = list;
            }

            list.Add(handler);
            _log.Debug($"[Propagation] 注册 {key} -> {handler.Method.Name}");
        }

        public void Publish(
            IElectricalDevice source,
            ElectricalPort port,
            DeviceStepContext context,
            TimeSpan step)
        {
            var key = new ElectricalPortRef(source.DeviceId, port.PortId);
            if (!_subscribers.TryGetValue(key, out var list) || list.Count == 0)
                return;

            var args = new ElectricalOutputChangedEventArgs
            {
                Source = source,
                Port = port,
                Output = port.Output,
                Context = context,
                Step = step
            };

            foreach (var handler in list)
            {
                try { handler(args); }
                catch (Exception ex)
                {
                    _log.Error($"[Propagation] 回调异常 {key}", ex);
                }
            }
        }

        public int SubscriberCount(ElectricalPortRef key) =>
            _subscribers.TryGetValue(key, out var list) ? list.Count : 0;
    }
}
