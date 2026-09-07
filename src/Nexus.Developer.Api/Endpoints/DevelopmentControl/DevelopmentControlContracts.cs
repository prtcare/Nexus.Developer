namespace Nexus.Developer.Api.Endpoints.DevelopmentControl;

// SP1-M05: request/response contracts for the Development Control plane. Deliberately
// grouped in this one file (the existing repo splits one record per file for the
// SQL-backed slices; the control plane's many small DTOs are compacted here to keep the
// new endpoint folder proportionate -- still plain sealed records, JSON-bound by the
// minimal-API binder exactly like the per-file records elsewhere).
//
// Reads project Core Development Control DTOs (Node/ControlState/ActiveChange/
// PreflightResult) onto flat response records whose node identities are plain strings,
// so the JSON never leaks the Core NodeId record-struct wrapper.

// ---------------------------------------------------------------- responses

public sealed record GetDevelopmentControlStateResponse(
    string WorkbookVersion,
    string RoadmapVersion,
    string? RootNodeId,
    int CurrentNodeCount,
    int MilestoneCount,
    int WorkItemCount,
    int BlockedNodeCount,
    int ActiveChangeCount,
    int OpenAuditFindingCount,
    DateTimeOffset? LastUpdatedAt);

public sealed record DevelopmentControlNodeResponse(
    string NodeId,
    string? ParentId,
    string NodeType,
    string SortKey,
    string Path,
    string Layer,
    string? Phase,
    string Name,
    string Status,
    IReadOnlyList<string> Dependencies,
    bool BreakdownComplete,
    int? ReportedProgress,
    string? Owner,
    string? Risk,
    int RowVersion,
    DateTimeOffset UpdatedAt);

public sealed record DevelopmentControlActiveChangeResponse(
    string ChangeId,
    string NodeId,
    string Summary,
    string Worker,
    string Status,
    string Branch,
    string Worktree,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string ResultOrEvidence,
    IReadOnlyList<string> AffectedNodes,
    string ChangeType,
    string ValidationResult);

public sealed record DevelopmentControlPreflightResponse(
    string Verdict,
    string? Detail,
    IReadOnlyList<string> Findings);

public sealed record DevelopmentControlMutationResponse(
    bool Success,
    string Outcome,
    string? ActivityLogEntryId,
    DevelopmentControlNodeResponse? Node);

// ---------------------------------------------------------------- requests

public sealed record ReserveWorkItemRequest(
    string ChangeId,
    string ActorType,
    string ActorName,
    string? ActorId = null,
    string? Branch = null,
    string? Worktree = null,
    string? Reason = null,
    string? SessionId = null);

public sealed record ReleaseReservationRequest(
    string ChangeId,
    string ActorType,
    string ActorName,
    string? ActorId = null,
    string? Reason = null,
    string? SessionId = null);

public sealed record CompleteWorkItemRequest(
    string ChangeId,
    string ActorType,
    string ActorName,
    string ResultOrEvidence,
    string? ActorId = null,
    string? Reason = null,
    string? SessionId = null);

public sealed record RunPreflightRequest(
    string ChangeId,
    string RoadmapNodeId,
    IReadOnlyList<string>? Repositories = null,
    IReadOnlyList<string>? Projects = null,
    IReadOnlyList<string>? FilesGlobs = null,
    bool SchemaOrDbContextMutation = false,
    IReadOnlyList<string>? ContractsApis = null,
    IReadOnlyList<string>? Dependencies = null,
    string? Risk = null,
    string? Worker = null,
    string? Branch = null,
    string? SiblingWorktree = null);
