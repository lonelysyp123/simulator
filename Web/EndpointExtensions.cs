using EssSimulator.Configuration;
using EssSimulator.Core;
using EssSimulator.Display;
using EssSimulator.Web.DroopSlices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EssSimulator.Web
{
    public static class EndpointExtensions
    {
        public static WebApplication MapSimulatorEndpoints(this WebApplication app)
        {
            app.MapGet("/api/health", () => Results.Ok(new
            {
                status = "ok",
                time = DateTime.Now,
                ready = IsSimulatorReady()
            }));

            app.MapGet("/api/mainline", () => Results.Ok(MainLineEnricher.Build()));

            app.MapGet("/api/battery/{unit}", (int unit) =>
            {
                int unitCount = Math.Max(1, GuiSimDataAccess.GetEssUnitCount());
                int idx = Math.Clamp(unit - 1, 0, unitCount - 1);
                return Results.Ok(BatterySnapshotReader.ReadOverview(idx));
            });

            app.MapGet("/api/cells/{unit}/{cluster}", (int unit, int cluster) =>
            {
                int unitCount = Math.Max(1, GuiSimDataAccess.GetEssUnitCount());
                int idx = Math.Clamp(unit - 1, 0, unitCount - 1);
                return Results.Ok(BatterySnapshotReader.ReadCells(idx, cluster - 1));
            });

            app.MapGet("/api/connections", () => Results.Ok(ConnectionSnapshotReader.Read()));

            app.MapGet("/api/alert", () => Results.Ok(FatalSystemAlert.GetSnapshot()));

            app.MapGet("/api/config", (IOptions<SimulatorConfig> simCfg, IOptions<WebConfig> webCfg) =>
                Results.Ok(new
                {
                    simulator = new
                    {
                        simCfg.Value.Runtime,
                        simCfg.Value.Protocol,
                        unitCount = simCfg.Value.Devices?.Count ?? 0,
                        channelCount = simCfg.Value.UnitCount
                    },
                    web = webCfg.Value
                }));

            app.MapGet("/api/protocol", (IOptions<SimulatorConfig> cfg) =>
            {
                var c = cfg.Value;
                var info = new
                {
                    em = new { name = "simEm", port = c.Protocol.EmModbusPort },
                    bms = Enumerable.Range(0, Math.Max(1, c.UnitCount))
                        .Select(i => new { name = $"simBms{i + 1}", port = c.Protocol.BaseBmsModbusPort + i * c.Protocol.BmsPortStep })
                        .ToArray(),
                    emu = Enumerable.Range(0, Math.Max(1, c.Devices?.Count ?? 1))
                        .Select(i => new { name = $"simEmu{i + 1}", port = c.Protocol.BaseEmuModbusPort + i * c.Protocol.EmuPortStep })
                        .ToArray()
                };
                return Results.Ok(info);
            });

            app.MapGet("/api/autotest", () =>
            {
                if (!DpcAutoTestCommand.TryListTests(out var tests, out var error))
                    return Results.Ok(new { ok = false, error, tests = Array.Empty<object>() });
                return Results.Ok(new { ok = true, tests });
            });

            app.MapGet("/api/pointmaps", () =>
            {
                var store = SimulatorHost.Instance;
                var devices = new List<object>();
                int bms = 1;
                while (store.Contains($"simBms{bms}"))
                {
                    var srv = store.Get<ModbusSimServer>($"simBms{bms}");
                    devices.Add(new
                    {
                        device = $"simBms{bms}",
                        dataMaps = srv?.DataMaps,
                        controlMaps = srv?.ControlMaps
                    });
                    bms++;
                }
                int emu = 1;
                while (store.Contains($"simEmu{emu}"))
                {
                    var srv = store.Get<ModbusSimServer>($"simEmu{emu}");
                    devices.Add(new
                    {
                        device = $"simEmu{emu}",
                        dataMaps = srv?.DataMaps,
                        controlMaps = srv?.ControlMaps
                    });
                    emu++;
                }
                if (store.Contains("simEm"))
                {
                    var srv = store.Get<ModbusSimServer>("simEm");
                    devices.Add(new
                    {
                        device = "simEm",
                        dataMaps = srv?.DataMaps,
                        controlMaps = srv?.ControlMaps
                    });
                }
                return Results.Ok(devices);
            });

            // 断路器控制：经 CommandProcessor 执行指令，不直接改模型（与原 TUI 一致）
            app.MapPost("/api/breaker/main/{closed:bool}", (bool closed, WebCommandExecutor exec) =>
            {
                var result = exec.Execute($"breaker set {closed.ToString().ToLowerInvariant()}");
                return FromCommandResult(result);
            });

            // 单元断路器：经 dpc 写 EMU 控制点 yx0（高压断路器开合），unit 从 1 起
            app.MapPost("/api/breaker/unit/{unit:int}/{closed:bool}", (int unit, bool closed, WebCommandExecutor exec) =>
            {
                var result = exec.Execute($"dpc simEmu{unit}.yx0 set {(closed ? 1 : 0)}");
                return FromCommandResult(result);
            });

            // 通用命令执行：POST /api/command  body: { "input": "esscmd link status" }
            app.MapPost("/api/command", (CommandRequest req, WebCommandExecutor exec) =>
            {
                if (string.IsNullOrWhiteSpace(req.Input))
                    return Results.BadRequest(CommandResult.Fail("input 不能为空"));
                return Results.Ok(exec.Execute(req.Input));
            });

            // 链路控制：POST /api/link/{target}/{state}  （target: em|bms1|pcs1  state: on|off）
            app.MapPost("/api/link/{target}/{state}", (string target, string state) =>
            {
                if (!EssCommand.TryParseLinkState(state, out var enable, out var msg))
                    return Results.BadRequest(CommandResult.Fail(msg));
                if (!EssCommand.TryResolveProtocolServer(target, out var server, out var name, out var detail))
                    return Results.BadRequest(CommandResult.Fail(detail));

                bool ok = server!.SetOnline(enable);
                if (!ok)
                    return Results.Ok(CommandResult.Fail($"操作失败: {name} 未能{(enable ? "恢复" : "关闭")} Modbus 服务"));

                var listenInfo = SimServer.serverListenInfo.TryGetValue(name, out var info) ? info : name;
                return Results.Ok(CommandResult.Ok(enable
                    ? $"{target} -> {name} 已上线（{listenInfo}）{detail}"
                    : $"{target} -> {name} 已离线{detail}"));
            });

            // dpctest 异步执行：POST /api/dpctest/{name}  → 进度通过 SignalR cmdprogress 频道推送
            app.MapPost("/api/dpctest/{name}", async (string name, WebCommandExecutor exec, CancellationToken ct) =>
            {
                var result = await exec.ExecuteDpcTestAsync(name, ct);
                return Results.Ok(result);
            });

            // 下垂白盒切片
            app.MapGet("/api/droop-slices/status", (DroopSliceStore store) => Results.Ok(store.GetStatus()));

            app.MapGet("/api/droop-slices", (DroopSliceStore store, int? limit, int? offset) =>
                Results.Ok(store.List(limit ?? 100, offset ?? 0)));

            app.MapGet("/api/droop-slices/{id:guid}", (Guid id, DroopSliceStore store) =>
            {
                var slice = store.Get(id);
                return slice == null ? Results.NotFound(new { message = "切片不存在" }) : Results.Ok(slice);
            });

            app.MapPost("/api/droop-slices/clear", (DroopSliceStore store) =>
            {
                store.Clear();
                return Results.Ok(store.GetStatus());
            });

            app.MapPost("/api/droop-slices/config", (DroopSliceConfigRequest req, DroopSliceStore store) =>
            {
                if (req.Enabled.HasValue)
                    store.Enabled = req.Enabled.Value;
                if (req.MaxCount.HasValue)
                    store.MaxCount = req.MaxCount.Value;
                return Results.Ok(store.GetStatus());
            });

            return app;
        }

        private static bool IsSimulatorReady()
        {
            var store = SimulatorHost.Instance;
            return store.Contains("ess") && store.Contains("simEm") && store.Contains("simBms1");
        }

        private static IResult FromCommandResult(CommandResult result) =>
            result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }

    public sealed class CommandRequest
    {
        public string Input { get; set; } = "";
    }

    public sealed class DroopSliceConfigRequest
    {
        public bool? Enabled { get; set; }
        public int? MaxCount { get; set; }
    }
}
