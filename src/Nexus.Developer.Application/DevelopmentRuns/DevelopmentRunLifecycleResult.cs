using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.DevelopmentRuns;

namespace Nexus.Developer.Application.DevelopmentRuns;

// SP1-M05: the read model returned by every DevelopmentRun lifecycle command
// (Start/Cancel/Succeed/Fail), carrying the execution-session fields SP1-M03 added to
// the aggregate so a caller sees what the transition actually recorded. Phase-2
// placeholder ids stay off the surface.
public sealed record DevelopmentRunLifecycleResult(
    DevelopmentRunId DevelopmentRunId,
    DevelopmentRunStatus Status,
    string Reference,
    string? WorkerId,
    string? WorkerType,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? ResultSummary);
