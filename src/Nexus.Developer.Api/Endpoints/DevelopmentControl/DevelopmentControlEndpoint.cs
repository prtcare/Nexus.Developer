using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nexus.Developer.Application.DevelopmentControl.Commands.CompleteDevelopmentControlWorkItem;
using Nexus.Developer.Application.DevelopmentControl.Commands.ReleaseDevelopmentControlReservation;
using Nexus.Developer.Application.DevelopmentControl.Commands.ReserveDevelopmentControlWorkItem;
using Nexus.Developer.Application.DevelopmentControl.Queries.GetActiveDevelopmentChanges;
using Nexus.Developer.Application.DevelopmentControl.Queries.GetDevelopmentControlNode;
using Nexus.Developer.Application.DevelopmentControl.Queries.GetDevelopmentControlState;
using Nexus.Developer.Application.DevelopmentControl.Queries.RunDevelopmentControlPreflight;
using Nexus.Developer.Core.DevelopmentControl;
using NodeAtomicWriteResult = Nexus.Developer.Core.DevelopmentControl.AtomicWriteResult<Nexus.Developer.Core.DevelopmentControl.Node>;

namespace Nexus.Developer.Api.Endpoints.DevelopmentControl;

// SP1-M05: minimal-API surface for the governed Development Control plane. Reads map to
// 200 (null node/state -> 404). Governed mutations run through the guarded store's
// atomic-write entry and translate the DevelopmentControlConcurrencyOutcome to an HTTP
// status: Success 200, ConcurrencyConflict 409, NotFound 404, ValidationFailure 422,
// InvalidRequest 400, LockTimeout/IoFailure 503 -- never an unhandled 500.
public static class DevelopmentControlEndpoint
{
    private static readonly string[] ValidActorTypes = Enum.GetNames<ActorType>();

    public static void MapDevelopmentControlEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/development-control/state",
            async ([FromServices] GetDevelopmentControlStateHandler handler,
                CancellationToken cancellationToken) =>
            {
                var state = await handler.HandleAsync(
                    new GetDevelopmentControlStateQuery(), cancellationToken);

                if (state is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(ToStateResponse(state));
            });

        app.MapGet("/api/v1/development-control/active-changes",
            async ([FromServices] GetActiveDevelopmentChangesHandler handler,
                CancellationToken cancellationToken) =>
            {
                var changes = await handler.HandleAsync(
                    new GetActiveDevelopmentChangesQuery(), cancellationToken);

                return Results.Ok(changes.Select(ToActiveChangeResponse));
            });

        app.MapGet("/api/v1/development-control/nodes/{nodeId}",
            async (string nodeId,
                [FromServices] GetDevelopmentControlNodeHandler handler,
                CancellationToken cancellationToken) =>
            {
                NodeId parsed;
                try
                {
                    parsed = new NodeId(nodeId);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }

                var node = await handler.HandleAsync(
                    new GetDevelopmentControlNodeQuery(parsed.Value), cancellationToken);

                return node is null
                    ? Results.NotFound()
                    : Results.Ok(ToNodeResponse(node));
            });

        app.MapPost("/api/v1/development-control/preflight",
            async ([FromBody] RunPreflightRequest request,
                [FromServices] RunDevelopmentControlPreflightHandler handler,
                CancellationToken cancellationToken) =>
            {
                var declaration = new PreflightDeclaration(
                    request.ChangeId,
                    new NodeId(request.RoadmapNodeId),
                    request.Repositories ?? Array.Empty<string>(),
                    request.Projects ?? Array.Empty<string>(),
                    request.FilesGlobs ?? Array.Empty<string>(),
                    request.SchemaOrDbContextMutation,
                    request.ContractsApis ?? Array.Empty<string>(),
                    request.Dependencies ?? Array.Empty<string>(),
                    request.Risk ?? string.Empty,
                    request.Worker ?? string.Empty,
                    request.Branch,
                    request.SiblingWorktree);

                var result = await handler.HandleAsync(
                    new RunDevelopmentControlPreflightQuery(declaration), cancellationToken);

                return Results.Ok(new DevelopmentControlPreflightResponse(
                    result.Verdict.ToString(),
                    result.Detail,
                    result.Findings));
            });

