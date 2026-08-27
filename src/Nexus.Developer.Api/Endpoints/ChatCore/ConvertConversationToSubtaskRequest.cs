namespace Nexus.Developer.Api.Endpoints.ChatCore;

public sealed record ConvertConversationToSubtaskRequest(
    Guid TaskId,
    string Title,
    string? Description,
    Guid CreatedByUserId,
    Guid? MessageRangeStart,
    Guid? MessageRangeEnd);
