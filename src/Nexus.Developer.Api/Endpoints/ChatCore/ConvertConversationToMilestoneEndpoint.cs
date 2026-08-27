using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nexus.Developer.Application.ChatCore;
using Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToMilestone;

namespace Nexus.Developer.Api.Endpoints.ChatCore;

public static class ConvertConversationToMilestoneEndpoint
{
    public static void MapConvertConversationToMilestoneEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/v1/developer-chat/conversations/{conversationId:guid}/convert-to-milestone",
            async (
                Guid conversationId,
                [FromBody] ConvertConversationToMilestoneRequest request,
                [FromServices] ConvertConversationToMilestoneHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Results.BadRequest(new { error = "Name is required." });
                }

                try
                {
                    var result = await handler.HandleAsync(
                        new ConvertConversationToMilestoneCommand(
                            conversationId,
                            request.SubprojectId,
                            request.Name,
                            request.Description,
                            request.TargetDate,
                            request.CreatedByUserId,
                            request.MessageRangeStart,
                            request.MessageRangeEnd),
                        cancellationToken);

                    return Results.Ok(
                        new ConvertConversationToMilestoneResponse(
                            result.MilestoneId.Value,
                            result.MilestoneReference,
                            result.Name,
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
