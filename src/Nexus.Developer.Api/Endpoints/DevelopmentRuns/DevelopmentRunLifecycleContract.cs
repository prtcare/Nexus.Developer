namespace Nexus.Developer.Api.Endpoints.DevelopmentRuns;

// SP1-M05: the four governed DevelopmentRun lifecycle transitions. Start is the only
// one carrying required worker identity; Succeed requires the caller's own summary of
// what happened; Cancel and Fail carry an optional reason (Cancel falls back to the
// aggregate's standard text). Nothing is ever auto-generated.
public sealed record StartDevelopmentRunRequest(
    string WorkerId,
    string WorkerType);

public sealed record CancelDevelopmentRunRequest(
    string? Summary = null);

public sealed record SucceedDevelopmentRunRequest(
    string Summary);

public sealed record FailDevelopmentRunRequest(
    string? Summary = null);

// The read model every lifecycle transition returns, so a caller can see exactly what
// the transition recorded (execution-session fields included).
public sealed record DevelopmentRunLifecycleResponse(
    Guid DevelopmentRunId,
    int Status,
    string Reference,
    string? WorkerId,
    string? WorkerType,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? ResultSummary);
