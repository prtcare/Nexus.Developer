namespace Nexus.Developer.Api.Endpoints.DevelopmentRuns;

public sealed record GetDevelopmentRunResponse(
    Guid DevelopmentRunId,
    string TargetType,
    Guid TargetId,
    int Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string Reference);
