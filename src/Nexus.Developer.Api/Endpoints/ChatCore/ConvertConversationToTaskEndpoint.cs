using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nexus.Developer.Application.ChatCore;
using Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToTask;

namespace Nexus.Developer.Api.Endpoints.ChatCore;

public static class ConvertConversationToTaskEndpoint
{
    public static void MapConvertConversationToTaskEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/v1/developer-chat/conversations/{conversationId:guid}/convert-to-task",
            async (
                Guid conversationId,
                [FromBody] ConvertConversationToTaskRequest request,
                [FromServices] ConvertConversationToTaskHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Title))
                {
                    return Results.BadRequest(new { error = "Title is required." });
                }

                // Task's parent Feature is required -- without it the converted
                // Task would be parented to a non-existent Feature.
                if (request.FeatureId == Guid.Empty)
                {
                    return Results.BadRequest(new { error = "FeatureId is required." });
                }

                try
                {
                    var result = await handler.HandleAsync(
                        new ConvertConversationToTaskCommand(
                            conversationId,
                            request.FeatureId,
                            request.Title,
                            request.Description,
                            request.CreatedByUserId,
                            request.MessageRangeStart,
                            request.MessageRangeEnd),
                        cancellationToken);

                    return Results.Ok(
                        new ConvertConversationToTaskResponse(
                            result.TaskId.Value,
                            result.TaskReference,
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
