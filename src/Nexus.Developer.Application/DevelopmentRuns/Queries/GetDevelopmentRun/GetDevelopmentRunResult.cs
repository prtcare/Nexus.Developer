using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.DevelopmentRuns;

namespace Nexus.Developer.Application.DevelopmentRuns.Queries.GetDevelopmentRun;

// Phase 1 surfaces only the fields WI-07-10.3.1 owns (Ref, TargetType, TargetId,
// Status, CreatedByUserId, CreatedAt). The Plan/Prompt/Result/Report/CheckSet/
// Verification placeholders are reserved for Phase 2 and stay off the API surface.
public sealed record GetDevelopmentRunResult(
    DevelopmentRunId DevelopmentRunId,
    DevelopmentRunTargetType TargetType,
    Guid TargetId,
    DevelopmentRunStatus Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string Reference);
