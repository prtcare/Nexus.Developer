using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nexus.Developer.Application.Dependencies;
using Nexus.Developer.Application.Dependencies.Commands.CreateWorkItemDependency;
using Nexus.Developer.Application.Dependencies.Queries.GetBlockingChain;
using Nexus.Developer.Application.Dependencies.Queries.ListDependenciesByNode;
using Nexus.Developer.Core.Dependencies;

namespace Nexus.Developer.Api.Endpoints.Dependencies;

public static class DependencyEndpoint
{
    private static readonly string[] ValidNodeTypes = Enum.GetNames<WorkItemDependencyNodeType>();
    private static readonly string[] ValidKinds = Enum.GetNames<WorkItemDependencyKind>();
    private static readonly string[] ValidRequiredStates = Enum.GetNames<WorkItemDependencyRequiredState>();

    public static void MapDependencyEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/v1/dependencies",
            async (
                [FromBody] CreateWorkItemDependencyRequest request,
                [FromServices] CreateWorkItemDependencyHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!TryParseNodeType(request.UpstreamType, out var upstreamType))
                {
                    return Results.BadRequest(
                        new { error = $"UpstreamType '{request.UpstreamType}' is not a valid WorkItemDependencyNodeType. Valid values: {string.Join(", ", ValidNodeTypes)}." });
                }

                if (!TryParseNodeType(request.DownstreamType, out var downstreamType))
                {
                    return Results.BadRequest(
                        new { error = $"DownstreamType '{request.DownstreamType}' is not a valid WorkItemDependencyNodeType. Valid values: {string.Join(", ", ValidNodeTypes)}." });
                }

                if (!TryParseKind(request.Kind, out var kind))
                {
                    return Results.BadRequest(
                        new { error = $"Kind '{request.Kind}' is not a valid WorkItemDependencyKind. Valid values: {string.Join(", ", ValidKinds)}." });
                }

                if (!TryParseRequiredState(request.RequiredState, out var requiredState))
                {
                    return Results.BadRequest(
                        new { error = $"RequiredState '{request.RequiredState}' is not a valid WorkItemDependencyRequiredState. Valid values: {string.Join(", ", ValidRequiredStates)}." });
                }

                try
                {
                    var result = await handler.HandleAsync(
                        new CreateWorkItemDependencyCommand(
                            upstreamType,
                            request.UpstreamId,
                            downstreamType,
                            request.DownstreamId,
                            kind,
                            requiredState,
                            request.CreatedByUserId,
                            request.Reason),
                        cancellationToken);

                    return Results.Ok(
                        new CreateWorkItemDependencyResponse(
                            result.WorkItemDependencyId.Value,
                            result.UpstreamType.ToString(),
                            result.UpstreamId,
                            result.DownstreamType.ToString(),
                            result.DownstreamId,
                            result.Kind.ToString(),
                            result.RequiredState?.ToString(),
                            result.CreatedByUserId,
                            result.CreatedAt,
                            result.Reason));
                }
                catch (WorkItemDependencyTargetNotFoundException ex)
                {
                    // Either endpoint is invalid caller-supplied input on this
                    // create call -- 400, never an unhandled 500 (WI-07-10.3.1
                    // corrected convention).
                    return Results.BadRequest(new { error = ex.Message });
                }
                catch (WorkItemDependencyCycleException ex)
                {
                    // A Blocking edge that would close a cycle is rejected at write
                    // time with the full cycle path named -- 400.
                    return Results.BadRequest(new { error = ex.Message });
                }
                catch (ArgumentException ex)
                {
                    // Domain shape validation surfaced through the create call
                    // (self-loop, RequiredState on a non-Blocking edge) -- 400.
                    return Results.BadRequest(new { error = ex.Message });
                }
            });

        app.MapGet(
            "/api/v1/dependencies/by-node/{nodeType}/{nodeId:guid}",
            async (
                string nodeType,
                Guid nodeId,
                [FromServices] ListDependenciesByNodeHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!TryParseNodeType(nodeType, out var parsedNodeType))
                {
                    return Results.BadRequest(
                        new { error = $"NodeType '{nodeType}' is not a valid WorkItemDependencyNodeType. Valid values: {string.Join(", ", ValidNodeTypes)}." });
                }

                var result = await handler.HandleAsync(
                    new ListDependenciesByNodeQuery(parsedNodeType, nodeId),
                    cancellationToken);

                var dependencies = result.Dependencies
                    .Select(dependency => new GetDependencyResponse(
                        dependency.WorkItemDependencyId.Value,
                        dependency.UpstreamType.ToString(),
                        dependency.UpstreamId,
                        dependency.DownstreamType.ToString(),
                        dependency.DownstreamId,
                        dependency.Kind.ToString(),
                        dependency.RequiredState?.ToString(),
                        dependency.CreatedByUserId,
                        dependency.CreatedAt,
                        dependency.Reason))
                    .ToList();

                return Results.Ok(dependencies);
            });

        app.MapGet(
            "/api/v1/dependencies/blocking-chain/{nodeType}/{nodeId:guid}",
            async (
                string nodeType,
                Guid nodeId,
                [FromServices] GetBlockingChainHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!TryParseNodeType(nodeType, out var parsedNodeType))
                {
                    return Results.BadRequest(
                        new { error = $"NodeType '{nodeType}' is not a valid WorkItemDependencyNodeType. Valid values: {string.Join(", ", ValidNodeTypes)}." });
                }

                var result = await handler.HandleAsync(
                    new GetBlockingChainQuery(parsedNodeType, nodeId),
                    cancellationToken);

                var chain = result.Chain
                    .Select(node => new DependencyNodeResponse(node.Type.ToString(), node.Id))
                    .ToList();

                return Results.Ok(chain);
            });
    }

    private static bool TryParseNodeType(string? value, out WorkItemDependencyNodeType nodeType)
        => TryParseEnum(value, out nodeType);

    private static bool TryParseKind(string? value, out WorkItemDependencyKind kind)
        => TryParseEnum(value, out kind);

    // Optional: a null/absent RequiredState is valid (means full completion).
    private static bool TryParseRequiredState(string? value, out WorkItemDependencyRequiredState? requiredState)
    {
        requiredState = null;

        if (value is null)
        {
            return true;
        }

        if (Enum.TryParse<WorkItemDependencyRequiredState>(value, ignoreCase: true, out var parsed) &&
            Enum.IsDefined(parsed))
        {
            requiredState = parsed;
            return true;
        }

        return false;
    }

    private static bool TryParseEnum<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out parsed) && Enum.IsDefined(parsed))
        {
            return true;
        }

        parsed = default;
        return false;
    }
}
