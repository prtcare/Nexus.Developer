namespace Nexus.Developer.Api.Endpoints.ChatCore;

public sealed record ConvertConversationToIssueRequest(
    string Title,
    string? Description,
    Guid CreatedByUserId,
    Guid? MessageRangeStart,
    Guid? MessageRangeEnd);
