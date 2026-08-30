namespace Nexus.Developer.Api.Endpoints.DevelopmentRuns;

public sealed record CreateDevelopmentRunResponse(
    Guid DevelopmentRunId,
    string TargetType,
    Guid TargetId,
    string Reference);
