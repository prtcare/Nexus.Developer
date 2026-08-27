namespace Nexus.Developer.Core.ChatCore;

// Client contract for Chat Core (Nexus.Experience), not an implementation -- the
// HTTP implementation lives in Nexus.Developer.Infrastructure/ChatCore.
public interface IChatCoreClient
{
    // Returns null when Chat Core's GET /api/v1/conversations/{id} responds 404
    // (the conversation does not exist). Any other failure -- 5xx, timeout,
    // deserialization error -- throws: that is a real infrastructure failure, not
    // "conversation doesn't exist".
    Task<ChatCoreConversation?> GetConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);
}
