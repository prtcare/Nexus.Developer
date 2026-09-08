using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.DevelopmentRuns;

namespace Nexus.Developer.Application.DevelopmentRuns.Queries.GetDevelopmentRun;

// Surfaces the fields WI-07-10.3.1 owns (Ref, TargetType, TargetId, Status,
// CreatedByUserId, CreatedAt) plus the SP1-M03 execution-session fields (WorkerId,
// WorkerType, StartedAt, CompletedAt, ResultSummary) so a governed caller can see who is
// running the run, when it started/ended, and what outcome it recorded. The
// Plan/Prompt/Result/Report/CheckSet/Verification placeholders are reserved for
// Phase 2 and stay off the API surface.
public sealed record GetDevelopmentRunResult(
    DevelopmentRunId DevelopmentRunId,
    DevelopmentRunTargetType TargetType,
    Guid TargetId,
    DevelopmentRunStatus Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string Reference,
    string? WorkerId,
    string? WorkerType,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? ResultSummary);
