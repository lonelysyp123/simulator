using EssSimulator.Configuration;
using EssSimulator.Core;
using EssSimulator.Display;
using EssSimulator.Web.DroopSlices;
using EssSimulator.Web.Topology;
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

            app.MapGet("/api/mainline", (TopologyStore topologyStore) =>
                Results.Ok(MainLineEnricher.Build(topologyStore)));

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

            // BMS 簇告警门限：点表元数据 + 当前工程值（下发仍走 POST /api/command → dpc）
            app.MapGet("/api/bms/{unit:int}/rack-thresholds", (int unit, int? rack) =>
            {
                int unitCount = Math.Max(1, GuiSimDataAccess.GetEssUnitCount());
                int unitNum = Math.Clamp(unit, 1, unitCount);
                int rackIndex = Math.Max(0, rack ?? 0);
                var snap = RackThresholdSnapshotReader.Read(unitNum, rackIndex);
                return snap == null
                    ? Results.NotFound(new { message = $"未找到设备 simBms{unitNum} 或点表未加载" })
                    : Results.Ok(snap);
            });

            // 设备告警/故障位：绿=未触发，红=已触发
            app.MapGet("/api/alarms", () => Results.Ok(AlarmSnapshotReader.ReadAll()));

            app.MapGet("/api/alarms/bms/{unit:int}", (int unit, int? rack) =>
            {
                int unitCount = Math.Max(1, GuiSimDataAccess.GetEssUnitCount());
                int unitNum = Math.Clamp(unit, 1, unitCount);
                return Results.Ok(AlarmSnapshotReader.ReadBmsUnit(unitNum, rack));
            });

            app.MapGet("/api/connections", () => Results.Ok(ConnectionSnapshotReader.Read()));

            app.MapGet("/api/alert", () => Results.Ok(FatalSystemAlert.GetSnapshot()));

            app.MapGet("/api/config", (IOptions<SimulatorConfig> simCfg, IOptions<WebConfig> webCfg, IOptions<EditionConfig> editionCfg) =>
            {
                var w = webCfg.Value;
                var e = editionCfg.Value;
                var devices = simCfg.Value.Devices ?? new List<EssUnitConfig>();
                // 按舱号（channel / compartment，1-based）展开各 BMS 拓扑，供 3D 详情等使用
                var bmsTopology = new List<object>();
                for (int ui = 0; ui < devices.Count; ui++)
                {
                    var bmsList = devices[ui].Bms ?? new List<BmsDeviceConfig>();
                    for (int bi = 0; bi < bmsList.Count; bi++)
                    {
                        var b = bmsList[bi];
                        int channelIndex0 = ui * 2 + bi;
                        bmsTopology.Add(new
                        {
                            unitIndex = ui,
                            slotInUnit = bi,
                            channelIndex = channelIndex0,
                            compartmentNumber = channelIndex0 + 1,
                            name = b.Name,
                            clusterCount = Math.Max(1, b.ClusterCount),
                            packCount = Math.Max(1, b.PackCount),
                            cellSeriesCount = Math.Max(1, b.CellSeriesCount),
                            cellParallelCount = Math.Max(1, b.CellParallelCount)
                        });
                    }
                }

                return Results.Ok(new
                {
                    simulator = new
                    {
                        simCfg.Value.Runtime,
                        simCfg.Value.Protocol,
                        unitCount = devices.Count,
                        channelCount = simCfg.Value.UnitCount,
                        bmsTopology
                    },
                    edition = new
                    {
                        e.Name,
                        e.LockTopology,
                        e.MaxEssUnits,
                        e.AllowDroopSlices,
                        e.AllowMainline3d,
                        e.AllowTopologyEditor,
                        e.IsCommunity
                    },
                    web = new
                    {
                        w.HttpPort,
                        w.HttpBaseUrl,
                        w.StaticFiles,
                        w.CorsOrigins,
                        w.SnapshotIntervalMs,
                        w.DroopSliceCaptureEnabled,
                        w.DroopSliceMaxCount,
                        w.ApiKeyEnabled,
                        apiKeyConfigured = !string.IsNullOrWhiteSpace(w.ApiKey)
                    }
                });
            });

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
                        controlMaps = srv?.ControlMaps,
                        rackControlMaps = srv?.RackControlMaps
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

            // 白盒切片（社区版 AllowDroopSlices=false 时拒绝）
            app.MapGet("/api/droop-slices/status", (DroopSliceStore store) =>
                store.FeatureAllowed
                    ? Results.Ok(store.GetStatus())
                    : Results.Json(new { message = "当前产品档位不包含白盒切片功能" }, statusCode: StatusCodes.Status403Forbidden));

            app.MapGet("/api/droop-slices", (DroopSliceStore store, int? limit, int? offset) =>
                store.FeatureAllowed
                    ? Results.Ok(store.List(limit ?? 100, offset ?? 0))
                    : Results.Json(new { message = "当前产品档位不包含白盒切片功能" }, statusCode: StatusCodes.Status403Forbidden));

            app.MapGet("/api/droop-slices/{id:guid}", (Guid id, DroopSliceStore store) =>
            {
                if (!store.FeatureAllowed)
                    return Results.Json(new { message = "当前产品档位不包含白盒切片功能" }, statusCode: StatusCodes.Status403Forbidden);
                var slice = store.Get(id);
                return slice == null ? Results.NotFound(new { message = "切片不存在" }) : Results.Ok(slice);
            });

            app.MapPost("/api/droop-slices/clear", (DroopSliceStore store) =>
            {
                if (!store.FeatureAllowed)
                    return Results.Json(new { message = "当前产品档位不包含白盒切片功能" }, statusCode: StatusCodes.Status403Forbidden);
                store.Clear();
                return Results.Ok(store.GetStatus());
            });

            app.MapPost("/api/droop-slices/config", (DroopSliceConfigRequest req, DroopSliceStore store) =>
            {
                if (!store.FeatureAllowed)
                    return Results.Json(new { message = "当前产品档位不包含白盒切片功能" }, statusCode: StatusCodes.Status403Forbidden);
                if (req.Enabled.HasValue)
                    store.Enabled = req.Enabled.Value;
                if (req.MaxCount.HasValue)
                    store.MaxCount = req.MaxCount.Value;
                return Results.Ok(store.GetStatus());
            });

            // 组态编辑：模板 / 工程 / 连线校验 / 设备库
            app.MapTopologyEndpoints();
            // 系统配置：工程模式 / 应用到仿真
            app.MapSystemConfigEndpoints();

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
