namespace Nexus.Developer.Api.Endpoints.Milestones;

public sealed record GetMilestoneResponse(
    Guid MilestoneId,
    Guid SubprojectId,
    string Name,
    string Description,
    DateTimeOffset? TargetDate,
    int Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string Reference);
