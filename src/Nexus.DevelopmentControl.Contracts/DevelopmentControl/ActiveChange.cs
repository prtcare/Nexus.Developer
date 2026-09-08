namespace Nexus.Developer.Core.DevelopmentControl;

// One row of the workbook's Active Changes sheet, whose exact column set (inspected
// directly from NEXUS_DEVELOPMENT_CONTROL.xlsx sheet "Active Changes", header row 5, and
// mirrored by control/ACTIVE_CHANGES.csv) is:
//   Change ID, Node ID, Milestone / Feature, Summary, Requested By, Worker,
//   Repositories, Projects, Files / Globs, Schema Contexts, Contracts / APIs, Status,
//   Preflight Verdict, Conflicts With, Dependency On, Risk, Branch, Worktree,
//   Started At, Last Heartbeat, Completed At, Result / Evidence, Change Version,
//   Session / Chat, Notes, Version History ID, ADR ID, Affected Nodes, Change Type,
//   Validation Result.
// Free-text columns stay strings (the later Excel adapter owns parsing); multi-value
// columns (pipe/separator-delimited) are lists; Preflight Verdict is typed to the
// PreflightVerdict enum it maps onto. NodeId is kept as-authored text because the cell
// can hold non-node annotation text as well as roadmap node ids.
public sealed record ActiveChange(
    string ChangeId,
    string NodeId,
    string MilestoneOrFeature,
    string Summary,
    string RequestedBy,
    string Worker,
    IReadOnlyList<string> Repositories,
    IReadOnlyList<string> Projects,
    IReadOnlyList<string> FilesGlobs,
    IReadOnlyList<string> SchemaContexts,
    IReadOnlyList<string> ContractsApis,
    string Status,
    PreflightVerdict? PreflightVerdict,
    string ConflictsWith,
    string DependencyOn,
    string Risk,
    string Branch,
    string Worktree,
    DateTimeOffset? StartedAt,
    DateTimeOffset? LastHeartbeat,
    DateTimeOffset? CompletedAt,
    string ResultOrEvidence,
    string ChangeVersion,
    string SessionOrChat,
    string Notes,
    string VersionHistoryId,
    string AdrId,
    IReadOnlyList<string> AffectedNodes,
    string ChangeType,
    string ValidationResult);
