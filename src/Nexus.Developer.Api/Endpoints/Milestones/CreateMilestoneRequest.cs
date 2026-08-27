namespace Nexus.Developer.Api.Endpoints.Milestones;

public sealed record CreateMilestoneRequest(
    Guid SubprojectId,
    string Name,
    string? Description,
    DateTimeOffset? TargetDate,
    Guid CreatedByUserId);