        app.MapPost("/api/v1/development-control/nodes/{nodeId}/reserve",
            async (string nodeId,
                [FromBody] ReserveWorkItemRequest request,
                [FromServices] ReserveDevelopmentControlWorkItemHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!TryParseActorType(request.ActorType, out var actorType))
                {
                    return Results.BadRequest(new
                    {
                        error = $"ActorType '{request.ActorType}' is not a valid ActorType. Valid values: {string.Join(", ", ValidActorTypes)}."
                    });
                }

                var result = await handler.HandleAsync(
                    new ReserveDevelopmentControlWorkItemCommand(
                        nodeId,
                        request.ChangeId,
                        request.ActorName,
                        request.ActorId,
                        actorType,
                        Source: "Nexus.Developer.Api",
                        request.Branch,
                        request.Worktree,
                        request.Reason,
                        request.SessionId),
                    cancellationToken);

                return ToMutationResult(result);
            });

        app.MapPost("/api/v1/development-control/nodes/{nodeId}/release-reservation",
            async (string nodeId,
                [FromBody] ReleaseReservationRequest request,
                [FromServices] ReleaseDevelopmentControlReservationHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!TryParseActorType(request.ActorType, out var actorType))
                {
                    return Results.BadRequest(new
                    {
                        error = $"ActorType '{request.ActorType}' is not a valid ActorType. Valid values: {string.Join(", ", ValidActorTypes)}."
                    });
                }

                var result = await handler.HandleAsync(
                    new ReleaseDevelopmentControlReservationCommand(
                        nodeId,
                        request.ChangeId,
                        request.ActorName,
                        request.ActorId,
                        actorType,
                        Source: "Nexus.Developer.Api",
                        request.Reason,
                        request.SessionId),
                    cancellationToken);

                return ToMutationResult(result);
            });

        app.MapPost("/api/v1/development-control/nodes/{nodeId}/complete",
            async (string nodeId,
                [FromBody] CompleteWorkItemRequest request,
                [FromServices] CompleteDevelopmentControlWorkItemHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!TryParseActorType(request.ActorType, out var actorType))
                {
                    return Results.BadRequest(new
                    {
                        error = $"ActorType '{request.ActorType}' is not a valid ActorType. Valid values: {string.Join(", ", ValidActorTypes)}."
                    });
                }

                var result = await handler.HandleAsync(
                    new CompleteDevelopmentControlWorkItemCommand(
                        nodeId,
                        request.ChangeId,
                        request.ActorName,
                        request.ResultOrEvidence,
                        request.ActorId,
                        actorType,
                        Source: "Nexus.Developer.Api",
                        request.Reason,
                        request.SessionId),
                    cancellationToken);

                return ToMutationResult(result);
            });
    }

    // ------------------------------------------------------------------ outcome mapping
    // AtomicWriteResult -> IResult. Every terminal outcome is mapped to an HTTP status;
    // nothing here can fall through to an unhandled 500.

    private static IResult ToMutationResult(NodeAtomicWriteResult result)
    {
        switch (result.Outcome)
        {
            case DevelopmentControlConcurrencyOutcome.Success:
                return Results.Ok(new DevelopmentControlMutationResponse(
                    true,
                    result.Outcome.ToString(),
                    result.ActivityLogEntryId,
                    result.Value is null ? null : ToNodeResponse(result.Value)));

            case DevelopmentControlConcurrencyOutcome.ConcurrencyConflict:
                return Results.Conflict(new
                {
                    error = "The node changed since it was read; the governed write was not applied. Re-read the node and retry.",
                    outcome = result.Outcome.ToString(),
                    validationErrors = result.ValidationErrors
                });

            case DevelopmentControlConcurrencyOutcome.NotFound:
                return Results.NotFound(new
                {
                    error = ErrorMessage(result),
                    outcome = result.Outcome.ToString()
                });

            case DevelopmentControlConcurrencyOutcome.ValidationFailure:
                return Results.UnprocessableEntity(new
                {
                    error = ErrorMessage(result),
                    outcome = result.Outcome.ToString(),
                    validationErrors = result.ValidationErrors
                });

            case DevelopmentControlConcurrencyOutcome.InvalidRequest:
                return Results.BadRequest(new
                {
                    error = ErrorMessage(result),
                    outcome = result.Outcome.ToString(),
                    validationErrors = result.ValidationErrors
                });

            case DevelopmentControlConcurrencyOutcome.LockTimeout:
            case DevelopmentControlConcurrencyOutcome.IoFailure:
                return Results.Json(new
                {
                    error = ErrorMessage(result),
                    outcome = result.Outcome.ToString(),
                    lockWaitMilliseconds = result.LockWait?.TotalMilliseconds
                }, statusCode: StatusCodes.Status503ServiceUnavailable);

            default:
                // Defensive: a future outcome value must still map to a controlled failure.
                return Results.Json(new
                {
                    error = $"Unclassified Development Control outcome '{result.Outcome}'.",
                    outcome = result.Outcome.ToString()
                }, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static string ErrorMessage(NodeAtomicWriteResult result)
        => result.ValidationErrors.Count > 0
            ? string.Join(" ", result.ValidationErrors)
            : "The Development Control write could not be completed.";

    private static bool TryParseActorType(string? value, out ActorType actorType)
    {
        if (Enum.TryParse<ActorType>(value, ignoreCase: true, out actorType) &&
            Enum.IsDefined(actorType))
        {
            return true;
        }

        actorType = default;
        return false;
    }

    // ------------------------------------------------------------------ projections

    private static GetDevelopmentControlStateResponse ToStateResponse(ControlState state)
        => new(
            state.WorkbookVersion,
            state.RoadmapVersion,
            state.RootNodeId?.Value,
            state.CurrentNodeCount,
            state.MilestoneCount,
            state.WorkItemCount,
            state.BlockedNodeCount,
            state.ActiveChangeCount,
            state.OpenAuditFindingCount,
            state.LastUpdatedAt);

    private static DevelopmentControlNodeResponse ToNodeResponse(Node node)
        => new(
            node.NodeId.Value,
            node.ParentId?.Value,
            node.NodeType.ToString(),
            node.SortKey,
            node.Path,
            node.Layer,
            node.Phase,
            node.Name,
            node.Status.ToString(),
            node.Dependencies.Select(d => d.Value).ToArray(),
            node.BreakdownComplete,
            node.ReportedProgress,
            node.Owner,
            node.Risk,
            node.RowVersion,
            node.UpdatedAt);

    private static DevelopmentControlActiveChangeResponse ToActiveChangeResponse(ActiveChange change)
        => new(
            change.ChangeId,
            change.NodeId,
            change.Summary,
            change.Worker,
            change.Status,
            change.Branch,
            change.Worktree,
            change.StartedAt,
            change.CompletedAt,
            change.ResultOrEvidence,
            change.AffectedNodes,
            change.ChangeType,
            change.ValidationResult);
}
