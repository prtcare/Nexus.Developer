using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nexus.Developer.Application.DevelopmentRuns;
using Nexus.Developer.Application.DevelopmentRuns.Commands.CancelDevelopmentRun;
using Nexus.Developer.Application.DevelopmentRuns.Commands.CreateDevelopmentRun;
using Nexus.Developer.Application.DevelopmentRuns.Commands.FailDevelopmentRun;
using Nexus.Developer.Application.DevelopmentRuns.Commands.StartDevelopmentRun;
using Nexus.Developer.Application.DevelopmentRuns.Commands.SucceedDevelopmentRun;
using Nexus.Developer.Application.DevelopmentRuns.Queries.GetDevelopmentRun;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.DevelopmentRuns;

namespace Nexus.Developer.Api.Endpoints.DevelopmentRuns;

public static class DevelopmentRunEndpoint
{
    private static readonly string[] ValidTargetTypes = Enum.GetNames<DevelopmentRunTargetType>();

    public static void MapDevelopmentRunEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/v1/development-runs",
            async (
                [FromBody] CreateDevelopmentRunRequest request,
                [FromServices] CreateDevelopmentRunHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!TryParseTargetType(request.TargetType, out var targetType))
                {
                    return Results.BadRequest(
                        new { error = $"TargetType '{request.TargetType}' is not a valid DevelopmentRunTargetType. Valid values: {string.Join(", ", ValidTargetTypes)}." });
                }

                try
                {
                    var result = await handler.HandleAsync(
                        new CreateDevelopmentRunCommand(
                            targetType,
                            request.TargetId,
                            request.CreatedByUserId),
                        cancellationToken);

                    return Results.Ok(
                        new CreateDevelopmentRunResponse(
                            result.DevelopmentRunId.Value,
                            result.TargetType.ToString(),
                            result.TargetId,
                            result.Reference));
                }
                catch (DevelopmentRunTargetNotFoundException ex)
                {
                    // The TargetId is invalid input on this create endpoint (a
                    // foreign reference, not the resource being created), so
                    // 400 -- mirroring FeatureEndpoint's SubprojectNotFoundException
                    // handling, never an unhandled 500.
                    return Results.BadRequest(new { error = ex.Message });
                }
            });

        app.MapGet(
            "/api/v1/development-runs/{id:guid}",
            async (
                Guid id,
                [FromServices] GetDevelopmentRunHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new GetDevelopmentRunQuery(new DevelopmentRunId(id)),
                    cancellationToken);

                if (result is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(
                    new GetDevelopmentRunResponse(
                        result.DevelopmentRunId.Value,
                        result.TargetType.ToString(),
                        result.TargetId,
                        (int)result.Status,
                        result.CreatedByUserId,
                        result.CreatedAt,
                        result.Reference,
                        result.WorkerId,
                        result.WorkerType,
                        result.StartedAt,
                        result.CompletedAt,
                        result.ResultSummary));
            });

        // SP1-M05 lifecycle surface: Start/Cancel/Succeed/Fail are governed transitions
        // over the SP1-M03 Core aggregate. Each returns the resulting lifecycle read
        // model; an unknown run is 404 and an illegal transition for the run's current
        // status is 409 -- never an unhandled 500. Input validation (blank worker id /
        // blank required summary) is 400.
        app.MapPost(
            "/api/v1/development-runs/{id:guid}/start",
            async (
                Guid id,
                [FromBody] StartDevelopmentRunRequest request,
                [FromServices] StartDevelopmentRunHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.WorkerId) ||
                    string.IsNullOrWhiteSpace(request.WorkerType))
                {
                    return Results.BadRequest(
                        new { error = "WorkerId and WorkerType are required to start a development run." });
                }

                return await RunLifecycleAsync(
                    () => handler.HandleAsync(
                        new StartDevelopmentRunCommand(
                            new DevelopmentRunId(id),
                            request.WorkerId,
                            request.WorkerType),
                        cancellationToken));
            });

        app.MapPost(
            "/api/v1/development-runs/{id:guid}/cancel",
            async (
                Guid id,
                [FromBody] CancelDevelopmentRunRequest request,
                [FromServices] CancelDevelopmentRunHandler handler,
                CancellationToken cancellationToken) =>
            {
                return await RunLifecycleAsync(
                    () => handler.HandleAsync(
                        new CancelDevelopmentRunCommand(
                            new DevelopmentRunId(id),
                            request.Summary),
                        cancellationToken));
            });

        app.MapPost(
            "/api/v1/development-runs/{id:guid}/succeed",
            async (
                Guid id,
                [FromBody] SucceedDevelopmentRunRequest request,
                [FromServices] SucceedDevelopmentRunHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Summary))
                {
                    return Results.BadRequest(
                        new { error = "A non-blank Summary is required to complete a development run." });
                }

                return await RunLifecycleAsync(
                    () => handler.HandleAsync(
                        new SucceedDevelopmentRunCommand(
                            new DevelopmentRunId(id),
                            request.Summary),
                        cancellationToken));
            });

        app.MapPost(
            "/api/v1/development-runs/{id:guid}/fail",
            async (
                Guid id,
                [FromBody] FailDevelopmentRunRequest request,
                [FromServices] FailDevelopmentRunHandler handler,
                CancellationToken cancellationToken) =>
            {
                return await RunLifecycleAsync(
                    () => handler.HandleAsync(
                        new FailDevelopmentRunCommand(
                            new DevelopmentRunId(id),
                            request.Summary),
                        cancellationToken));
            });
    }

    // Shared error translation for the four lifecycle transitions: 200 with the
    // resulting read model on success, 404 for an unknown run, 409 for an illegal
    // transition given the run's current status.
    private static async Task<IResult> RunLifecycleAsync(
        Func<Task<DevelopmentRunLifecycleResult>> operation)
    {
        try
        {
            var result = await operation();
            return Results.Ok(
                new DevelopmentRunLifecycleResponse(
                    result.DevelopmentRunId.Value,
                    (int)result.Status,
                    result.Reference,
                    result.WorkerId,
                    result.WorkerType,
                    result.StartedAt,
                    result.CompletedAt,
                    result.ResultSummary));
        }
        catch (DevelopmentRunNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (DevelopmentRunStateException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static bool TryParseTargetType(
        string? value,
        out DevelopmentRunTargetType targetType)
    {
        if (Enum.TryParse<DevelopmentRunTargetType>(value, ignoreCase: true, out targetType) &&
            Enum.IsDefined(targetType))
        {
            return true;
        }

        targetType = default;
        return false;
    }
}
