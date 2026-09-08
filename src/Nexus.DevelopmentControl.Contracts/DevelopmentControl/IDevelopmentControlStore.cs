namespace Nexus.Developer.Core.DevelopmentControl;

// Programmatic read/mutate contract for Nexus Development Control state -- the Master
// Roadmap, Active Changes, Audit Findings and Activity Log that today live only in
// NEXUS_DEVELOPMENT_CONTROL.xlsx. WI-07-0.2.1 defines this interface and the DTOs it
// depends on; nothing implements it yet (that is WI-07-0.2.3's Excel adapter, and
// possibly a SQL store later). Read/validation operations take only their parameters and
// a CancellationToken; every mutating operation takes a MutationEnvelope (expected
// RowVersion, actor/source provenance, change id, idempotency key, reason) and returns a
// MutationResult<T>, so expected domain outcomes (conflict, invalid input, reserved
// state) are returned as results rather than thrown.
//
// Concurrency and versioning semantics, shared by every node mutation below:
//   - RowVersion is store-managed. On Create the store assigns the first version (1);
//     on Update/Reparent/Retire/etc. it writes a NEW higher version (append-only, the
//     previous version becomes non-current) exactly as the workbook's Record Version
//     rule requires.
//   - envelope.ExpectedRowVersion is the optimistic-concurrency token: null on Create,
//     otherwise the RowVersion the caller last read. If the store's current RowVersion
//     differs, the mutation returns Conflict=true with ConflictDetails = the current
//     Node and applies nothing.
public interface IDevelopmentControlStore
{
    // Overall control snapshot (workbook versions, node/change/finding counts, root
    // node), or null when the store has no content yet.
    Task<ControlState?> GetControlStateAsync(CancellationToken cancellationToken = default);

    // The current (Is Current = Yes) version of one node, or null if the id is unknown.
    Task<Node?> GetNodeAsync(NodeId nodeId, CancellationToken cancellationToken = default);

    // The subtree rooted at rootNodeId, root first, in SortKey order (level-order). Every
    // node whose Path descends from rootNodeId is included.
    Task<IReadOnlyList<Node>> GetSubtreeAsync(NodeId rootNodeId, CancellationToken cancellationToken = default);

    // Nodes matching the criteria (all filters optional); result is in SortKey order.
    Task<IReadOnlyList<Node>> SearchNodesAsync(NodeSearchCriteria criteria, CancellationToken cancellationToken = default);

    // Add a new node as a new NodeId/RowVersion-1 current row. envelope.ExpectedRowVersion
    // is null (there is no previous version to race). The node's NodeId must not already
    // exist as a current row, or ValidationErrors reports it.
    Task<MutationResult<Node>> CreateNodeAsync(Node node, MutationEnvelope envelope, CancellationToken cancellationToken = default);

    // Replace the current version of updatedNode.NodeId with a new higher RowVersion
    // carrying the supplied field values (the passed Node is the desired full shape).
    Task<MutationResult<Node>> UpdateNodeAsync(Node updatedNode, MutationEnvelope envelope, CancellationToken cancellationToken = default);

    // Move a node under a new parent (null = root). Its SortKey/path-based ordering is
    // refreshed by the store; children are carried along.
    Task<MutationResult<Node>> ReparentNodeAsync(NodeId nodeId, NodeId? newParentId, MutationEnvelope envelope, CancellationToken cancellationToken = default);

    // Retire a node: write a new version with IsDeleted=true (history is never deleted --
    // AGENTS.md append-only). envelope.Reason should say why.
    Task<MutationResult<Node>> RetireNodeAsync(NodeId nodeId, MutationEnvelope envelope, CancellationToken cancellationToken = default);

    // Add a roadmap-level dependency edge to nodeId's Dependencies (the typed "Depends
    // On" set). This is the roadmap declaration; WI-07-2.1.1's WorkItemDependency graph
    // is a separate, stricter mechanism.
    Task<MutationResult<Node>> AddDependencyAsync(NodeId nodeId, NodeId dependencyNodeId, MutationEnvelope envelope, CancellationToken cancellationToken = default);

