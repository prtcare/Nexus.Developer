using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nexus.Developer.Application.ObjectChatLinks;
using Nexus.Developer.Application.ObjectChatLinks.Commands.CreateObjectChatLink;
using Nexus.Developer.Application.ObjectChatLinks.Queries.ListObjectChatLinksByConversation;
using Nexus.Developer.Application.ObjectChatLinks.Queries.ListObjectChatLinksByTarget;
using Nexus.Developer.Core.ObjectChatLinks;

namespace Nexus.Developer.Api.Endpoints.ObjectChatLinks;

public static class ObjectChatLinkEndpoint
{
    private static readonly string[] ValidTargetTypes = Enum.GetNames<ObjectChatLinkTargetType>();

    public static void MapObjectChatLinkEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/v1/object-chat-links",
            async (
                [FromBody] CreateObjectChatLinkRequest request,
                [FromServices] CreateObjectChatLinkHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!TryParseTargetType(request.TargetType, out var targetType))
                {
                    return Results.BadRequest(
                        new { error = $"TargetType '{request.TargetType}' is not a valid ObjectChatLinkTargetType. Valid values: {string.Join(", ", ValidTargetTypes)}." });
                }

                try
                {
                    var result = await handler.HandleAsync(
                        new CreateObjectChatLinkCommand(
                            request.ConversationId,
                            request.MessageRangeStart,
                            request.MessageRangeEnd,
                            targetType,
                            request.TargetId,
                            request.LinkedByUserId),
                        cancellationToken);

                    return Results.Ok(
                        new CreateObjectChatLinkResponse(
                            result.ObjectChatLinkId.Value,
                            result.ConversationId,
                            result.TargetType.ToString(),
                            result.TargetId,
                            result.LinkedAt));
                }
                catch (ObjectChatLinkTargetNotFoundException)
                {
                    // Target object does not resolve: 404, never an unhandled 500.
                    return Results.NotFound(
                        new { error = $"The {targetType} target '{request.TargetId}' does not exist." });
                }
            });

        app.MapGet(
            "/api/v1/conversations/{conversationId:guid}/object-chat-links",
            async (
                Guid conversationId,
                [FromServices] ListObjectChatLinksByConversationHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new ListObjectChatLinksByConversationQuery(conversationId),
                    cancellationToken);

                var links = result.Links
                    .Select(link => new GetObjectChatLinkResponse(
                        link.ObjectChatLinkId.Value,
                        link.ConversationId,
                        link.MessageRangeStart,
                        link.MessageRangeEnd,
                        link.TargetType.ToString(),
                        link.TargetId,
                        link.LinkedByUserId,
                        link.LinkedAt))
                    .ToList();

                return Results.Ok(links);
            });

        app.MapGet(
            "/api/v1/object-chat-links/by-target",
            async (
                string targetType,
                Guid targetId,
                [FromServices] ListObjectChatLinksByTargetHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!TryParseTargetType(targetType, out var parsedTargetType))
                {
                    return Results.BadRequest(
                        new { error = $"TargetType '{targetType}' is not a valid ObjectChatLinkTargetType. Valid values: {string.Join(", ", ValidTargetTypes)}." });
                }

                var result = await handler.HandleAsync(
                    new ListObjectChatLinksByTargetQuery(parsedTargetType, targetId),
                    cancellationToken);

                var links = result.Links
                    .Select(link => new GetObjectChatLinkResponse(
                        link.ObjectChatLinkId.Value,
                        link.ConversationId,
                        link.MessageRangeStart,
                        link.MessageRangeEnd,
                        link.TargetType.ToString(),
                        link.TargetId,
                        link.LinkedByUserId,
                        link.LinkedAt))
                    .ToList();

                return Results.Ok(links);
            });
    }

    private static bool TryParseTargetType(
        string? value,
        out ObjectChatLinkTargetType targetType)
    {
        if (Enum.TryParse<ObjectChatLinkTargetType>(value, ignoreCase: true, out targetType) &&
            Enum.IsDefined(targetType))
        {
            return true;
        }

        targetType = default;
        return false;
    }
}
