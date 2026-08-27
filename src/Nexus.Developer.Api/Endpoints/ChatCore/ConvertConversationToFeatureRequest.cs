namespace Nexus.Developer.Api.Endpoints.ChatCore;

public sealed record ConvertConversationToFeatureRequest(
    Guid SubprojectId,
    string Title,
    string? Description,
    Guid CreatedByUserId,
    Guid? MessageRangeStart,
    Guid? MessageRangeEnd);
