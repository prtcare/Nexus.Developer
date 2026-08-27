using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nexus.Developer.Application.ChatCore;
using Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToIssue;

namespace Nexus.Developer.Api.Endpoints.ChatCore;

public static class ConvertConversationToIssueEndpoint
{
    public static void MapConvertConversationToIssueEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/v1/developer-chat/conversations/{conversationId:guid}/convert-to-issue",
            async (
                Guid conversationId,
                [FromBody] ConvertConversationToIssueRequest request,
                [FromServices] ConvertConversationToIssueHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Title))
                {
                    return Results.BadRequest(new { error = "Title is required." });
                }

                try
                {
                    var result = await handler.HandleAsync(
                        new ConvertConversationToIssueCommand(
                            conversationId,
                            request.Title,
                            request.Description,
                            request.CreatedByUserId,
                            request.MessageRangeStart,
                            request.MessageRangeEnd),
                        cancellationToken);

                    return Results.Ok(
                        new ConvertConversationToIssueResponse(
                            result.IssueId.Value,
                            result.IssueReference,
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
