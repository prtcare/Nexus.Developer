namespace Nexus.Developer.Api.Endpoints.DevelopmentRuns;

public sealed record CreateDevelopmentRunRequest(
    string TargetType,
    Guid TargetId,
    Guid CreatedByUserId);
