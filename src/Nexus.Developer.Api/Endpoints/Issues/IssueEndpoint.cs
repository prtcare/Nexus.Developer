using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nexus.Developer.Application.Issues.Commands.CreateIssue;
using Nexus.Developer.Application.Issues.Commands.LinkIssue;
using Nexus.Developer.Application.Issues.Queries.GetIssue;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Issues;

namespace Nexus.Developer.Api.Endpoints.Issues;

public static class IssueEndpoint
{
    public static void MapIssueEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/v1/issues",
            async (
                [FromBody] CreateIssueRequest request,
                [FromServices] CreateIssueHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Title))
                {
                    return Results.BadRequest(new { error = "Title is required." });
                }

                var result = await handler.HandleAsync(
                    new CreateIssueCommand(
                        request.Title,
                        request.Description ?? string.Empty,
                        request.CreatedByUserId),
                    cancellationToken);

                return Results.Ok(
                    new CreateIssueResponse(
                        result.IssueId.Value,
                        result.Title,
                        result.Reference));
            });

        app.MapGet(
            "/api/v1/issues/{id:guid}",
            async (
                Guid id,
                [FromServices] GetIssueHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new GetIssueQuery(new IssueId(id)),
                    cancellationToken);

                if (result is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(
                    new GetIssueResponse(
                        result.IssueId.Value,
                        result.Title,
                        result.Description,
                        (int)result.Status,
                        result.CreatedByUserId,
                        result.CreatedAt,
                        result.Reference));
            });

        app.MapPost(
            "/api/v1/issues/{id:guid}/links",
            async (
                Guid id,
                [FromBody] LinkIssueRequest request,
                [FromServices] LinkIssueHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!Enum.IsDefined(typeof(IssueLinkTargetType), request.TargetType))
                {
                    return Results.BadRequest(new { error = "TargetType is not a valid IssueLinkTargetType." });
                }

                var result = await handler.HandleAsync(
                    new LinkIssueCommand(
                        new IssueId(id),
                        (IssueLinkTargetType)request.TargetType,
                        request.TargetId,
                        request.LinkedByUserId),
                    cancellationToken);

                return Results.Ok(
                    new LinkIssueResponse(result.IssueLinkId.Value));
            });
    }
}