    // Remove a dependency edge previously declared on nodeId.
    Task<MutationResult<Node>> RemoveDependencyAsync(NodeId nodeId, NodeId dependencyNodeId, MutationEnvelope envelope, CancellationToken cancellationToken = default);

    // AGENTS.md: "Reserve the change before the first edit." Marks nodeId as reserved by
    // this worker on the given branch/worktree so no other worker edits it concurrently.
    // Returns the node with reservation recorded and status moved to InProgress; a
    // reservation conflict (node already reserved by another worker) returns Conflict.
    Task<MutationResult<Node>> ReserveWorkItemAsync(NodeId nodeId, ActorRef worker, string? branch, string? worktree, MutationEnvelope envelope, CancellationToken cancellationToken = default);

    // Begin a work activity on a node: records an ActivityLogEntry (activity id in the
    // workbook's "ACT-..." scheme) and returns it. The same node can have sequential
    // activities; a concurrently-live activity is a Conflict.
    Task<MutationResult<ActivityLogEntry>> StartActivityAsync(NodeId nodeId, ActorRef worker, string operation, MutationEnvelope envelope, CancellationToken cancellationToken = default);

    // Extend a live activity's heartbeat so its lease/expiry does not lapse.
    Task<MutationResult<ActivityLogEntry>> RecordHeartbeatAsync(string activityId, MutationEnvelope envelope, CancellationToken cancellationToken = default);

    // Finish an activity successfully, recording result/evidence and completion time.
    Task<MutationResult<ActivityLogEntry>> CompleteActivityAsync(string activityId, string result, string? evidence, MutationEnvelope envelope, CancellationToken cancellationToken = default);

    // Finish an activity as failed, recording the error code and message.
    Task<MutationResult<ActivityLogEntry>> FailActivityAsync(string activityId, string errorCode, string errorMessage, MutationEnvelope envelope, CancellationToken cancellationToken = default);

    // Release a reservation made by ReserveWorkItemAsync (worker gave the work up).
    Task<MutationResult<Node>> ReleaseReservationAsync(NodeId nodeId, MutationEnvelope envelope, CancellationToken cancellationToken = default);

    // Mark a work item fully complete: AGENTS.md requires a recorded human review and
    // verified green integration before completion. Returns the node with status
    // Completed and the evidence recorded.
    Task<MutationResult<Node>> CompleteWorkItemAsync(NodeId nodeId, string resultOrEvidence, MutationEnvelope envelope, CancellationToken cancellationToken = default);

    // Compare a mandatory-preflight declaration against every change whose status is not
    // Completed or Cancelled and return exactly one verdict plus named findings.
    // (A check, not a mutation: no envelope.)
    Task<PreflightResult> RunPreflightAsync(PreflightDeclaration declaration, CancellationToken cancellationToken = default);

    // Every change whose status is not Completed/Cancelled (the same set preflight
    // compares against), newest first.
    Task<IReadOnlyList<ActiveChange>> GetActiveChangesAsync(CancellationToken cancellationToken = default);

    // The next work item to execute: a Ready/Planned WorkItem (or Task/Subtask) whose
    // blocking Dependencies are complete, whose parent is BreakdownComplete, and that no
    // other worker holds reserved -- or null when nothing is executable.
    Task<Node?> GetNextExecutableWorkItemAsync(CancellationToken cancellationToken = default);

    // The append-only Activity Log, newest first.
    Task<IReadOnlyList<ActivityLogEntry>> GetActivityLogAsync(CancellationToken cancellationToken = default);

    // Run the store's consistency invariants (parent resolution, one current version per
    // NodeId, dependency resolution, SortKey uniqueness among siblings, ...) and report
    // problems. A check, not a mutation.
    Task<ValidationResult> ValidateControlStoreAsync(CancellationToken cancellationToken = default);
}
