using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nexus.Developer.Application.Tasks.Commands.CreateTask;
using Nexus.Developer.Application.Tasks.Queries.GetTask;
using Nexus.Developer.Application.Tasks.Queries.ListTasksByFeature;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Api.Endpoints.Tasks;

public static class TaskEndpoint
{
    public static void MapTaskEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/v1/tasks",
            async (
                [FromBody] CreateTaskRequest request,
                [FromServices] CreateTaskHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Title))
                {
                    return Results.BadRequest(new { error = "Title is required." });
                }

                var result = await handler.HandleAsync(
                    new CreateTaskCommand(
                        new FeatureId(request.FeatureId),
                        request.Title,
                        request.Description ?? string.Empty,
                        request.CreatedByUserId),
                    cancellationToken);

                return Results.Ok(
                    new CreateTaskResponse(
                        result.TaskId.Value,
                        result.Title,
                        result.Reference));
            });

        app.MapGet(
            "/api/v1/tasks/{id:guid}",
            async (
                Guid id,
                [FromServices] GetTaskHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new GetTaskQuery(new TaskId(id)),
                    cancellationToken);

                if (result is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(
                    new GetTaskResponse(
                        result.TaskId.Value,
                        result.FeatureId.Value,
                        result.Title,
                        result.Description,
                        (int)result.Status,
                        result.CreatedByUserId,
                        result.CreatedAt,
                        result.Reference,
                        result.MigratedFromWorkItemId));
            });

        app.MapGet(
            "/api/v1/features/{featureId:guid}/tasks",
            async (
                Guid featureId,
                [FromServices] ListTasksByFeatureHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new ListTasksByFeatureQuery(new FeatureId(featureId)),
                    cancellationToken);

                var tasks = result.Tasks
                    .Select(task => new GetTaskResponse(
                        task.TaskId.Value,
                        task.FeatureId.Value,
                        task.Title,
                        task.Description,
                        (int)task.Status,
                        task.CreatedByUserId,
                        task.CreatedAt,
                        task.Reference,
                        task.MigratedFromWorkItemId))
                    .ToList();

                return Results.Ok(tasks);
            });
    }
}
