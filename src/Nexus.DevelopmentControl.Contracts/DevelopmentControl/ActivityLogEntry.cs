namespace Nexus.Developer.Core.DevelopmentControl;

// One append-only activity/event entry, with EXACTLY the 34 fields the architecture
// note lists (the workbook's own Activity Log sheet is a coarser 11-column subset; the
// note's 34-field event-log model is the contract every activity-producing operation
// records against). Field order follows the note verbatim:
//   Activity ID, Timestamp UTC, Actor Type, Actor ID, Actor Name, Source, Chat Platform,
//   Chat/Session ID, Prompt ID, Change ID, Correlation ID, Operation, Entity Type,
//   Entity ID, Parent ID, Expected/Previous/New RowVersion, Before/After Value, Reason,
//   Repository, Project, Branch, Worktree, Files/Globs, Preflight Verdict, Result,
//   Evidence, Error Code, Error Message, Duration, Human Review Status, Created At.
// ActivityId follows the workbook's own "ACT-..." string scheme, so the log's identity
// is a string (and MutationResult.ActivityLogEntryId is a string to match).
public sealed record ActivityLogEntry(
    string ActivityId,
    DateTimeOffset TimestampUtc,
    ActorType ActorType,
    string ActorId,
    string ActorName,
    string Source,
    string? ChatPlatform,
    string? ChatOrSessionId,
    string? PromptId,
    string? ChangeId,
    string? CorrelationId,
    string Operation,
    string EntityType,
    string EntityId,
    string? ParentId,
    int? ExpectedRowVersion,
    int? PreviousRowVersion,
    int? NewRowVersion,
    string? BeforeValue,
    string? AfterValue,
    string? Reason,
    string? Repository,
    string? Project,
    string? Branch,
    string? Worktree,
    IReadOnlyList<string> FilesGlobs,
    PreflightVerdict? PreflightVerdict,
    string? Result,
    string? Evidence,
    string? ErrorCode,
    string? ErrorMessage,
    TimeSpan? Duration,
    string? HumanReviewStatus,
    DateTimeOffset CreatedAt);
