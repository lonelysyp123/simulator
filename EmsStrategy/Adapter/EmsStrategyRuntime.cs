using System.Text.Json;
using System.Text.Json.Serialization;
using EssSimulator.Core;
using EssSimulator.EmsStrategy.Application;
using EssSimulator.EmsStrategy.Domain;

namespace EssSimulator.EmsStrategy.Adapter;

/// <summary>策略运行时：配置热更新、占用闸门、引擎单例。</summary>
public sealed class EmsStrategyRuntime
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly object _gate = new();
    public IEmsStrategyEngine Engine { get; }

    public EmsStrategyRuntime() : this(new EmsStrategyEngine(), LoadFileOrDefault()) { }

    public EmsStrategyRuntime(IEmsStrategyEngine engine, EmsStrategyConfig? config = null)
    {
        Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        var cfg = (config ?? EmsStrategyConfig.CreateDefault()).Clone();
        Engine.Initialize(cfg);
        lock (_gate)
        {
            Config = cfg;
        }
        SyncGate(cfg.Enabled);
    }

    public EmsStrategyConfig Config { get; private set; }

    public bool TrySetEnabled(bool enabled, out string error)
    {
        lock (_gate)
        {
            var next = Config.Clone();
            next.Enabled = enabled;
            return TryReplaceLocked(next, out error);
        }
    }

    public bool TryReplace(EmsStrategyConfig config, out string error)
    {
        lock (_gate)
            return TryReplaceLocked(config.Clone(), out error);
    }

    public EmsStrategySnapshot Snapshot()
    {
        lock (_gate)
            return Engine.GetSnapshot();
    }

    private bool TryReplaceLocked(EmsStrategyConfig next, out string error)
    {
        if (next.Enabled)
        {
            if (!ExternalControlGate.TryOccupy(ExternalControlOwner.EmsStrategy, out error))
                return false;
        }
        else if (ExternalControlGate.Owner == ExternalControlOwner.EmsStrategy)
        {
            ExternalControlGate.Release(ExternalControlOwner.EmsStrategy);
        }

        Engine.UpdateConfig(next);
        Config = next;
        error = "";
        return true;
    }

    private static void SyncGate(bool enabled)
    {
        if (enabled)
            ExternalControlGate.TryOccupy(ExternalControlOwner.EmsStrategy, out _);
        else if (ExternalControlGate.Owner == ExternalControlOwner.EmsStrategy)
            ExternalControlGate.Release(ExternalControlOwner.EmsStrategy);
    }

    public static EmsStrategyConfig LoadFileOrDefault()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "configs", "ems-strategy.json");
        if (!File.Exists(path))
            return EmsStrategyConfig.CreateDefault();
        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<EmsStrategyConfig>(json, JsonOptions)
                   ?? EmsStrategyConfig.CreateDefault();
        }
        catch
        {
            return EmsStrategyConfig.CreateDefault();
        }
    }
}
