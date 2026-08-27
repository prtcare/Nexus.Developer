using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nexus.Developer.Application.Subtasks.Commands.CreateSubtask;
using Nexus.Developer.Application.Subtasks.Queries.GetSubtask;
using Nexus.Developer.Application.Subtasks.Queries.ListSubtasksByTask;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Api.Endpoints.Subtasks;

public static class SubtaskEndpoint
{
    public static void MapSubtaskEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/v1/subtasks",
            async (
                [FromBody] CreateSubtaskRequest request,
                [FromServices] CreateSubtaskHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Title))
                {
                    return Results.BadRequest(new { error = "Title is required." });
                }

                var result = await handler.HandleAsync(
                    new CreateSubtaskCommand(
                        new TaskId(request.TaskId),
                        request.Title,
                        request.Description ?? string.Empty,
                        request.CreatedByUserId),
                    cancellationToken);

                return Results.Ok(
                    new CreateSubtaskResponse(
                        result.SubtaskId.Value,
                        result.Title,
                        result.Reference));
            });

        app.MapGet(
            "/api/v1/subtasks/{id:guid}",
            async (
                Guid id,
                [FromServices] GetSubtaskHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new GetSubtaskQuery(new SubtaskId(id)),
                    cancellationToken);

                if (result is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(
                    new GetSubtaskResponse(
                        result.SubtaskId.Value,
                        result.TaskId.Value,
                        result.Title,
                        result.Description,
                        (int)result.Status,
                        result.CreatedByUserId,
                        result.CreatedAt,
                        result.Reference));
            });

        app.MapGet(
            "/api/v1/tasks/{taskId:guid}/subtasks",
            async (
                Guid taskId,
                [FromServices] ListSubtasksByTaskHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new ListSubtasksByTaskQuery(new TaskId(taskId)),
                    cancellationToken);

                var subtasks = result.Subtasks
                    .Select(subtask => new GetSubtaskResponse(
                        subtask.SubtaskId.Value,
                        subtask.TaskId.Value,
                        subtask.Title,
                        subtask.Description,
                        (int)subtask.Status,
                        subtask.CreatedByUserId,
                        subtask.CreatedAt,
                        subtask.Reference))
                    .ToList();

                return Results.Ok(subtasks);
            });
    }
}
