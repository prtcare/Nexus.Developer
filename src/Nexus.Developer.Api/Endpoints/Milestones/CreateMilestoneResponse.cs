namespace Nexus.Developer.Api.Endpoints.Milestones;

public sealed record CreateMilestoneResponse(
    Guid MilestoneId,
    string Name,
    string Reference);
