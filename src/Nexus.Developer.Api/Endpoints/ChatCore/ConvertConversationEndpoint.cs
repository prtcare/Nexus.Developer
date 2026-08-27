using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nexus.Developer.Application.ChatCore;
using Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToFeature;

namespace Nexus.Developer.Api.Endpoints.ChatCore;

public static class ConvertConversationEndpoint
{
    public static void MapConvertConversationEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/v1/developer-chat/conversations/{conversationId:guid}/convert-to-feature",
            async (
                Guid conversationId,
                [FromBody] ConvertConversationToFeatureRequest request,
                [FromServices] ConvertConversationToFeatureHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Title))
                {
                    return Results.BadRequest(new { error = "Title is required." });
                }

                try
                {
                    var result = await handler.HandleAsync(
                        new ConvertConversationToFeatureCommand(
                            conversationId,
                            request.SubprojectId,
                            request.Title,
                            request.Description,
                            request.CreatedByUserId,
                            request.MessageRangeStart,
                            request.MessageRangeEnd),
                        cancellationToken);

                    return Results.Ok(
                        new ConvertConversationToFeatureResponse(
                            result.FeatureId.Value,
                            result.FeatureReference,
                            result.Title,
                            result.ObjectChatLinkId.Value,
                            result.ConversationId));
                }
                catch (ConversationNotFoundException)
                {
                    // Chat Core says the conversation does not exist: 404, not 400,
                    // never an unhandled 500.
                    return Results.NotFound(
                        new { error = $"The conversation '{conversationId}' does not exist." });
                }
            });
    }
}
