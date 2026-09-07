using EssSimulator.Configuration;
using EssSimulator.Display;
using EssSimulator.Protocol.Iec61850;
using EssSimulator.Protocol.Modbus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace EssSimulator.Web
{
    public sealed class ProtocolBindingsChangeRequest
    {
        public List<ProtocolBindingEntry> Entries { get; set; } = new();
        public bool Rebuild { get; set; } = true;
    }

    public static class Iec61850Endpoints
    {
        public static IEndpointRouteBuilder MapIec61850Endpoints(this IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/api/iec61850");

            g.MapGet("/", () =>
            {
                var manager = Iec61850LayerManager.Instance;
                return Results.Ok(new
                {
                    devices = manager.GetSnapshot(),
                    bindings = manager.Bindings.Entries,
                    bindingsError = manager.BindingsError
                });
            });

            g.MapGet("/bindings", () =>
            {
                var bindings = ProtocolBindings.Load();
                return Results.Ok(new
                {
                    entries = bindings.Entries,
                    error = bindings.LoadError
                });
            });

            g.MapPut("/bindings", (ProtocolBindingsChangeRequest req, IOptions<SimulatorConfig> cfg) =>
            {
                if (req?.Entries == null)
                    return Results.BadRequest(new { ok = false, message = "修改内容为空" });

                foreach (var entry in req.Entries)
                {
                    if (string.IsNullOrWhiteSpace(entry.Name))
                        return Results.BadRequest(new { ok = false, message = "设备名不能为空" });
                    if (entry.Protocols == null || entry.Protocols.Count == 0)
                        return Results.BadRequest(new { ok = false, message = $"{entry.Name} 至少选择一种协议" });
                }

                ProtocolBindings.Save(req.Entries);
                if (req.Rebuild)
                {
                    ProtocolLayerManager.Instance.Rebuild();
                    Iec61850LayerManager.Instance.ReloadBindingsAndRestart(cfg.Value);
                }

                return Results.Ok(new
                {
                    ok = true,
                    message = req.Rebuild ? "已保存并重建 Modbus / IEC 61850 协议层" : "已保存（重启后生效）",
                    devices = Iec61850LayerManager.Instance.GetSnapshot()
                });
            });

            g.MapPost("/link/{target}/{state}", (string target, string state) =>
            {
                if (!Display.EssCommand.TryParseLinkState(state, out var enable, out var msg))
                    return Results.BadRequest(new { success = false, message = msg });

                string? name = ResolveEmuName(target);
                if (name == null)
                    return Results.BadRequest(new { success = false, message = "请使用 iec61850-pcsN 或 simEmuN" });

                bool ok = Iec61850LayerManager.Instance.TrySetOnline(name, enable);
                if (!ok)
                    return Results.Ok(new { success = false, message = $"找不到 {name} 的 IEC 61850 IED" });

                return Results.Ok(new
                {
                    success = true,
                    message = enable ? $"{name} IEC 61850 已上线" : $"{name} IEC 61850 已离线"
                });
            });

            return app;
        }

        internal static string? ResolveEmuName(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
                return null;
            if (target.StartsWith("simEmu", StringComparison.OrdinalIgnoreCase))
                return target;
            if (target.StartsWith("iec61850-pcs", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(target.AsSpan("iec61850-pcs".Length), out var n)
                && n >= 1)
            {
                return $"simEmu{n}";
            }

            if (target.StartsWith("pcs", StringComparison.OrdinalIgnoreCase)
                && target.EndsWith("-iec61850", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(target.AsSpan(3, target.Length - 3 - "-iec61850".Length), out n)
                && n >= 1)
            {
                return $"simEmu{n}";
            }

            return null;
        }
    }
}
