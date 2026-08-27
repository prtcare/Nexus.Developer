namespace Nexus.Developer.Api.Endpoints.ChatCore;

public sealed record ConvertConversationToMilestoneRequest(
    Guid SubprojectId,
    string Name,
    string? Description,
    DateTimeOffset? TargetDate,
    Guid CreatedByUserId,
    Guid? MessageRangeStart,
    Guid? MessageRangeEnd);
