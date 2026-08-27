namespace Nexus.Developer.Api.Endpoints.ChatCore;

public sealed record ConvertConversationToIssueResponse(
    Guid IssueId,
    string IssueReference,
    string Title,
    Guid ObjectChatLinkId,
    Guid ConversationId);
