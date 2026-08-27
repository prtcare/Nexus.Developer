namespace Nexus.Developer.Api.Endpoints.Milestones;

public sealed record LinkMilestoneRequest(
    int TargetType,
    Guid TargetId,
    Guid LinkedByUserId);
