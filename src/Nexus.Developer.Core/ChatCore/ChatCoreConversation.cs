namespace Nexus.Developer.Core.ChatCore;

// Mirror of Nexus.Experience's GetConversationResponse (ConversationId, Title,
// CreatedAt) as returned by its GET /api/v1/conversations/{id}. Nexus.Developer
// holds the ids the Chat API already returns and never imports the Chat Core
// domain assembly (AGENTS.md Boundary rules), so the contract is duplicated here
// as plain data and populated by HttpChatCoreClient.
public sealed record ChatCoreConversation(
    Guid ConversationId,
    string Title,
    DateTimeOffset CreatedAt);
