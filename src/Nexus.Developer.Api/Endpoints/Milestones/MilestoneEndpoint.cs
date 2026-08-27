using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nexus.Developer.Application.Milestones.Commands.CreateMilestone;
using Nexus.Developer.Application.Milestones.Commands.LinkMilestone;
using Nexus.Developer.Application.Milestones.Queries.GetMilestone;
using Nexus.Developer.Application.Milestones.Queries.ListMilestonesBySubproject;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Milestones;

namespace Nexus.Developer.Api.Endpoints.Milestones;

public static class MilestoneEndpoint
{
    public static void MapMilestoneEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/v1/milestones",
            async (
                [FromBody] CreateMilestoneRequest request,
                [FromServices] CreateMilestoneHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Results.BadRequest(new { error = "Name is required." });
                }

                var result = await handler.HandleAsync(
                    new CreateMilestoneCommand(
                        new SubprojectId(request.SubprojectId),
                        request.Name,
                        request.Description ?? string.Empty,
                        request.TargetDate,
                        request.CreatedByUserId),
                    cancellationToken);

                return Results.Ok(
                    new CreateMilestoneResponse(
                        result.MilestoneId.Value,
                        result.Name,
                        result.Reference));
            });

        app.MapGet(
            "/api/v1/milestones/{id:guid}",
            async (
                Guid id,
                [FromServices] GetMilestoneHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new GetMilestoneQuery(new MilestoneId(id)),
                    cancellationToken);

                if (result is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(
                    new GetMilestoneResponse(
                        result.MilestoneId.Value,
                        result.SubprojectId.Value,
                        result.Name,
                        result.Description,
                        result.TargetDate,
                        (int)result.Status,
                        result.CreatedByUserId,
                        result.CreatedAt,
                        result.Reference));
            });

        app.MapGet(
            "/api/v1/subprojects/{subprojectId:guid}/milestones",
            async (
                Guid subprojectId,
                [FromServices] ListMilestonesBySubprojectHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new ListMilestonesBySubprojectQuery(new SubprojectId(subprojectId)),
                    cancellationToken);

                var milestones = result.Milestones
                    .Select(milestone => new GetMilestoneResponse(
                        milestone.MilestoneId.Value,
                        milestone.SubprojectId.Value,
                        milestone.Name,
                        milestone.Description,
                        milestone.TargetDate,
                        (int)milestone.Status,
                        milestone.CreatedByUserId,
                        milestone.CreatedAt,
                        milestone.Reference))
                    .ToList();

                return Results.Ok(milestones);
            });

        app.MapPost(
            "/api/v1/milestones/{id:guid}/links",
            async (
                Guid id,
                [FromBody] LinkMilestoneRequest request,
                [FromServices] LinkMilestoneHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!Enum.IsDefined(typeof(MilestoneLinkTargetType), request.TargetType))
                {
                    return Results.BadRequest(new { error = "TargetType is not a valid MilestoneLinkTargetType." });
                }

                var result = await handler.HandleAsync(
                    new LinkMilestoneCommand(
                        new MilestoneId(id),
                        (MilestoneLinkTargetType)request.TargetType,
                        request.TargetId,
                        request.LinkedByUserId),
                    cancellationToken);

                return Results.Ok(
                    new LinkMilestoneResponse(result.MilestoneLinkId.Value));
            });
    }
}
