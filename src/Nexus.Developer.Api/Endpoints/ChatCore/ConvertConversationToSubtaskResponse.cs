namespace Nexus.Developer.Api.Endpoints.ChatCore;

public sealed record ConvertConversationToSubtaskResponse(
    Guid SubtaskId,
    string SubtaskReference,
    string Title,
    Guid ObjectChatLinkId,
    Guid ConversationId);
