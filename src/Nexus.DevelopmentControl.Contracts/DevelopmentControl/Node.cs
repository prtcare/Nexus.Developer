namespace Nexus.Developer.Core.DevelopmentControl;

// One roadmap node, shaped to the Master Roadmap sheet's data contract (the "Node ID /
// Parent ID / Node Type / ... / Is Current / Notes" column set in the ledger). Field
// order and count follow the architecture note exactly; nullable strings reflect the
// ledger's genuinely blank cells (a Layer node has no Phase, an unimplemented node has
// no Outcome). Dependencies is the declared "Depends On" set as typed node ids.
// RowVersion is the workbook's "Record Version" -- the append-only control rule makes a
// governed fact change by adding a higher Record Version, never by editing a row in
// place. IsDeleted mirrors "Is Current = No" (history is never physically deleted).
public sealed record Node(
    NodeId NodeId,
    NodeId? ParentId,
    NodeType NodeType,
    string SortKey,
    string Path,
    string Layer,
    string? Phase,
    string Name,
    string? Outcome,
    IReadOnlyList<NodeId> Dependencies,
    bool ParallelSafe,
    IReadOnlyList<string> Projects,
    IReadOnlyList<string> FilesGlobs,
    IReadOnlyList<string> SchemaContexts,
    IReadOnlyList<string> ContractsApis,
    string? Gate,
    string? AcceptanceCriteria,
    Status Status,
    bool BreakdownComplete,
    int? ManualProgress,
    int? DerivedProgress,
    int? ReportedProgress,
    string? Owner,
    string? Priority,
    string? Risk,
    int RowVersion,
    bool IsDeleted,
    string? Source,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
