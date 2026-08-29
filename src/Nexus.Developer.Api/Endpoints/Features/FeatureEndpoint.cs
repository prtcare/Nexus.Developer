using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nexus.Developer.Application.Features.Commands.CreateFeature;
using Nexus.Developer.Application.Features.Queries.GetFeature;
using Nexus.Developer.Application.Features.Queries.ListFeaturesBySubproject;
using Nexus.Developer.Application.Scope;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Api.Endpoints.Features;

public static class FeatureEndpoint
{
    public static void MapFeatureEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/v1/features",
            async (
                [FromBody] CreateFeatureRequest request,
                [FromServices] CreateFeatureHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Title))
                {
                    return Results.BadRequest(new { error = "Title is required." });
                }

                try
                {
                    var result = await handler.HandleAsync(
                        new CreateFeatureCommand(
                            new SubprojectId(request.SubprojectId),
                            request.Title,
                            request.Description ?? string.Empty,
                            request.CreatedByUserId),
                        cancellationToken);

                    return Results.Ok(
                        new CreateFeatureResponse(
                            result.FeatureId.Value,
                            result.Title,
                            result.Reference));
                }
                catch (SubprojectNotFoundException ex)
                {
                    // The SubprojectId is invalid input on this create endpoint
                    // (a foreign reference, not the resource being created), so
                    // 400 -- matching the Title-required check above, never an
                    // unhandled 500.
                    return Results.BadRequest(new { error = ex.Message });
                }
            });

        app.MapGet(
            "/api/v1/features/{id:guid}",
            async (
                Guid id,
                [FromServices] GetFeatureHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new GetFeatureQuery(new FeatureId(id)),
                    cancellationToken);

                if (result is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(
                    new GetFeatureResponse(
                        result.FeatureId.Value,
                        result.SubprojectId.Value,
                        result.Title,
                        result.Description,
                        (int)result.Status,
                        result.CreatedByUserId,
                        result.CreatedAt,
                        result.Reference));
            });

        app.MapGet(
            "/api/v1/subprojects/{subprojectId:guid}/features",
            async (
                Guid subprojectId,
                [FromServices] ListFeaturesBySubprojectHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new ListFeaturesBySubprojectQuery(new SubprojectId(subprojectId)),
                    cancellationToken);

                var features = result.Features
                    .Select(feature => new GetFeatureResponse(
                        feature.FeatureId.Value,
                        feature.SubprojectId.Value,
                        feature.Title,
                        feature.Description,
                        (int)feature.Status,
                        feature.CreatedByUserId,
                        feature.CreatedAt,
                        feature.Reference))
                    .ToList();

                return Results.Ok(features);
            });
    }
}
