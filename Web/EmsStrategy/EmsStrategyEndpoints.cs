using EssSimulator.Core;
using EssSimulator.EmsStrategy.Adapter;
using EssSimulator.EmsStrategy.Application;
using EssSimulator.EmsStrategy.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EssSimulator.Web.EmsStrategy;

public sealed class EmsStrategyStatusDto
{
    public bool Enabled { get; set; }
    public string GateOwner { get; set; } = "";
    public EmsStrategyConfig Config { get; set; } = EmsStrategyConfig.CreateDefault();
    public EmsStrategySnapshot Snapshot { get; set; } = new();
}

public sealed class EmsStrategyPatchRequest
{
    public bool? Enabled { get; set; }
    public bool? SystemSwitch { get; set; }
    public bool? ActiveEnable { get; set; }
    public bool? ReactiveEnable { get; set; }
    public ActiveMode? ActiveMode { get; set; }
    public ReactiveMode? ReactiveMode { get; set; }
    public LocalRemote? LocalRemote { get; set; }
    public double? LocalActiveSetKw { get; set; }
    public double? RemoteActiveSetKw { get; set; }
    public double? LocalReactiveSetKvar { get; set; }
    public double? PowerFactorSet { get; set; }
    public double? VoltageSetV { get; set; }
    public bool? SlopeEnabled { get; set; }
    public bool? PrimaryFrequencyEnabled { get; set; }
    public bool? InertiaEnabled { get; set; }
    public bool? VoltageDroopEnabled { get; set; }
    public bool? SocBalance { get; set; }
}

public static class EmsStrategyEndpoints
{
    public static IEndpointRouteBuilder MapEmsStrategyEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/ems-strategy");

        g.MapGet("/", (EmsStrategyRuntime runtime) => Results.Ok(ToStatus(runtime)));

        g.MapPost("/enable", (EmsStrategyEnableRequest? req, EmsStrategyRuntime runtime) =>
        {
            bool enabled = req?.Enabled ?? true;
            if (!runtime.TrySetEnabled(enabled, out var error))
                return Results.Json(new { ok = false, message = error }, statusCode: StatusCodes.Status409Conflict);
            return Results.Ok(new
            {
                ok = true,
                message = enabled ? "EMS 策略已启用，外部指令已拒绝" : "EMS 策略已关闭",
                status = ToStatus(runtime)
            });
        });

        g.MapPost("/", (EmsStrategyPatchRequest? req, EmsStrategyRuntime runtime) =>
        {
            var next = runtime.Config.Clone();
            ApplyPatch(next, req ?? new EmsStrategyPatchRequest());
            if (!runtime.TryReplace(next, out var error))
                return Results.Json(new { ok = false, message = error }, statusCode: StatusCodes.Status409Conflict);
            return Results.Ok(new { ok = true, message = "已更新 EMS 策略", status = ToStatus(runtime) });
        });

        return app;
    }

    private static EmsStrategyStatusDto ToStatus(EmsStrategyRuntime runtime) => new()
    {
        Enabled = runtime.Config.Enabled,
        GateOwner = ExternalControlGate.Owner.ToString(),
        Config = runtime.Config,
        Snapshot = runtime.Snapshot()
    };

    private static void ApplyPatch(EmsStrategyConfig cfg, EmsStrategyPatchRequest req)
    {
        if (req.Enabled is { } en)
            cfg.Enabled = en;
        if (req.SystemSwitch is { } sw)
            cfg.SystemSwitch = sw;
        if (req.ActiveEnable is { } ae)
            cfg.ActiveEnable = ae;
        if (req.ReactiveEnable is { } re)
            cfg.ReactiveEnable = re;
        if (req.ActiveMode is { } mode)
            cfg.ActiveMode = mode;
        if (req.ReactiveMode is { } rm)
            cfg.ReactiveMode = rm;
        if (req.LocalRemote is { } lr)
            cfg.LocalRemote = lr;
        if (req.LocalActiveSetKw is { } p)
            cfg.LocalActiveSetKw = p;
        if (req.RemoteActiveSetKw is { } rp)
            cfg.RemoteActiveSetKw = rp;
        if (req.LocalReactiveSetKvar is { } q)
            cfg.LocalReactiveSetKvar = q;
        if (req.PowerFactorSet is { } pf)
            cfg.PowerFactorSet = pf;
        if (req.VoltageSetV is { } u)
            cfg.VoltageSetV = u;
        if (req.SlopeEnabled is { } se)
            cfg.Slope.Enabled = se;
        if (req.PrimaryFrequencyEnabled is { } pfe)
            cfg.PrimaryFrequency.Enabled = pfe;
        if (req.InertiaEnabled is { } ie)
            cfg.Inertia.Enabled = ie;
        if (req.VoltageDroopEnabled is { } vd)
            cfg.VoltageDroop.Enabled = vd;
        if (req.SocBalance is { } soc)
            cfg.Distribution.SocBalance = soc;
    }
}

public sealed class EmsStrategyEnableRequest
{
    public bool Enabled { get; set; } = true;
}
