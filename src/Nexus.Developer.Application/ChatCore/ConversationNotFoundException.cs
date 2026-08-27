namespace Nexus.Developer.Application.ChatCore;

// Thrown by ConvertConversationToFeatureHandler when Chat Core's
// GET /api/v1/conversations/{id} reports the conversation does not exist
// (IChatCoreClient returned null). The endpoint maps this to 404 Not Found
// rather than letting it surface as an unhandled 500.
public sealed class ConversationNotFoundException : Exception
{
    public ConversationNotFoundException(Guid conversationId)
        : base($"The conversation '{conversationId}' does not exist.")
    {
        ConversationId = conversationId;
    }

    public Guid ConversationId { get; }
}
