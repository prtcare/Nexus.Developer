using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.DevelopmentRuns;

// Phase 1 (WI-07-10.3.1) created only NotStarted rows carrying "Ref, TargetType,
// TargetId, Status, CreatedByUserId, CreatedAt" plus six reserved Phase-2 nullable
// placeholders (Plan/Prompt/Result/Report/Check/Verification). SP1-M03 (Lane B1)
// expands the row into a real, governed lifecycle that a human or external runner
// drives: Start -> Succeed/Fail/Cancel, each transition recording only what the
// caller supplies (worker identity on Start, a caller-supplied result summary on
// Succeed/Fail/Cancel). NOTHING here fakes autonomous execution: there is no
// scheduler, prompt runner, or auto-verification, and the reserved placeholder ids
// below are never populated, read, or branched on by any Phase 1 code path -- they
// exist purely so Phase 2's Development Run pipeline (P2-6) can start using this row
// without a breaking migration.
public sealed class DevelopmentRun : AggregateRoot<DevelopmentRunId>
{
    // Standard summary used by Cancel when the caller supplies none. It only records
    // that the run was cancelled -- it never invents content about what happened.
    public const string DefaultCancellationSummary = "Run cancelled.";

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
        Guid? verificationId,
        string? workerId,
        string? workerType,
        DateTimeOffset? startedAt,
        DateTimeOffset? completedAt,
        string? resultSummary)
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
        WorkerId = workerId;
        WorkerType = workerType;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        ResultSummary = resultSummary;
    }

    public DevelopmentRunTargetType TargetType { get; }

    public Guid TargetId { get; }

    public DevelopmentRunStatus Status { get; private set; }

    public Guid CreatedByUserId { get; }

    public DateTimeOffset CreatedAt { get; }

    public string Reference { get; private set; } = string.Empty;

    // Execution-session fields (SP1-M03, required now). All nullable -- a NotStarted
    // run has none of them. Start() stamps WorkerId/WorkerType/StartedAt; the first
    // terminal transition stamps CompletedAt and the caller-supplied ResultSummary.
    public string? WorkerId { get; private set; }

    // Free text: no run-worker vocabulary exists outside the DevelopmentControl
    // workbook's Human/Agent actor kind, which is a separate context and is not
    // reused here. Phase 2 may promote this to a value object if a run-worker
    // vocabulary emerges -- today it is a nullable column, exactly like WorkerId.
    public string? WorkerType { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    // The caller's own words for what actually happened. Never auto-generated: a
    // Completed run must carry a non-blank summary, a Failed run may carry an error
    // reason, and a Cancelled run falls back to the standard text only when the
    // caller supplies none.
    public string? ResultSummary { get; private set; }

    // Derived projection of the run's terminal outcome. Non-null only once the run is
    // terminal, and always consistent with Status (Completed/Failed/Cancelled map 1:1
    // to Success/Failed/Cancelled) -- see DevelopmentRunResult remarks.
    public DevelopmentRunResult? Result => Status switch
    {
        DevelopmentRunStatus.Completed => new DevelopmentRunResult(DevelopmentRunOutcome.Success, ResultSummary),
        DevelopmentRunStatus.Failed => new DevelopmentRunResult(DevelopmentRunOutcome.Failed, ResultSummary),
        DevelopmentRunStatus.Cancelled => new DevelopmentRunResult(DevelopmentRunOutcome.Cancelled, ResultSummary),
        _ => null
    };

    // Phase 2 placeholders -- reserved, unused in Phase 1. See class remarks.
    public Guid? PlanId { get; private set; }

    public Guid? PromptId { get; private set; }

    public Guid? ResultId { get; private set; }

    public Guid? ReportId { get; private set; }

    public Guid? CheckSetId { get; private set; }

    public Guid? VerificationId { get; private set; }

    // Rehydration path: only a repository restoring a persisted row knows the
    // reference the store already allocated and the run's execution-session/result
    // state -- the constructor above never does. The SP1-M03 fields default to null so
    // a NotStarted/Planned run (the only rows Phase 1 persists today) round-trips
    // unchanged; a terminal row supplies them.
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
        Guid? verificationId,
        string? workerId = null,
        string? workerType = null,
        DateTimeOffset? startedAt = null,
        DateTimeOffset? completedAt = null,
        string? resultSummary = null)
        => new(id, targetType, targetId, status, createdByUserId, createdAt, reference,
            planId, promptId, resultId, reportId, checkSetId, verificationId,
            workerId, workerType, startedAt, completedAt, resultSummary);

    // Lifecycle: NotStarted -> InProgress. The run records who/what is executing it;
    // a NotStarted run has no worker yet, so Start requires both.
    public void Start(string workerId, string workerType)
    {
        EnsureStateIs(
            DevelopmentRunStatus.NotStarted,
            null,
            $"A {Status} run cannot be started; only a NotStarted run can Start.");

        ArgumentException.ThrowIfNullOrWhiteSpace(workerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(workerType);

        WorkerId = workerId.Trim();
        WorkerType = workerType.Trim();
        StartedAt = DateTimeOffset.UtcNow;
        Status = DevelopmentRunStatus.InProgress;
    }

    // Lifecycle: NotStarted | InProgress -> Cancelled. Caller-supplied or standard
    // summary; stamps when the run ended.
    public void Cancel(string? summary = null)
    {
        EnsureStateIs(
            DevelopmentRunStatus.NotStarted,
            DevelopmentRunStatus.InProgress,
            $"A {Status} run cannot be cancelled; only NotStarted or InProgress runs can Cancel.");

        Status = DevelopmentRunStatus.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
        ResultSummary = string.IsNullOrWhiteSpace(summary)
            ? DefaultCancellationSummary
            : summary.Trim();
    }

    // Lifecycle: InProgress -> Completed. A Completed run must carry a non-blank,
    // caller-supplied summary -- nothing is ever auto-generated here.
    public void Succeed(string summary)
    {
        EnsureStateIs(
            DevelopmentRunStatus.InProgress,
            null,
            $"A {Status} run cannot be completed; only an InProgress run can Succeed.");

        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        Status = DevelopmentRunStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        ResultSummary = summary.Trim();
    }

    // Lifecycle: InProgress -> Failed. The summary is the caller's error reason and
    // is optional (a failed run "may carry an error reason", not must).
    public void Fail(string? summary = null)
    {
        EnsureStateIs(
            DevelopmentRunStatus.InProgress,
            null,
            $"A {Status} run cannot be failed; only an InProgress run can Fail.");

        Status = DevelopmentRunStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        ResultSummary = summary?.Trim();
    }

    private void EnsureStateIs(
        DevelopmentRunStatus expected,
        DevelopmentRunStatus? alsoExpected,
        string message)
    {
        var legal = Status == expected || (alsoExpected.HasValue && Status == alsoExpected.Value);
        if (!legal)
        {
            throw new InvalidOperationException(message);
        }
    }
}
