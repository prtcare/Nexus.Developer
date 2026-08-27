namespace Nexus.Developer.Api.Endpoints.ChatCore;

public sealed record ConvertConversationToTaskRequest(
    Guid FeatureId,
    string Title,
    string? Description,
    Guid CreatedByUserId,
    Guid? MessageRangeStart,
    Guid? MessageRangeEnd);
