namespace Nexus.Developer.Api.Endpoints.DevelopmentRuns;

public sealed record GetDevelopmentRunResponse(
    Guid DevelopmentRunId,
    string TargetType,
    Guid TargetId,
    int Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string Reference,
    string? WorkerId,
    string? WorkerType,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? ResultSummary);
