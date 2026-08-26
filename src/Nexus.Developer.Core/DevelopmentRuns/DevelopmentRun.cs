using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.DevelopmentRuns;

// Phase 1 placeholder only. WI-07-10.3.1: "Ref, TargetType, TargetId, Status,
// CreatedByUserId, CreatedAt only. Plan/Prompt/Result/Report/Check/Verification
// relationships reserved as nullable placeholders." The six placeholder ids below
// are never populated or read by any Phase 1 code path -- they exist purely so
// Phase 2's Development Run pipeline (P2-6) can start using this row without a
// breaking migration. No business logic in this class or anywhere in Phase 1
// branches on them, and nothing here fakes autonomous execution.
public sealed class DevelopmentRun : AggregateRoot<DevelopmentRunId>
{
    public DevelopmentRun(
        DevelopmentRunId id,
        DevelopmentRunTargetType targetType,
        Guid targetId,
        Guid createdByUserId,
        DateTimeOffset createdAt)
        : base(id)
    {
        if (!Enum.IsDefined(targetType))
        {
            throw new ArgumentOutOfRangeException(nameof(targetType));
        }

        TargetType = targetType;
        TargetId = targetId;
        Status = DevelopmentRunStatus.NotStarted;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
    }

    private DevelopmentRun(
        DevelopmentRunId id,
        DevelopmentRunTargetType targetType,
        Guid targetId,
        DevelopmentRunStatus status,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        string reference,
        Guid? planId,
        Guid? promptId,
        Guid? resultId,
        Guid? reportId,
        Guid? checkSetId,
        Guid? verificationId)
        : base(id)
    {
        TargetType = targetType;
        TargetId = targetId;
        Status = status;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
        Reference = reference;
        PlanId = planId;
        PromptId = promptId;
        ResultId = resultId;
        ReportId = reportId;
        CheckSetId = checkSetId;
        VerificationId = verificationId;
    }

    public DevelopmentRunTargetType TargetType { get; }

    public Guid TargetId { get; }

    public DevelopmentRunStatus Status { get; private set; }

    public Guid CreatedByUserId { get; }

    public DateTimeOffset CreatedAt { get; }

    public string Reference { get; private set; } = string.Empty;

    // Phase 2 placeholders -- reserved, unused in Phase 1. See class remarks.
    public Guid? PlanId { get; private set; }

    public Guid? PromptId { get; private set; }

    public Guid? ResultId { get; private set; }

    public Guid? ReportId { get; private set; }

    public Guid? CheckSetId { get; private set; }

    public Guid? VerificationId { get; private set; }

    public static DevelopmentRun Restore(
        DevelopmentRunId id,
        DevelopmentRunTargetType targetType,
        Guid targetId,
        DevelopmentRunStatus status,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        string reference,
        Guid? planId,
        Guid? promptId,
        Guid? resultId,
        Guid? reportId,
        Guid? checkSetId,
        Guid? verificationId)
        => new(id, targetType, targetId, status, createdByUserId, createdAt, reference,
            planId, promptId, resultId, reportId, checkSetId, verificationId);
}
