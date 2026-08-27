namespace Nexus.Developer.Api.Endpoints.ChatCore;

public sealed record ConvertConversationToTaskResponse(
    Guid TaskId,
    string TaskReference,
    string Title,
    Guid ObjectChatLinkId,
    Guid ConversationId);
