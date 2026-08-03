using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EssSimulator.Web.Topology
{
    public static class TopologyEndpoints
    {
        public static IEndpointRouteBuilder MapTopologyEndpoints(this IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/api/topology");

            g.MapGet("/templates", () => Results.Ok(TopologyTemplates.All));

            g.MapGet("/templates/{id}", (string id) =>
            {
                var t = TopologyTemplates.Get(id);
                return t == null ? Results.NotFound(new { message = $"模板不存在: {id}" }) : Results.Ok(t);
            });

            g.MapGet("/project", (TopologyStore store) => Results.Ok(store.LoadProject()));

            g.MapPut("/project", (TopologyProject project, TopologyStore store) =>
            {
                if (project == null)
                    return Results.BadRequest(new { message = "工程体为空" });
                return Results.Ok(store.SaveProject(project));
            });

            g.MapPost("/connect", (ConnectRequest req, TopologyStore store) =>
            {
                if (req?.Project == null || req.Edge == null)
                    return Results.BadRequest(new ConnectResponse
                    {
                        Validation = new TopologyValidationResult
                        {
                            Ok = false,
                            Code = "BAD_REQUEST",
                            Message = "请求缺少 Project 或 Edge"
                        }
                    });

                var validation = TopologyValidator.TryConnect(req.Project, req.Edge);
                if (!validation.Ok)
                {
                    return Results.Ok(new ConnectResponse
                    {
                        Validation = validation,
                        Project = req.Project
                    });
                }

                var updated = TopologyValidator.ApplyConnect(req.Project, req.Edge);
                return Results.Ok(new ConnectResponse
                {
                    Validation = validation,
                    Project = updated
                });
            });

            g.MapPost("/disconnect", (DisconnectRequest req) =>
            {
                if (req?.Project == null || string.IsNullOrWhiteSpace(req.EdgeId))
                    return Results.BadRequest(new { message = "请求缺少 Project 或 EdgeId" });
                var updated = TopologyValidator.RemoveEdge(req.Project, req.EdgeId);
                return Results.Ok(updated);
            });

            g.MapGet("/library", (TopologyStore store) => Results.Ok(store.ListLibrary()));

            g.MapGet("/library/{id}", (string id, TopologyStore store) =>
            {
                var item = store.GetLibraryItem(id);
                return item == null ? Results.NotFound(new { message = "设备库条目不存在" }) : Results.Ok(item);
            });

            g.MapPut("/library", (TopologyLibraryItem item, TopologyStore store) =>
            {
                try
                {
                    return Results.Ok(store.SaveLibraryItem(item));
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            });

            g.MapDelete("/library/{id}", (string id, TopologyStore store) =>
                store.DeleteLibraryItem(id)
                    ? Results.Ok(new { ok = true })
                    : Results.NotFound(new { message = "设备库条目不存在" }));

            g.MapGet("/paths", (TopologyStore store) => Results.Ok(new
            {
                root = store.RootDirectory,
                project = Path.Combine(store.RootDirectory, "project.json"),
                library = Path.Combine(store.RootDirectory, "library")
            }));

            return app;
        }
    }

    public sealed class DisconnectRequest
    {
        public TopologyProject Project { get; set; } = new();
        public string EdgeId { get; set; } = "";
    }
}
