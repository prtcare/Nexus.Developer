using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nexus.Developer.Application.DevelopmentRuns;
using Nexus.Developer.Application.DevelopmentRuns.Commands.CreateDevelopmentRun;
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
                        result.Reference));
            });
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
