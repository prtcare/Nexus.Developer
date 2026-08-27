using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nexus.Developer.Application.ChatCore;
using Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToSubtask;

namespace Nexus.Developer.Api.Endpoints.ChatCore;

public static class ConvertConversationToSubtaskEndpoint
{
    public static void MapConvertConversationToSubtaskEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/v1/developer-chat/conversations/{conversationId:guid}/convert-to-subtask",
            async (
                Guid conversationId,
                [FromBody] ConvertConversationToSubtaskRequest request,
                [FromServices] ConvertConversationToSubtaskHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Title))
                {
                    return Results.BadRequest(new { error = "Title is required." });
                }

                // Subtask's parent Task is required -- without it the converted
                // Subtask would be parented to a non-existent Task.
                if (request.TaskId == Guid.Empty)
                {
                    return Results.BadRequest(new { error = "TaskId is required." });
                }

                try
                {
                    var result = await handler.HandleAsync(
                        new ConvertConversationToSubtaskCommand(
                            conversationId,
                            request.TaskId,
                            request.Title,
                            request.Description,
                            request.CreatedByUserId,
                            request.MessageRangeStart,
                            request.MessageRangeEnd),
                        cancellationToken);

                    return Results.Ok(
                        new ConvertConversationToSubtaskResponse(
                            result.SubtaskId.Value,
                            result.SubtaskReference,
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
