using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Nexus.Developer.Core.DevelopmentControl;

namespace Nexus.Developer.Infrastructure.DevelopmentControl;

// WI-07-0.2.3: the Excel persistence adapter -- the IDevelopmentControlStore implemented
// directly against the canonical NEXUS_DEVELOPMENT_CONTROL workbook, on top of the
// existing ClosedXML integration in this project.
//
// Ground truth (read-only probes of the live workbook, 2026-08-30):
//   Master Roadmap   header row 5, data from row 6, columns A..AG  (27 node columns A..AA)
//   Version History  header row 5, data from row 6, columns A..AJ  (node columns A..Y, then
//                     Z Baseline, AA Record Version, AB Effective From (OADate), AC Is Current,
//                     AD Change ID, AE Supersedes, AF Source, AG Notes, AH Change Type,
//                     AI Change Summary, AJ ADR/Decision Link)
//   Active Changes   header row 5, data from row 6, columns A..AD
//   Audit Findings   header row 5, data from row 6, columns A..M
//   Activity Log     header row 4, data from row 5, 34 columns A..AH (WI-07-0.2.2)
//   Control Center   narrative changelog in merged cell A2; the manual summary values
//                    (column L) are stale and are never trusted -- counts below are
//                    recomputed from the source sheets (ADR-005 map mandatory rules).
//
// WI-07-0.2.4 (scope-amended CHG-20260830-017) adds the atomic work-unit integration point
// this adapter was blocked on: IDevelopmentControlAtomicWorkUnitRunner executes a governed
// multi-operation work unit as ONE atomic save (open once -> N mutations against one
// in-memory workbook -> append Version History / Activity Log -> single temp-write ->
// validate -> atomic-replace). Single-operation behavior is unchanged; every existing call
// still runs through the original per-op atomic save.
public sealed class ExcelDevelopmentControlStore : IDevelopmentControlStore, IDevelopmentControlAtomicWorkUnitRunner
{
    private const string SheetMasterRoadmap = "Master Roadmap";
    private const string SheetVersionHistory = "Version History";
    private const string SheetActiveChanges = "Active Changes";
    private const string SheetAuditFindings = "Audit Findings";
    private const string SheetActivityLog = "Activity Log";
    private const string SheetControlCenter = "Control Center";

    private const int MasterRoadmapHeaderRow = 5;
    private const int MasterRoadmapDataStart = 6;
    private const int VersionHistoryHeaderRow = 5;
    private const int VersionHistoryDataStart = 6;
    private const int ActiveChangesHeaderRow = 5;
    private const int ActiveChangesDataStart = 6;
    private const int AuditFindingsHeaderRow = 5;
    private const int AuditFindingsDataStart = 6;
    private const int ActivityLogHeaderRow = 4;
    private const int ActivityLogDataStart = 5;
    private const int ControlCenterNarrativeRow = 2;

    private readonly string _workbookPath;

    // When a governed atomic work unit is executing (ExecuteAtomicWorkUnitAsync), this holds
    // the ONE open workbook shared by every operation in the unit. Mutations run against its
    // in-memory sheets (no per-op save) and reads observe them, so multiple governed
    // operations compose against a single in-memory workbook state that is saved exactly once
    // if -- and only if -- the whole unit succeeds. Null outside a work-unit execution.
    private IXLWorkbook? _batchWorkbook;

    public ExcelDevelopmentControlStore(string workbookPath)
    {
        if (workbookPath is null) throw new ArgumentNullException(nameof(workbookPath));
        if (!File.Exists(workbookPath)) throw new FileNotFoundException("The development-control workbook was not found.", workbookPath);
        _workbookPath = workbookPath;
    }

    // The canonical workbook this store reads and writes. Mutations are verified against
    // temp copies only -- this instance is constructed over whatever path is supplied.
    public string WorkbookPath => _workbookPath;

    // ------------------------------------------------------------------ shared read helper

    // Runs a read against the current authoritative state. Inside an atomic work unit
    // (_batchWorkbook set) that state is the shared in-memory workbook, so a work unit sees
    // its own prior writes; otherwise a fresh read opens its own workbook and disposes it.
    private TResult WithSnapshotRead<TResult>(CancellationToken ct, Func<WorkbookSnapshot, TResult> read)
    {
        if (_batchWorkbook is not null)
            return read(WorkbookSnapshot.Load(_batchWorkbook));
        using var workbook = Open(ct);
        return read(WorkbookSnapshot.Load(workbook));
    }

    // ------------------------------------------------------------------ reads

    public Task<ControlState?> GetControlStateAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<ControlState?>(GetControlStateCore(cancellationToken));

    public Task<Node?> GetNodeAsync(NodeId nodeId, CancellationToken cancellationToken = default)
        => Task.FromResult(GetNode(cancellationToken, nodeId));

    public Task<IReadOnlyList<Node>> GetSubtreeAsync(NodeId rootNodeId, CancellationToken cancellationToken = default)
        => Task.FromResult(GetSubtreeCore(cancellationToken, rootNodeId));

    public Task<IReadOnlyList<Node>> SearchNodesAsync(NodeSearchCriteria criteria, CancellationToken cancellationToken = default)
        => Task.FromResult(SearchNodesCore(cancellationToken, criteria));

    public Task<PreflightResult> RunPreflightAsync(PreflightDeclaration declaration, CancellationToken cancellationToken = default)
        => Task.FromResult(RunPreflightCore(cancellationToken, declaration));

    public Task<IReadOnlyList<ActiveChange>> GetActiveChangesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(GetActiveChangesCore(cancellationToken));

    public Task<Node?> GetNextExecutableWorkItemAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(GetNextExecutableWorkItemCore(cancellationToken));

    public Task<IReadOnlyList<ActivityLogEntry>> GetActivityLogAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(GetActivityLogCore(cancellationToken));

    public Task<ValidationResult> ValidateControlStoreAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(ValidateControlStoreCore(cancellationToken));

    // ------------------------------------------------------------------ mutations

    public Task<MutationResult<Node>> CreateNodeAsync(Node node, MutationEnvelope envelope, CancellationToken cancellationToken = default)
        => Task.FromResult(RunMutation<Node>(workbook =>
        {
            var snap = WorkbookSnapshot.Load(workbook);
            var errors = ValidateEnvelope(envelope);
            if (envelope.ExpectedRowVersion is not null)
                errors.Add("ExpectedRowVersion must be null when creating a node (there is no previous version to race).");
            if (FindMasterRoadmapRow(snap, node.NodeId.Value) != 0)
                errors.Add($"Node '{node.NodeId.Value}' already exists as a current row.");
            if (errors.Count > 0) return new MutationResult<Node>(false, null, false, null, errors, null);

            var now = DateTimeOffset.UtcNow;
            var desired = node with { RowVersion = 1, IsDeleted = false, CreatedAt = now, UpdatedAt = now };
            desired = EnsureSortKeyAndPath(snap, desired);
            var (resultNode, activityId) = ApplyNodeWrite(snap, null, desired, envelope,
                "Create Node", "Create",
                $"Created node {desired.NodeId.Value} ({DevelopmentControlCellCodec.NodeTypeToText(desired.NodeType)}) '{desired.Name}'.");
            return new MutationResult<Node>(true, resultNode, false, null, Array.Empty<string>(), activityId);
        }, cancellationToken));

    public Task<MutationResult<Node>> UpdateNodeAsync(Node updatedNode, MutationEnvelope envelope, CancellationToken cancellationToken = default)
        => Task.FromResult(RunMutation<Node>(workbook =>
        {
            var snap = WorkbookSnapshot.Load(workbook);
            var current = GetNodeCore(snap, updatedNode.NodeId);
            if (current is null)
                return new MutationResult<Node>(false, null, false, null,
                    new[] { $"No current version of node '{updatedNode.NodeId.Value}' to update." }, null);
            var desired = updatedNode with
            {
                RowVersion = current.RowVersion + 1,
                CreatedAt = current.CreatedAt,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            return ApplyNodeMutation(snap, current, desired, envelope, "Update Node", "Update",
                $"Updated node {desired.NodeId.Value}.");
        }, cancellationToken));

    public Task<MutationResult<Node>> ReparentNodeAsync(NodeId nodeId, NodeId? newParentId, MutationEnvelope envelope, CancellationToken cancellationToken = default)
        => Task.FromResult(RunMutation<Node>(workbook =>
        {
            var snap = WorkbookSnapshot.Load(workbook);
            var current = GetNodeCore(snap, nodeId);
            if (current is null)
                return new MutationResult<Node>(false, null, false, null, new[] { $"No current version of node '{nodeId.Value}' to reparent." }, null);
            if (newParentId is not null)
            {
                if (newParentId.Value.Value == nodeId.Value)
                    return new MutationResult<Node>(false, null, false, null, new[] { "A node cannot be its own parent." }, null);
                if (GetNodeCore(snap, newParentId.Value) is null)
                    return new MutationResult<Node>(false, null, false, null, new[] { $"New parent '{newParentId.Value.Value}' does not exist." }, null);
            }
            var desired = current with
            {
                ParentId = newParentId,
                SortKey = GenerateSortKey(snap, newParentId),
                Path = GeneratePath(snap, newParentId, current.Name),
                RowVersion = current.RowVersion + 1,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            // Children are NOT carried in this single-operation adapter: carrying a subtree's
            // SortKey/Path is a multi-node atomic write, explicitly deferred to WI-07-0.2.4.
            return ApplyNodeMutation(snap, current, desired, envelope, "Reparent Node", "Reparent",
                $"Reparented {nodeId.Value} under {newParentId?.Value ?? "(root)"}.");
        }, cancellationToken));

    public Task<MutationResult<Node>> RetireNodeAsync(NodeId nodeId, MutationEnvelope envelope, CancellationToken cancellationToken = default)
        => Task.FromResult(RunMutation<Node>(workbook =>
        {
            var snap = WorkbookSnapshot.Load(workbook);
            var current = GetNodeCore(snap, nodeId);
            if (current is null)
                return new MutationResult<Node>(false, null, false, null, new[] { $"No current version of node '{nodeId.Value}' to retire." }, null);
            var desired = current with
            {
                Status = Status.Superseded,
                IsDeleted = true,
                RowVersion = current.RowVersion + 1,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            // History is never deleted: the new record is appended with Is Current = No.
            return ApplyNodeMutation(snap, current, desired, envelope, "Retire Node", "Retire",
                envelope.Reason ?? $"Retired node {nodeId.Value}.");
        }, cancellationToken));

    public Task<MutationResult<Node>> AddDependencyAsync(NodeId nodeId, NodeId dependencyNodeId, MutationEnvelope envelope, CancellationToken cancellationToken = default)
        => Task.FromResult(RunMutation<Node>(workbook =>
        {
            var snap = WorkbookSnapshot.Load(workbook);
            var current = GetNodeCore(snap, nodeId);
            if (current is null)
                return new MutationResult<Node>(false, null, false, null, new[] { $"No current version of node '{nodeId.Value}'." }, null);
            if (GetNodeCore(snap, dependencyNodeId) is null)
                return new MutationResult<Node>(false, null, false, null, new[] { $"Dependency node '{dependencyNodeId.Value}' does not exist." }, null);
            if (current.Dependencies.Any(d => d.Value == dependencyNodeId.Value))
                return new MutationResult<Node>(true, current, false, null, Array.Empty<string>(), null); // edge already declared: no-op
            var desired = current with
            {
                Dependencies = current.Dependencies.Append(dependencyNodeId).ToArray(),
                RowVersion = current.RowVersion + 1,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            return ApplyNodeMutation(snap, current, desired, envelope, "Add Dependency", "Scope Update",
                $"Added dependency {dependencyNodeId.Value} to {nodeId.Value}.");
        }, cancellationToken));

    public Task<MutationResult<Node>> RemoveDependencyAsync(NodeId nodeId, NodeId dependencyNodeId, MutationEnvelope envelope, CancellationToken cancellationToken = default)
        => Task.FromResult(RunMutation<Node>(workbook =>
        {
            var snap = WorkbookSnapshot.Load(workbook);
            var current = GetNodeCore(snap, nodeId);
            if (current is null)
                return new MutationResult<Node>(false, null, false, null, new[] { $"No current version of node '{nodeId.Value}'." }, null);
            if (!current.Dependencies.Any(d => d.Value == dependencyNodeId.Value))
                return new MutationResult<Node>(true, current, false, null, Array.Empty<string>(), null); // edge not declared: no-op
            var desired = current with
            {
                Dependencies = current.Dependencies.Where(d => d.Value != dependencyNodeId.Value).ToArray(),
                RowVersion = current.RowVersion + 1,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            return ApplyNodeMutation(snap, current, desired, envelope, "Remove Dependency", "Scope Update",
                $"Removed dependency {dependencyNodeId.Value} from {nodeId.Value}.");
        }, cancellationToken));

    public Task<MutationResult<Node>> ReserveWorkItemAsync(NodeId nodeId, ActorRef worker, string? branch, string? worktree, MutationEnvelope envelope, CancellationToken cancellationToken = default)
        => Task.FromResult(RunMutation<Node>(workbook =>
        {
            var snap = WorkbookSnapshot.Load(workbook);
            var current = GetNodeCore(snap, nodeId);
            if (current is null)
                return new MutationResult<Node>(false, null, false, null, new[] { $"No current version of node '{nodeId.Value}' to reserve." }, null);
            var errors = ValidateEnvelope(envelope);
            if (errors.Count > 0) return new MutationResult<Node>(false, null, false, null, errors, null);

            // A reservation conflict is a state conflict (another worker already owns the
            // change), reported via Conflict with the conflicting ActiveChange as details.
            var conflict = ReadActiveChanges(snap)
                .Select(t => t.Change)
                .FirstOrDefault(c => IsOpenChange(c) && (c.NodeId == nodeId.Value || c.AffectedNodes.Contains(nodeId.Value)));
            if (conflict is not null)
                return new MutationResult<Node>(false, null, true, conflict, Array.Empty<string>(), null);

            var now = DateTimeOffset.UtcNow;
            var changeRow = NextFreeRow(snap.ActiveChanges, ActiveChangesDataStart);
            WriteActiveChangeRow(snap, changeRow, new ActiveChange(
                ChangeId: envelope.ChangeId!,
                NodeId: nodeId.Value,
                MilestoneOrFeature: "",
                Summary: $"Reserved for implementation by {worker.Name}.",
                RequestedBy: "",
                Worker: worker.Name,
                Repositories: Array.Empty<string>(),
                Projects: current.Projects,
                FilesGlobs: current.FilesGlobs,
                SchemaContexts: current.SchemaContexts,
                ContractsApis: current.ContractsApis,
                Status: "Open -- reserved",
                PreflightVerdict: PreflightVerdict.Clear,
                ConflictsWith: "",
                DependencyOn: "",
                Risk: current.Risk ?? "",
                Branch: branch ?? "",
                Worktree: worktree ?? "",
                StartedAt: now,
                LastHeartbeat: now,
                CompletedAt: null,
                ResultOrEvidence: "",
                ChangeVersion: "1.0",
                SessionOrChat: envelope.SessionId ?? "",
                Notes: envelope.Reason ?? "",
                VersionHistoryId: "",
                AdrId: "",
                AffectedNodes: new[] { nodeId.Value },
                ChangeType: "Implementation",
                ValidationResult: "Pending -- reserved; implementation not started"));

            // Reserving moves the node to In Progress -- a governed fact change, so it gets a
            // new Version History record and an Activity Log entry alongside the ledger row.
            var desired = current with { Status = Status.InProgress, RowVersion = current.RowVersion + 1, UpdatedAt = now };
            var (resultNode, activityId) = ApplyNodeWrite(snap, current, desired, envelope, "Reserve Work Item", "Reserve",
                $"Reserved {nodeId.Value} for {worker.Name} on {branch ?? "(no branch)"} / {worktree ?? "(no worktree)"}.");
            return new MutationResult<Node>(true, resultNode, false, null, Array.Empty<string>(), activityId);
        }, cancellationToken));

    public Task<MutationResult<ActivityLogEntry>> StartActivityAsync(NodeId nodeId, ActorRef worker, string operation, MutationEnvelope envelope, CancellationToken cancellationToken = default)
        => Task.FromResult(RunMutation<ActivityLogEntry>(workbook =>
        {
            var snap = WorkbookSnapshot.Load(workbook);
            var errors = ValidateEnvelope(envelope);
            if (string.IsNullOrWhiteSpace(operation)) errors.Add("operation is required.");
            if (GetNodeCore(snap, nodeId) is null) errors.Add($"Node '{nodeId.Value}' does not exist; an activity must reference a roadmap node.");
            if (errors.Count > 0) return new MutationResult<ActivityLogEntry>(false, null, false, null, errors, null);

            var now = DateTimeOffset.UtcNow;
            var entry = BuildActivityEntry(NextActivityId(snap, now), operation, "Node", nodeId.Value,
                current: null, updated: null, envelope: envelope, actor: worker);
            var id = AppendActivityLog(snap, entry);
            // WI-07-0.2.4 owns the "concurrently-live activity is a Conflict" liveness check;
            // this adapter records the activity without it (documented boundary).
            return new MutationResult<ActivityLogEntry>(true, entry, false, null, Array.Empty<string>(), id);
        }, cancellationToken));

    public Task<MutationResult<ActivityLogEntry>> RecordHeartbeatAsync(string activityId, MutationEnvelope envelope, CancellationToken cancellationToken = default)
        => Task.FromResult(RunMutation<ActivityLogEntry>(workbook =>
        {
            var snap = WorkbookSnapshot.Load(workbook);
            var source = FindActivityById(snap, activityId);
            if (source is null)
                return new MutationResult<ActivityLogEntry>(false, null, false, null, new[] { $"No activity '{activityId}' in the log to heartbeat." }, null);
            var errors = ValidateEnvelope(envelope);
            if (errors.Count > 0) return new MutationResult<ActivityLogEntry>(false, null, false, null, errors, null);

            var now = DateTimeOffset.UtcNow;
            // The Activity Log is append-only (WI-07-0.2.2): a heartbeat is recorded as a NEW
            // event row referencing the source activity, never an in-place update of a row.
            var entry = BuildActivityEntry(NextActivityId(snap, now), "Heartbeat", "Activity", activityId,
                current: null, updated: null, envelope: envelope, result: "Heartbeat");
            var id = AppendActivityLog(snap, entry);
            return new MutationResult<ActivityLogEntry>(true, entry, false, null, Array.Empty<string>(), id);
        }, cancellationToken));

    public Task<MutationResult<ActivityLogEntry>> CompleteActivityAsync(string activityId, string result, string? evidence, MutationEnvelope envelope, CancellationToken cancellationToken = default)
        => Task.FromResult(RunMutation<ActivityLogEntry>(workbook =>
        {
            var snap = WorkbookSnapshot.Load(workbook);
            var source = FindActivityById(snap, activityId);
            if (source is null)
                return new MutationResult<ActivityLogEntry>(false, null, false, null, new[] { $"No activity '{activityId}' in the log to complete." }, null);
            var errors = ValidateEnvelope(envelope);
            if (errors.Count > 0) return new MutationResult<ActivityLogEntry>(false, null, false, null, errors, null);

            var now = DateTimeOffset.UtcNow;
            var duration = now - source.TimestampUtc;
            var entry = BuildActivityEntry(NextActivityId(snap, now), "Complete Activity", "Activity", activityId,
                current: null, updated: null, envelope: envelope, result: result, evidence: evidence, duration: duration,
                afterValue: "Completed " + activityId);
            var id = AppendActivityLog(snap, entry);
            return new MutationResult<ActivityLogEntry>(true, entry, false, null, Array.Empty<string>(), id);
        }, cancellationToken));

    public Task<MutationResult<ActivityLogEntry>> FailActivityAsync(string activityId, string errorCode, string errorMessage, MutationEnvelope envelope, CancellationToken cancellationToken = default)
        => Task.FromResult(RunMutation<ActivityLogEntry>(workbook =>
        {
            var snap = WorkbookSnapshot.Load(workbook);
            var source = FindActivityById(snap, activityId);
            if (source is null)
                return new MutationResult<ActivityLogEntry>(false, null, false, null, new[] { $"No activity '{activityId}' in the log to fail." }, null);
            var errors = ValidateEnvelope(envelope);
            if (errors.Count > 0) return new MutationResult<ActivityLogEntry>(false, null, false, null, errors, null);

            var now = DateTimeOffset.UtcNow;
            var entry = BuildActivityEntry(NextActivityId(snap, now), "Fail Activity", "Activity", activityId,
                current: null, updated: null, envelope: envelope, errorCode: errorCode, errorMessage: errorMessage,
                afterValue: "Failed " + activityId);
            var id = AppendActivityLog(snap, entry);
            return new MutationResult<ActivityLogEntry>(true, entry, false, null, Array.Empty<string>(), id);
        }, cancellationToken));

    public Task<MutationResult<Node>> ReleaseReservationAsync(NodeId nodeId, MutationEnvelope envelope, CancellationToken cancellationToken = default)
        => Task.FromResult(RunMutation<Node>(workbook =>
        {
            var snap = WorkbookSnapshot.Load(workbook);
            var current = GetNodeCore(snap, nodeId);
            if (current is null)
                return new MutationResult<Node>(false, null, false, null, new[] { $"No current version of node '{nodeId.Value}'." }, null);
            var errors = ValidateEnvelope(envelope);
            if (errors.Count > 0) return new MutationResult<Node>(false, null, false, null, errors, null);

            var changes = ReadActiveChanges(snap);
            var match = changes.LastOrDefault(t => IsOpenChange(t.Change)
                && (t.Change.NodeId == nodeId.Value || t.Change.AffectedNodes.Contains(nodeId.Value))
                && (envelope.ChangeId is null || t.Change.ChangeId == envelope.ChangeId));
            if (match.Row == 0)
                return new MutationResult<Node>(false, null, false, null,
                    new[] { $"No open reservation for node '{nodeId.Value}' to release." }, null);

            var released = match.Change with
            {
                Status = "Released -- " + (envelope.Reason ?? "worker gave the work up"),
                Notes = match.Change.Notes.Length == 0 ? (envelope.Reason ?? "") : match.Change.Notes,
            };
            WriteActiveChangeRow(snap, match.Row, released);
            var now = DateTimeOffset.UtcNow;
            var activityId = AppendActivityLog(snap, BuildActivityEntry(NextActivityId(snap, now), "Release Reservation", "Change",
                match.Change.ChangeId, current, current, envelope, result: "Released reservation " + match.Change.ChangeId));
            return new MutationResult<Node>(true, current, false, null, Array.Empty<string>(), activityId);
        }, cancellationToken));

    public Task<MutationResult<Node>> CompleteWorkItemAsync(NodeId nodeId, string resultOrEvidence, MutationEnvelope envelope, CancellationToken cancellationToken = default)
        => Task.FromResult(RunMutation<Node>(workbook =>
        {
            var snap = WorkbookSnapshot.Load(workbook);
            var current = GetNodeCore(snap, nodeId);
            if (current is null)
                return new MutationResult<Node>(false, null, false, null, new[] { $"No current version of node '{nodeId.Value}' to complete." }, null);

            var desired = current with
            {
                Status = Status.Completed,
                Notes = AppendNote(current.Notes, resultOrEvidence),
                RowVersion = current.RowVersion + 1,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            var result = ApplyNodeMutation(snap, current, desired, envelope, "Complete Work Item", "Complete", resultOrEvidence);
            if (result.Success)
            {
                // Keep the ledger consistent: mark the matching open reservation row Completed
                // in the same atomic save.
                var changes = ReadActiveChanges(snap);
                foreach (var (row, change) in changes)
                {
                    if (IsOpenChange(change) && (change.NodeId == nodeId.Value || change.AffectedNodes.Contains(nodeId.Value)))
                    {
                        WriteActiveChangeRow(snap, row, change with
                        {
                            Status = "Completed",
                            CompletedAt = DateTimeOffset.UtcNow,
                            ResultOrEvidence = change.ResultOrEvidence.Length == 0 ? resultOrEvidence : change.ResultOrEvidence,
                        });
                        break;
                    }
                }
            }
            return result;
        }, cancellationToken));

    // ------------------------------------------------------------- infrastructure

    private XLWorkbook Open(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workbook = new XLWorkbook(_workbookPath);
        // Schema gate: refuse to operate on a workbook missing a tracked sheet.
        WorkbookSchemaValidator.Validate(workbook);
        return workbook;
    }

    private static IXLWorksheet Sheet(IXLWorkbook workbook, string name) =>
        workbook.Worksheets.TryGetWorksheet(name, out var sheet)
            ? sheet
            : throw new InvalidOperationException($"Required sheet '{name}' is missing from the development-control workbook.");

    private static WorksheetColumnMap Map(IXLWorksheet sheet, int headerRow) => new(sheet, headerRow);

    private static int NextFreeRow(IXLWorksheet sheet, int dataStart)
    {
        var lastUsed = sheet.LastRowUsed()?.RowNumber() ?? dataStart - 1;
        return Math.Max(lastUsed + 1, dataStart);
    }

    // Saves to a temp file in the same directory, then moves it over the canonical path.
    // The move is a same-volume replace, so a failure at any point leaves the canonical
    // workbook untouched -- no partial governed change is ever visible.
    private string SaveToTemp(XLWorkbook workbook)
    {
        var directory = Path.GetDirectoryName(_workbookPath) ?? ".";
        var tempPath = Path.Combine(directory, Path.GetRandomFileName() + ".xlsx");
        try { workbook.SaveAs(tempPath); }
        catch { try { File.Delete(tempPath); } catch { /* best effort */ } throw; }
        return tempPath;
    }

    private void CommitTemp(string tempPath)
    {
        try
        {
            File.Move(tempPath, _workbookPath, overwrite: true);
        }
        catch
        {
            try { File.Delete(tempPath); } catch { /* best effort */ }
            throw;
        }
    }

    private MutationResult<T> RunMutation<T>(Func<IXLWorkbook, MutationResult<T>> mutate, CancellationToken cancellationToken) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            // Atomic work-unit mode: the shared open workbook was provided by the batch entry.
            // The mutation runs against its in-memory sheets -- WorkbookSnapshot.Load inside
            // each mutation delegate re-reads the LIVE sheets, so every operation sees the
            // prior operations' in-memory writes and appends its Version History / Activity
            // Log evidence to the same open state. No per-operation save happens; the batch
            // entry performs the single temp-write -> validate -> atomic-replace only when the
            // whole unit succeeded.
            if (_batchWorkbook is not null)
                return mutate(_batchWorkbook);

            MutationResult<T> result;
            string? tempPath = null;
            using (var workbook = Open(cancellationToken))
            {
                result = mutate(workbook);
                if (result.Success)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    tempPath = SaveToTemp(workbook);
                }
            }
            if (tempPath is not null) CommitTemp(tempPath);
            return result;
        }
        catch (InvalidOperationException ex)
        {
            // A schema/data anomaly is exposed as a failed mutation with no partial governed
            // change (the canonical workbook was never written).
            return new MutationResult<T>(false, default, false, null, new[] { ex.Message }, null);
        }
    }

    // ------------------------------------------------------------------ atomic work unit

    // Executes a governed multi-operation work unit as ONE atomic save (WI-07-0.2.4,
    // scope-amended). Invoked by the Core coordinator/guard while the named cross-process
    // writer lock is held. Sequence:
    //   (a) open the workbook ONCE (schema gate);
    //   (b) pre-verify the expected RowVersion against the opened persisted state -- NotFound
    //       / ConcurrencyConflict abort before any save;
    //   (c) run every store operation the work unit calls against the SAME in-memory sheets,
    //       appending Version History / Activity Log evidence to that shared state;
    //   (d) when the whole unit succeeded, save ONCE to a temp file, re-open + validate it
    //       through the schema gate, and atomically replace the canonical target.
    // If ANY operation fails the unit aborts: no temp save, no promote, and the in-memory
    // mutations are discarded with the open workbook -- the canonical workbook is untouched.
    public Task<AtomicWriteResult<T>> ExecuteAtomicWorkUnitAsync<T>(
        Func<IDevelopmentControlStore, Task<MutationResult<T>>> workUnit,
        MutationEnvelope? envelope,
        string? verifyEntityNodeId,
        CancellationToken cancellationToken = default) where T : class
    {
        if (workUnit is null) throw new ArgumentNullException(nameof(workUnit));
        cancellationToken.ThrowIfCancellationRequested();

        var previousBatch = _batchWorkbook;
        try
        {
            string? tempPath = null;
            MutationResult<T>? outcome = null;
            using (var workbook = Open(cancellationToken))
            {
                _batchWorkbook = workbook;
                try
                {
                    outcome = RunAtomicWorkUnitCore<T>(workbook, workUnit, envelope, verifyEntityNodeId);
                    if (outcome.Success)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        tempPath = SaveToTemp(workbook);
                    }
                }
                finally
                {
                    _batchWorkbook = previousBatch;
                }
            }

            if (tempPath is not null) ValidateTempAndCommit(tempPath);
            return Task.FromResult(AtomicWriteResult<T>.FromMutation(outcome!, null));
        }
        catch (AtomicWritePreconditionException precondition)
        {
            return Task.FromResult(new AtomicWriteResult<T>(
                precondition.Outcome, false, default, precondition.ConflictDetails,
                precondition.Outcome == DevelopmentControlConcurrencyOutcome.ConcurrencyConflict
                    ? Array.Empty<string>()
                    : new[] { $"No current version of node '{verifyEntityNodeId}'." },
                null, null));
        }
        catch (InvalidOperationException ex)
        {
            // A schema/data anomaly (e.g. the temp re-validation gate) is exposed as a failed
            // atomic write with the canonical workbook untouched.
            return Task.FromResult(new AtomicWriteResult<T>(
                DevelopmentControlConcurrencyOutcome.ValidationFailure, false, default, null,
                new[] { ex.Message }, null, null));
        }
    }

    private MutationResult<T> RunAtomicWorkUnitCore<T>(
        IXLWorkbook workbook,
        Func<IDevelopmentControlStore, Task<MutationResult<T>>> workUnit,
        MutationEnvelope? envelope,
        string? verifyEntityNodeId) where T : class
    {
        var snap = WorkbookSnapshot.Load(workbook);

        // (b) Optimistic RowVersion pre-verification against the opened persisted state this
        //     same save will replace, inside the named writer lock held by the caller. A
        //     missing entity is NotFound; a stale ExpectedRowVersion is a controlled
        //     ConcurrencyConflict carrying the current node -- the unit aborts, no save.
        if (verifyEntityNodeId is not null && envelope?.ExpectedRowVersion is not null)
        {
            var current = GetNodeCore(snap, new NodeId(verifyEntityNodeId));
            if (current is null)
                throw new AtomicWritePreconditionException(DevelopmentControlConcurrencyOutcome.NotFound, null);
            if (envelope.ExpectedRowVersion != current.RowVersion)
                throw new AtomicWritePreconditionException(DevelopmentControlConcurrencyOutcome.ConcurrencyConflict, current);
        }

        // (c) Every store operation the work unit calls runs against the SAME open workbook:
        //     mutations via RunMutation's batch branch (no per-op save), reads via the batch
        //     read branches. The unit's final result decides whether the batch entry saves.
        var facade = new SnapshotBoundDevelopmentControlStore(this);
        return workUnit(facade).GetAwaiter().GetResult();
    }

    // Re-opens the saved temp workbook through the same schema gate the adapter applies at
    // Open, then atomically replaces the canonical target. A structurally-invalid temp is
    // deleted and the canonical workbook is never replaced -- the temp-write -> validate ->
    // atomic-replace principle, enforced before promotion.
    private void ValidateTempAndCommit(string tempPath)
    {
        try
        {
            using (var temp = new XLWorkbook(tempPath))
            {
                WorkbookSchemaValidator.Validate(temp);
            }
            CommitTemp(tempPath);
        }
        catch
        {
            try { File.Delete(tempPath); } catch { /* best effort */ }
            throw;
        }
    }

    // Signals a controlled precondition failure (NotFound / ConcurrencyConflict) during the
    // optimistic RowVersion pre-verification of an atomic work unit.
    private sealed class AtomicWritePreconditionException : Exception
    {
        public AtomicWritePreconditionException(DevelopmentControlConcurrencyOutcome outcome, object? conflictDetails)
            : base($"Atomic write precondition failed: {outcome}.") { Outcome = outcome; ConflictDetails = conflictDetails; }

        public DevelopmentControlConcurrencyOutcome Outcome { get; }
        public object? ConflictDetails { get; }
    }

    // A minimal IDevelopmentControlStore facade handed to an atomic work unit. It is bound to
    // the outer store while ExecuteAtomicWorkUnitAsync holds the ONE open workbook, so every
    // call -- read or mutate -- operates on that shared in-memory state and the batch entry
    // saves exactly once. The facade itself carries no state; the batch-mode branches on the
    // outer store provide the single-save semantics.
    private sealed class SnapshotBoundDevelopmentControlStore : IDevelopmentControlStore
    {
        private readonly ExcelDevelopmentControlStore _outer;

        public SnapshotBoundDevelopmentControlStore(ExcelDevelopmentControlStore outer) => _outer = outer;

        public Task<ControlState?> GetControlStateAsync(CancellationToken ct = default) => _outer.GetControlStateAsync(ct);
        public Task<Node?> GetNodeAsync(NodeId nodeId, CancellationToken ct = default) => _outer.GetNodeAsync(nodeId, ct);
        public Task<IReadOnlyList<Node>> GetSubtreeAsync(NodeId rootNodeId, CancellationToken ct = default) => _outer.GetSubtreeAsync(rootNodeId, ct);
        public Task<IReadOnlyList<Node>> SearchNodesAsync(NodeSearchCriteria criteria, CancellationToken ct = default) => _outer.SearchNodesAsync(criteria, ct);
        public Task<PreflightResult> RunPreflightAsync(PreflightDeclaration declaration, CancellationToken ct = default) => _outer.RunPreflightAsync(declaration, ct);
        public Task<IReadOnlyList<ActiveChange>> GetActiveChangesAsync(CancellationToken ct = default) => _outer.GetActiveChangesAsync(ct);
        public Task<Node?> GetNextExecutableWorkItemAsync(CancellationToken ct = default) => _outer.GetNextExecutableWorkItemAsync(ct);
        public Task<IReadOnlyList<ActivityLogEntry>> GetActivityLogAsync(CancellationToken ct = default) => _outer.GetActivityLogAsync(ct);
        public Task<ValidationResult> ValidateControlStoreAsync(CancellationToken ct = default) => _outer.ValidateControlStoreAsync(ct);

        public Task<MutationResult<Node>> CreateNodeAsync(Node node, MutationEnvelope envelope, CancellationToken ct = default) => _outer.CreateNodeAsync(node, envelope, ct);
        public Task<MutationResult<Node>> UpdateNodeAsync(Node updatedNode, MutationEnvelope envelope, CancellationToken ct = default) => _outer.UpdateNodeAsync(updatedNode, envelope, ct);
        public Task<MutationResult<Node>> ReparentNodeAsync(NodeId nodeId, NodeId? newParentId, MutationEnvelope envelope, CancellationToken ct = default) => _outer.ReparentNodeAsync(nodeId, newParentId, envelope, ct);
        public Task<MutationResult<Node>> RetireNodeAsync(NodeId nodeId, MutationEnvelope envelope, CancellationToken ct = default) => _outer.RetireNodeAsync(nodeId, envelope, ct);
        public Task<MutationResult<Node>> AddDependencyAsync(NodeId nodeId, NodeId dependencyNodeId, MutationEnvelope envelope, CancellationToken ct = default) => _outer.AddDependencyAsync(nodeId, dependencyNodeId, envelope, ct);
        public Task<MutationResult<Node>> RemoveDependencyAsync(NodeId nodeId, NodeId dependencyNodeId, MutationEnvelope envelope, CancellationToken ct = default) => _outer.RemoveDependencyAsync(nodeId, dependencyNodeId, envelope, ct);
        public Task<MutationResult<Node>> ReserveWorkItemAsync(NodeId nodeId, ActorRef worker, string? branch, string? worktree, MutationEnvelope envelope, CancellationToken ct = default) => _outer.ReserveWorkItemAsync(nodeId, worker, branch, worktree, envelope, ct);
        public Task<MutationResult<ActivityLogEntry>> StartActivityAsync(NodeId nodeId, ActorRef worker, string operation, MutationEnvelope envelope, CancellationToken ct = default) => _outer.StartActivityAsync(nodeId, worker, operation, envelope, ct);
        public Task<MutationResult<ActivityLogEntry>> RecordHeartbeatAsync(string activityId, MutationEnvelope envelope, CancellationToken ct = default) => _outer.RecordHeartbeatAsync(activityId, envelope, ct);
        public Task<MutationResult<ActivityLogEntry>> CompleteActivityAsync(string activityId, string result, string? evidence, MutationEnvelope envelope, CancellationToken ct = default) => _outer.CompleteActivityAsync(activityId, result, evidence, envelope, ct);
        public Task<MutationResult<ActivityLogEntry>> FailActivityAsync(string activityId, string errorCode, string errorMessage, MutationEnvelope envelope, CancellationToken ct = default) => _outer.FailActivityAsync(activityId, errorCode, errorMessage, envelope, ct);
        public Task<MutationResult<Node>> ReleaseReservationAsync(NodeId nodeId, MutationEnvelope envelope, CancellationToken ct = default) => _outer.ReleaseReservationAsync(nodeId, envelope, ct);
        public Task<MutationResult<Node>> CompleteWorkItemAsync(NodeId nodeId, string resultOrEvidence, MutationEnvelope envelope, CancellationToken ct = default) => _outer.CompleteWorkItemAsync(nodeId, resultOrEvidence, envelope, ct);
    }

    private static List<string> ValidateEnvelope(MutationEnvelope envelope)
    {
        var errors = new List<string>();
        if (envelope.Actor is null) errors.Add("envelope.Actor is required.");
        else if (string.IsNullOrWhiteSpace(envelope.Actor.Name)) errors.Add("envelope.Actor.Name is required.");
        if (string.IsNullOrWhiteSpace(envelope.Source)) errors.Add("envelope.Source is required.");
        if (string.IsNullOrWhiteSpace(envelope.ChangeId)) errors.Add("envelope.ChangeId is required (CHG-...) for a governed write.");
        return errors;
    }

    // ------------------------------------------------------------------ snapshot

    private sealed record WorkbookSnapshot(
        IXLWorksheet Master,
        IXLWorksheet VersionHistory,
        IXLWorksheet ActiveChanges,
        IXLWorksheet AuditFindings,
        IXLWorksheet ActivityLog,
        WorksheetColumnMap MasterMap,
        WorksheetColumnMap VersionHistoryMap,
        WorksheetColumnMap ActiveChangesMap,
        WorksheetColumnMap AuditFindingsMap,
        WorksheetColumnMap ActivityLogMap,
        VersionIndex Versions)
    {
        public static WorkbookSnapshot Load(IXLWorkbook workbook)
        {
            var master = Sheet(workbook, SheetMasterRoadmap);
            var versionHistory = Sheet(workbook, SheetVersionHistory);
            var activeChanges = Sheet(workbook, SheetActiveChanges);
            var auditFindings = Sheet(workbook, SheetAuditFindings);
            var activityLog = Sheet(workbook, SheetActivityLog);
            return new WorkbookSnapshot(
                master, versionHistory, activeChanges, auditFindings, activityLog,
                Map(master, MasterRoadmapHeaderRow),
                Map(versionHistory, VersionHistoryHeaderRow),
                Map(activeChanges, ActiveChangesHeaderRow),
                Map(auditFindings, AuditFindingsHeaderRow),
                Map(activityLog, ActivityLogHeaderRow),
                VersionIndex.Build(versionHistory));
        }
    }

    // ------------------------------------------------------------------ version index

    private sealed record NodeVersionInfo(
        int RowVersion,
        string? LatestVersionString,
        int LatestHistoryRow,
        string? BaselineVersion,
        DateTimeOffset? CreatedAt,
        DateTimeOffset? UpdatedAt);

    private sealed class VersionAccumulator
    {
        public int MaxRowVersion = -1;
        public string? MaxVersionString;
        public int MaxRow;
        public string? MaxBaselineVersion;
        public DateTimeOffset? MinEffective;
        public DateTimeOffset? MaxEffective;
    }

    private sealed class VersionIndex
    {
        private readonly IReadOnlyDictionary<string, NodeVersionInfo> _byNodeId;

        private VersionIndex(IReadOnlyDictionary<string, NodeVersionInfo> byNodeId) => _byNodeId = byNodeId;

        public NodeVersionInfo For(string nodeId) =>
            _byNodeId.TryGetValue(nodeId, out var info)
                ? info
                : new NodeVersionInfo(0, null, 0, null, null, null);

        public static VersionIndex Build(IXLWorksheet sheet)
        {
            var map = new WorksheetColumnMap(sheet, VersionHistoryHeaderRow);
            var colNode = map.Required(WorkbookColumns.NodeId);
            var colRecord = map.Required(WorkbookColumns.RecordVersion);
            var colEffective = map.Required(WorkbookColumns.EffectiveFrom);
            var colBaseline = map.Get(WorkbookColumns.BaselineVersion, 26);
            var accumulators = new Dictionary<string, VersionAccumulator>(StringComparer.Ordinal);
            var last = sheet.LastRowUsed()?.RowNumber() ?? VersionHistoryDataStart - 1;
            for (var row = VersionHistoryDataStart; row <= last; row++)
            {
                var nodeId = DevelopmentControlCellCodec.GetString(sheet.Cell(row, colNode));
                if (nodeId.Length == 0) continue;
                var recordVersion = DevelopmentControlCellCodec.GetString(sheet.Cell(row, colRecord));
                var rowVersion = DevelopmentControlCellCodec.ParseRecordVersion(recordVersion);
                var effective = DevelopmentControlCellCodec.GetDateTimeOffset(sheet.Cell(row, colEffective));
                if (!accumulators.TryGetValue(nodeId, out var acc))
                {
                    acc = new VersionAccumulator();
                    accumulators.Add(nodeId, acc);
                }

                if (rowVersion > acc.MaxRowVersion)
                {
                    acc.MaxRowVersion = rowVersion;
                    acc.MaxVersionString = recordVersion;
                    acc.MaxRow = row;
                    acc.MaxBaselineVersion = DevelopmentControlCellCodec.GetString(sheet.Cell(row, colBaseline));
                }
                if (effective.HasValue)
                {
                    acc.MinEffective = acc.MinEffective is null || effective < acc.MinEffective ? effective : acc.MinEffective;
                    acc.MaxEffective = acc.MaxEffective is null || effective > acc.MaxEffective ? effective : acc.MaxEffective;
                }
            }

            // The "previous" version for an append is the LATEST record by Record Version.
            // The workbook does not consistently mark Is Current=Yes on the newest record of
            // actively-developed nodes (observed live: Layer-08 nodes' v2/v3 are marked No),
            // so current-ness is never derived from the Is Current marker -- see ReadNodeAtRow.
            var byNodeId = new Dictionary<string, NodeVersionInfo>(StringComparer.Ordinal);
            foreach (var (nodeId, acc) in accumulators)
            {
                byNodeId.Add(nodeId, new NodeVersionInfo(
                    Math.Max(acc.MaxRowVersion, 0),
                    acc.MaxVersionString,
                    acc.MaxRow,
                    acc.MaxBaselineVersion,
                    acc.MinEffective,
                    acc.MaxEffective));
            }
            return new VersionIndex(byNodeId);
        }
    }

    // ------------------------------------------------------------------ node reads

    private Node? GetNode(CancellationToken cancellationToken, NodeId nodeId)
        => WithSnapshotRead(cancellationToken, snap => GetNodeCore(snap, nodeId));

    private static Node? GetNodeCore(WorkbookSnapshot snap, NodeId nodeId)
    {
        var row = FindMasterRoadmapRow(snap, nodeId.Value);
        if (row == 0) return null;
        var node = ReadNodeAtRow(snap.Master, snap.MasterMap, row, snap.Versions);
        if (node is null || node.IsDeleted) return null;
        return node;
    }

    private static int FindMasterRoadmapRow(WorkbookSnapshot snap, string nodeId)
    {
        var colNode = snap.MasterMap.Required(WorkbookColumns.NodeId);
        var last = snap.Master.LastRowUsed()?.RowNumber() ?? MasterRoadmapDataStart - 1;
        for (var row = MasterRoadmapDataStart; row <= last; row++)
        {
            if (DevelopmentControlCellCodec.GetString(snap.Master.Cell(row, colNode)) == nodeId) return row;
        }
        return 0;
    }

    private static Node? ReadNodeAtRow(IXLWorksheet sheet, WorksheetColumnMap map, int row, VersionIndex versions)
    {
        var nodeIdText = DevelopmentControlCellCodec.GetString(sheet.Cell(row, map.Required(WorkbookColumns.NodeId)));
        if (nodeIdText.Length == 0) return null;
        var nodeId = new NodeId(nodeIdText);
        var info = versions.For(nodeId.Value);
        var parentText = DevelopmentControlCellCodec.GetNullableString(sheet.Cell(row, map.Required(WorkbookColumns.ParentId)));
        var status = DevelopmentControlCellCodec.ParseStatus(DevelopmentControlCellCodec.GetString(sheet.Cell(row, map.Required(WorkbookColumns.Status))));
        return new Node(
            NodeId: nodeId,
            ParentId: parentText is null ? null : new NodeId(parentText),
            NodeType: DevelopmentControlCellCodec.ParseNodeType(DevelopmentControlCellCodec.GetString(sheet.Cell(row, map.Required(WorkbookColumns.NodeType)))),
            SortKey: DevelopmentControlCellCodec.GetString(sheet.Cell(row, map.Required(WorkbookColumns.SortKey))),
            Path: DevelopmentControlCellCodec.GetString(sheet.Cell(row, map.Required(WorkbookColumns.HierarchyPath))),
            Layer: DevelopmentControlCellCodec.GetString(sheet.Cell(row, map.Required(WorkbookColumns.Layer))),
            Phase: DevelopmentControlCellCodec.GetNullableString(sheet.Cell(row, map.Required(WorkbookColumns.Phase))),
            Name: DevelopmentControlCellCodec.GetString(sheet.Cell(row, map.Required(WorkbookColumns.Name))),
            Outcome: DevelopmentControlCellCodec.GetNullableString(sheet.Cell(row, map.Required(WorkbookColumns.OutcomePurpose))),
            Dependencies: DevelopmentControlCellCodec.GetList(sheet.Cell(row, map.Required(WorkbookColumns.Dependencies))).Select(d => new NodeId(d)).ToArray(),
            ParallelSafe: DevelopmentControlCellCodec.GetYesNo(sheet.Cell(row, map.Required(WorkbookColumns.ParallelSafe))),
            Projects: DevelopmentControlCellCodec.GetList(sheet.Cell(row, map.Required(WorkbookColumns.Projects))),
            FilesGlobs: DevelopmentControlCellCodec.GetList(sheet.Cell(row, map.Required(WorkbookColumns.FilesGlobs))),
            SchemaContexts: DevelopmentControlCellCodec.GetList(sheet.Cell(row, map.Required(WorkbookColumns.SchemaContexts))),
            ContractsApis: DevelopmentControlCellCodec.GetList(sheet.Cell(row, map.Required(WorkbookColumns.ContractsApis))),
            Gate: DevelopmentControlCellCodec.GetNullableString(sheet.Cell(row, map.Required(WorkbookColumns.Gate))),
            AcceptanceCriteria: DevelopmentControlCellCodec.GetNullableString(sheet.Cell(row, map.Required(WorkbookColumns.AcceptanceCriteria))),
            Status: status,
            BreakdownComplete: DevelopmentControlCellCodec.GetYesNo(sheet.Cell(row, map.Required(WorkbookColumns.BreakdownComplete))),
            ManualProgress: DevelopmentControlCellCodec.GetInt(sheet.Cell(row, map.Required(WorkbookColumns.ManualProgress))),
            DerivedProgress: DevelopmentControlCellCodec.GetInt(sheet.Cell(row, map.Required(WorkbookColumns.DerivedProgress))),
            ReportedProgress: DevelopmentControlCellCodec.GetInt(sheet.Cell(row, map.Required(WorkbookColumns.ReportedProgress))),
            Owner: DevelopmentControlCellCodec.GetNullableString(sheet.Cell(row, map.Required(WorkbookColumns.Owner))),
            Priority: DevelopmentControlCellCodec.GetNullableString(sheet.Cell(row, map.Required(WorkbookColumns.Priority))),
            Risk: DevelopmentControlCellCodec.GetNullableString(sheet.Cell(row, map.Required(WorkbookColumns.Risk))),
            RowVersion: info.RowVersion,
            // Current-ness is derived from STATUS, not from the Version History "Is Current"
            // marker: the workbook's own update tooling leaves the newest record of live
            // nodes marked "No" (observed live on Layer-08), so the marker cannot distinguish
            // a live node from a retired one. A node is deleted (retired) only when its status
            // says so -- history is never physically deleted (ADR-003).
            IsDeleted: IsTerminalStatus(status),
            Source: DevelopmentControlCellCodec.GetNullableString(sheet.Cell(row, map.Required(WorkbookColumns.Source))),
            Notes: DevelopmentControlCellCodec.GetNullableString(sheet.Cell(row, map.Required(WorkbookColumns.Notes))),
            CreatedAt: info.CreatedAt ?? DateTimeOffset.MinValue,
            UpdatedAt: info.UpdatedAt ?? DateTimeOffset.MinValue);
    }

    private static bool IsTerminalStatus(Status status) =>
        status is Status.Superseded or Status.Cancelled or Status.Obsolete;

    private static List<(int Row, Node Node)> ReadAllNodes(WorkbookSnapshot snap)
    {
        var list = new List<(int, Node)>();
        var map = snap.MasterMap;
        var last = snap.Master.LastRowUsed()?.RowNumber() ?? MasterRoadmapDataStart - 1;
        for (var row = MasterRoadmapDataStart; row <= last; row++)
        {
            var node = ReadNodeAtRow(snap.Master, map, row, snap.Versions);
            if (node is not null) list.Add((row, node));
        }
        return list;
    }

    private static void WriteNodeToMasterRoadmap(IXLWorksheet sheet, WorksheetColumnMap map, int row, Node node)
    {
        // A..AA carry the node's current-state projection (the same header names exist on
        // Master Roadmap and Version History, so this writer serves both append targets).
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.NodeId)), node.NodeId.Value);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.ParentId)), node.ParentId?.Value);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.NodeType)), DevelopmentControlCellCodec.NodeTypeToText(node.NodeType));
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.SortKey)), node.SortKey);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.HierarchyPath)), node.Path);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.Layer)), node.Layer);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.Phase)), node.Phase);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.Name)), node.Name);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.OutcomePurpose)), node.Outcome);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.Dependencies)), DevelopmentControlCellCodec.JoinList(node.Dependencies.Select(d => d.Value)));
        DevelopmentControlCellCodec.WriteYesNo(sheet.Cell(row, map.Required(WorkbookColumns.ParallelSafe)), node.ParallelSafe);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.Projects)), DevelopmentControlCellCodec.JoinList(node.Projects));
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.FilesGlobs)), DevelopmentControlCellCodec.JoinList(node.FilesGlobs));
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.SchemaContexts)), DevelopmentControlCellCodec.JoinList(node.SchemaContexts));
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.ContractsApis)), DevelopmentControlCellCodec.JoinList(node.ContractsApis));
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.Gate)), node.Gate);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.AcceptanceCriteria)), node.AcceptanceCriteria);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.Status)), DevelopmentControlCellCodec.StatusToText(node.Status));
        DevelopmentControlCellCodec.WriteYesNo(sheet.Cell(row, map.Required(WorkbookColumns.BreakdownComplete)), node.BreakdownComplete);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.ManualProgress)), node.ManualProgress);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.DerivedProgress)), node.DerivedProgress);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.ReportedProgress)), node.ReportedProgress);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.Owner)), node.Owner);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.Priority)), node.Priority);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.Risk)), node.Risk);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.Source)), node.Source);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.Notes)), node.Notes);
    }

    // ------------------------------------------------------------------ version history

    private static void AppendVersionHistory(
        IXLWorksheet sheet, WorksheetColumnMap map, Node node,
        string? previousVersionString, int previousHistoryRow, string? baselineVersion,
        MutationEnvelope envelope, string changeType, string changeSummary)
    {
        var row = NextFreeRow(sheet, VersionHistoryDataStart);

        // The workbook's own append convention: the previously-current record is flipped to
        // Is Current = No and the new record is appended with Is Current = Yes (a new Record
        // Version). History is never rewritten (ADR-003 append-only).
        if (previousHistoryRow > 0)
            DevelopmentControlCellCodec.Write(sheet.Cell(previousHistoryRow, map.Required(WorkbookColumns.IsCurrent)), "No");

        WriteNodeToMasterRoadmap(sheet, map, row, node);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.BaselineVersion)), baselineVersion ?? "1.0");
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.RecordVersion)), DevelopmentControlCellCodec.NextRecordVersion(previousVersionString));
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.EffectiveFrom)), DevelopmentControlCellCodec.ToOADate(DateTimeOffset.UtcNow));
        DevelopmentControlCellCodec.WriteYesNo(sheet.Cell(row, map.Required(WorkbookColumns.IsCurrent)), !node.IsDeleted);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.ChangeId)), envelope.ChangeId);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.SupersedesVersion)), previousVersionString ?? "");
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.Source)), envelope.Source);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.Notes)), envelope.Reason);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.ChangeType)), changeType);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.ChangeSummary)), changeSummary);
        DevelopmentControlCellCodec.Write(sheet.Cell(row, map.Required(WorkbookColumns.AdrDecisionLink)), "");
    }

    // ------------------------------------------------------------------ activity log

    private static string NextActivityId(WorkbookSnapshot snap, DateTimeOffset now)
    {
        var colActivity = snap.ActivityLogMap.Required(WorkbookColumns.ActivityId);
        var ids = new List<string>();
        var last = snap.ActivityLog.LastRowUsed()?.RowNumber() ?? ActivityLogDataStart - 1;
        for (var row = ActivityLogDataStart; row <= last; row++)
        {
            var id = DevelopmentControlCellCodec.GetString(snap.ActivityLog.Cell(row, colActivity));
            if (id.Length > 0) ids.Add(id);
        }
        return DevelopmentControlCellCodec.NextActivityId(ids, now);
    }

    private static ActivityLogEntry BuildActivityEntry(
        string activityId, string operation, string entityType, string entityId,
        Node? current, Node? updated, MutationEnvelope envelope, ActorRef? actor = null,
        int? newRowVersion = null, string? result = null, string? evidence = null,
        string? errorCode = null, string? errorMessage = null, TimeSpan? duration = null,
        string? beforeValue = null, string? afterValue = null,
        IReadOnlyList<string>? filesGlobs = null, PreflightVerdict? preflightVerdict = null)
    {
        var performedBy = actor ?? envelope.Actor ?? throw new InvalidOperationException("envelope.Actor is required.");
        var now = DateTimeOffset.UtcNow;
        return new ActivityLogEntry(
            ActivityId: activityId,
            TimestampUtc: now,
            ActorType: performedBy.Type,
            ActorId: performedBy.Id,
            ActorName: performedBy.Name,
            Source: envelope.Source,
            ChatPlatform: envelope.ChatPlatform,
            ChatOrSessionId: envelope.SessionId,
            PromptId: envelope.PromptId,
            ChangeId: envelope.ChangeId,
            CorrelationId: envelope.CorrelationId,
            Operation: operation,
            EntityType: entityType,
            EntityId: entityId,
            ParentId: (current ?? updated)?.ParentId?.Value,
            ExpectedRowVersion: envelope.ExpectedRowVersion,
            PreviousRowVersion: current?.RowVersion,
            NewRowVersion: newRowVersion,
            BeforeValue: beforeValue ?? (current is null ? null : DevelopmentControlCellCodec.SummarizeNode(current)),
            AfterValue: afterValue ?? (updated is null ? null : DevelopmentControlCellCodec.SummarizeNode(updated)),
            Reason: envelope.Reason,
            Repository: null,
            Project: null,
            Branch: null,
            Worktree: null,
            FilesGlobs: filesGlobs ?? updated?.FilesGlobs ?? current?.FilesGlobs ?? Array.Empty<string>(),
            PreflightVerdict: preflightVerdict,
            Result: result,
            Evidence: evidence,
            ErrorCode: errorCode,
            ErrorMessage: errorMessage,
            Duration: duration,
            HumanReviewStatus: "Not Reviewed",
            CreatedAt: now);
    }

    private static string AppendActivityLog(WorkbookSnapshot snap, ActivityLogEntry entry)
    {
        var map = snap.ActivityLogMap;
        var row = NextFreeRow(snap.ActivityLog, ActivityLogDataStart);
        void WriteCell(string column, string? value) => DevelopmentControlCellCodec.Write(snap.ActivityLog.Cell(row, map.Required(column)), value);
        WriteCell(WorkbookColumns.ActivityId, entry.ActivityId);
        WriteCell(WorkbookColumns.TimestampUtc, DevelopmentControlCellCodec.FormatTimestamp(entry.TimestampUtc));
        WriteCell(WorkbookColumns.ActorType, DevelopmentControlCellCodec.ActorTypeToText(entry.ActorType));
        WriteCell(WorkbookColumns.ActorId, entry.ActorId);
        WriteCell(WorkbookColumns.ActorName, entry.ActorName);
        WriteCell(WorkbookColumns.Source, entry.Source);
        WriteCell(WorkbookColumns.ChatPlatform, entry.ChatPlatform);
        WriteCell(WorkbookColumns.ChatSessionId, entry.ChatOrSessionId);
        WriteCell(WorkbookColumns.PromptId, entry.PromptId);
        WriteCell(WorkbookColumns.ChangeId, entry.ChangeId);
        WriteCell(WorkbookColumns.CorrelationId, entry.CorrelationId);
        WriteCell(WorkbookColumns.Operation, entry.Operation);
        WriteCell(WorkbookColumns.EntityType, entry.EntityType);
        WriteCell(WorkbookColumns.EntityId, entry.EntityId);
        WriteCell(WorkbookColumns.ActivityParentId, entry.ParentId);
        WriteCell(WorkbookColumns.ExpectedRowVersion, entry.ExpectedRowVersion?.ToString(CultureInfo.InvariantCulture));
        WriteCell(WorkbookColumns.PreviousRowVersion, entry.PreviousRowVersion?.ToString(CultureInfo.InvariantCulture));
        WriteCell(WorkbookColumns.NewRowVersion, entry.NewRowVersion?.ToString(CultureInfo.InvariantCulture));
        WriteCell(WorkbookColumns.BeforeValue, entry.BeforeValue);
        WriteCell(WorkbookColumns.AfterValue, entry.AfterValue);
        WriteCell(WorkbookColumns.Reason, entry.Reason);
        WriteCell(WorkbookColumns.Repository, entry.Repository);
        WriteCell(WorkbookColumns.Project, entry.Project);
        WriteCell(WorkbookColumns.Branch, entry.Branch);
        WriteCell(WorkbookColumns.Worktree, entry.Worktree);
        WriteCell(WorkbookColumns.FilesGlobs, DevelopmentControlCellCodec.JoinList(entry.FilesGlobs));
        WriteCell(WorkbookColumns.PreflightVerdict, entry.PreflightVerdict is null ? null : DevelopmentControlCellCodec.PreflightVerdictToText(entry.PreflightVerdict.Value));
        WriteCell(WorkbookColumns.Result, entry.Result);
        WriteCell(WorkbookColumns.Evidence, entry.Evidence);
        WriteCell(WorkbookColumns.ErrorCode, entry.ErrorCode);
        WriteCell(WorkbookColumns.ErrorMessage, entry.ErrorMessage);
        WriteCell(WorkbookColumns.Duration, entry.Duration?.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture));
        WriteCell(WorkbookColumns.HumanReviewStatus, entry.HumanReviewStatus);
        WriteCell(WorkbookColumns.CreatedAt, DevelopmentControlCellCodec.FormatTimestamp(entry.CreatedAt));
        return entry.ActivityId;
    }

    private ActivityLogEntry? FindActivityById(WorkbookSnapshot snap, string activityId)
    {
        var colActivity = snap.ActivityLogMap.Required(WorkbookColumns.ActivityId);
        var last = snap.ActivityLog.LastRowUsed()?.RowNumber() ?? ActivityLogDataStart - 1;
        for (var row = ActivityLogDataStart; row <= last; row++)
        {
            if (DevelopmentControlCellCodec.GetString(snap.ActivityLog.Cell(row, colActivity)) == activityId)
                return ReadActivityLogEntryAtRow(snap, row);
        }
        return null;
    }

    // Idempotency helper: a retried mutation is recognized by an existing Activity Log row
    // carrying the same change id + operation + entity id. The IdempotencyKey itself is not
    // persisted (the 34-column log has no dedicated column) -- documented limitation.
    private static ActivityLogEntry? FindAppliedActivity(WorkbookSnapshot snap, string operation, string? changeId, string entityId)
    {
        if (changeId is null) return null;
        var colOperation = snap.ActivityLogMap.Required(WorkbookColumns.Operation);
        var colChangeId = snap.ActivityLogMap.Required(WorkbookColumns.ChangeId);
        var colEntityId = snap.ActivityLogMap.Required(WorkbookColumns.EntityId);
        var last = snap.ActivityLog.LastRowUsed()?.RowNumber() ?? ActivityLogDataStart - 1;
        for (var row = last; row >= ActivityLogDataStart; row--)
        {
            if (DevelopmentControlCellCodec.GetString(snap.ActivityLog.Cell(row, colOperation)) == operation
                && DevelopmentControlCellCodec.GetString(snap.ActivityLog.Cell(row, colChangeId)) == changeId
                && DevelopmentControlCellCodec.GetString(snap.ActivityLog.Cell(row, colEntityId)) == entityId)
                return ReadActivityLogEntryAtRow(snap, row);
        }
        return null;
    }

    private static List<ActivityLogEntry> ReadActivityLog(WorkbookSnapshot snap)
    {
        var list = new List<ActivityLogEntry>();
        var colActivity = snap.ActivityLogMap.Required(WorkbookColumns.ActivityId);
        var last = snap.ActivityLog.LastRowUsed()?.RowNumber() ?? ActivityLogDataStart - 1;
        for (var row = last; row >= ActivityLogDataStart; row--) // newest first
        {
            if (DevelopmentControlCellCodec.GetString(snap.ActivityLog.Cell(row, colActivity)).Length == 0) continue;
            list.Add(ReadActivityLogEntryAtRow(snap, row));
        }
        return list;
    }

    private static ActivityLogEntry ReadActivityLogEntryAtRow(WorkbookSnapshot snap, int row)
    {
        var map = snap.ActivityLogMap;
        IXLCell Cell(string column) => snap.ActivityLog.Cell(row, map.Required(column));
        return new ActivityLogEntry(
            ActivityId: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.ActivityId)),
            TimestampUtc: DevelopmentControlCellCodec.GetDateTimeOffset(Cell(WorkbookColumns.TimestampUtc)) ?? DateTimeOffset.MinValue,
            ActorType: DevelopmentControlCellCodec.ParseActorType(DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.ActorType))),
            ActorId: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.ActorId)),
            ActorName: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.ActorName)),
            Source: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.Source)),
            ChatPlatform: DevelopmentControlCellCodec.GetNullableString(Cell(WorkbookColumns.ChatPlatform)),
            ChatOrSessionId: DevelopmentControlCellCodec.GetNullableString(Cell(WorkbookColumns.ChatSessionId)),
            PromptId: DevelopmentControlCellCodec.GetNullableString(Cell(WorkbookColumns.PromptId)),
            ChangeId: DevelopmentControlCellCodec.GetNullableString(Cell(WorkbookColumns.ChangeId)),
            CorrelationId: DevelopmentControlCellCodec.GetNullableString(Cell(WorkbookColumns.CorrelationId)),
            Operation: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.Operation)),
            EntityType: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.EntityType)),
            EntityId: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.EntityId)),
            ParentId: DevelopmentControlCellCodec.GetNullableString(Cell(WorkbookColumns.ActivityParentId)),
            ExpectedRowVersion: DevelopmentControlCellCodec.GetInt(Cell(WorkbookColumns.ExpectedRowVersion)),
            PreviousRowVersion: DevelopmentControlCellCodec.GetInt(Cell(WorkbookColumns.PreviousRowVersion)),
            NewRowVersion: DevelopmentControlCellCodec.GetInt(Cell(WorkbookColumns.NewRowVersion)),
            BeforeValue: DevelopmentControlCellCodec.GetNullableString(Cell(WorkbookColumns.BeforeValue)),
            AfterValue: DevelopmentControlCellCodec.GetNullableString(Cell(WorkbookColumns.AfterValue)),
            Reason: DevelopmentControlCellCodec.GetNullableString(Cell(WorkbookColumns.Reason)),
            Repository: DevelopmentControlCellCodec.GetNullableString(Cell(WorkbookColumns.Repository)),
            Project: DevelopmentControlCellCodec.GetNullableString(Cell(WorkbookColumns.Project)),
            Branch: DevelopmentControlCellCodec.GetNullableString(Cell(WorkbookColumns.Branch)),
            Worktree: DevelopmentControlCellCodec.GetNullableString(Cell(WorkbookColumns.Worktree)),
            FilesGlobs: DevelopmentControlCellCodec.GetList(Cell(WorkbookColumns.FilesGlobs)),
            PreflightVerdict: DevelopmentControlCellCodec.ParsePreflightVerdict(DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.PreflightVerdict))),
            Result: DevelopmentControlCellCodec.GetNullableString(Cell(WorkbookColumns.Result)),
            Evidence: DevelopmentControlCellCodec.GetNullableString(Cell(WorkbookColumns.Evidence)),
            ErrorCode: DevelopmentControlCellCodec.GetNullableString(Cell(WorkbookColumns.ErrorCode)),
            ErrorMessage: DevelopmentControlCellCodec.GetNullableString(Cell(WorkbookColumns.ErrorMessage)),
            Duration: DevelopmentControlCellCodec.GetDuration(Cell(WorkbookColumns.Duration)),
            HumanReviewStatus: DevelopmentControlCellCodec.GetNullableString(Cell(WorkbookColumns.HumanReviewStatus)),
            CreatedAt: DevelopmentControlCellCodec.GetDateTimeOffset(Cell(WorkbookColumns.CreatedAt)) ?? DateTimeOffset.MinValue);
    }

    // ------------------------------------------------------------------ active changes

    private static List<(int Row, ActiveChange Change)> ReadActiveChanges(WorkbookSnapshot snap)
    {
        var list = new List<(int, ActiveChange)>();
        var map = snap.ActiveChangesMap;
        var colChangeId = map.Required(WorkbookColumns.ChangeId);
        var last = snap.ActiveChanges.LastRowUsed()?.RowNumber() ?? ActiveChangesDataStart - 1;
        for (var row = ActiveChangesDataStart; row <= last; row++)
        {
            if (DevelopmentControlCellCodec.GetString(snap.ActiveChanges.Cell(row, colChangeId)).Length == 0) continue;
            list.Add((row, ReadActiveChangeAtRow(snap, row)));
        }
        return list;
    }

    private static ActiveChange ReadActiveChangeAtRow(WorkbookSnapshot snap, int row)
    {
        var map = snap.ActiveChangesMap;
        IXLCell Cell(string column) => snap.ActiveChanges.Cell(row, map.Required(column));
        return new ActiveChange(
            ChangeId: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.ChangeId)),
            NodeId: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.NodeId)),
            MilestoneOrFeature: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.MilestoneOrFeature)),
            Summary: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.Summary)),
            RequestedBy: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.RequestedBy)),
            Worker: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.Worker)),
            Repositories: DevelopmentControlCellCodec.GetList(Cell(WorkbookColumns.Repositories)),
            Projects: DevelopmentControlCellCodec.GetList(Cell(WorkbookColumns.Projects)),
            FilesGlobs: DevelopmentControlCellCodec.GetList(Cell(WorkbookColumns.FilesGlobs)),
            SchemaContexts: DevelopmentControlCellCodec.GetList(Cell(WorkbookColumns.SchemaContexts)),
            ContractsApis: DevelopmentControlCellCodec.GetList(Cell(WorkbookColumns.ContractsApis)),
            Status: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.Status)),
            PreflightVerdict: DevelopmentControlCellCodec.ParsePreflightVerdict(DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.PreflightVerdict))),
            ConflictsWith: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.ConflictsWith)),
            DependencyOn: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.DependencyOn)),
            Risk: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.Risk)),
            Branch: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.Branch)),
            Worktree: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.Worktree)),
            StartedAt: DevelopmentControlCellCodec.GetDateTimeOffset(Cell(WorkbookColumns.StartedAt)),
            LastHeartbeat: DevelopmentControlCellCodec.GetDateTimeOffset(Cell(WorkbookColumns.LastHeartbeat)),
            CompletedAt: DevelopmentControlCellCodec.GetDateTimeOffset(Cell(WorkbookColumns.CompletedAt)),
            ResultOrEvidence: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.ResultOrEvidence)),
            ChangeVersion: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.ChangeVersion)),
            SessionOrChat: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.SessionOrChat)),
            Notes: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.Notes)),
            VersionHistoryId: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.VersionHistoryId)),
            AdrId: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.AdrId)),
            AffectedNodes: DevelopmentControlCellCodec.GetList(Cell(WorkbookColumns.AffectedNodes)),
            ChangeType: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.ChangeType)),
            ValidationResult: DevelopmentControlCellCodec.GetString(Cell(WorkbookColumns.ValidationResult)));
    }

    private static void WriteActiveChangeRow(WorkbookSnapshot snap, int row, ActiveChange change)
    {
        var map = snap.ActiveChangesMap;
        void WriteCell(string column, string? value) => DevelopmentControlCellCodec.Write(snap.ActiveChanges.Cell(row, map.Required(column)), value);
        WriteCell(WorkbookColumns.ChangeId, change.ChangeId);
        WriteCell(WorkbookColumns.NodeId, change.NodeId);
        WriteCell(WorkbookColumns.MilestoneOrFeature, change.MilestoneOrFeature);
        WriteCell(WorkbookColumns.Summary, change.Summary);
        WriteCell(WorkbookColumns.RequestedBy, change.RequestedBy);
        WriteCell(WorkbookColumns.Worker, change.Worker);
        WriteCell(WorkbookColumns.Repositories, DevelopmentControlCellCodec.JoinList(change.Repositories));
        WriteCell(WorkbookColumns.Projects, DevelopmentControlCellCodec.JoinList(change.Projects));
        WriteCell(WorkbookColumns.FilesGlobs, DevelopmentControlCellCodec.JoinList(change.FilesGlobs));
        WriteCell(WorkbookColumns.SchemaContexts, DevelopmentControlCellCodec.JoinList(change.SchemaContexts));
        WriteCell(WorkbookColumns.ContractsApis, DevelopmentControlCellCodec.JoinList(change.ContractsApis));
        WriteCell(WorkbookColumns.Status, change.Status);
        WriteCell(WorkbookColumns.PreflightVerdict, change.PreflightVerdict is null ? null : DevelopmentControlCellCodec.PreflightVerdictToText(change.PreflightVerdict.Value));
        WriteCell(WorkbookColumns.ConflictsWith, change.ConflictsWith);
        WriteCell(WorkbookColumns.DependencyOn, change.DependencyOn);
        WriteCell(WorkbookColumns.Risk, change.Risk);
        WriteCell(WorkbookColumns.Branch, change.Branch);
        WriteCell(WorkbookColumns.Worktree, change.Worktree);
        WriteCell(WorkbookColumns.StartedAt, change.StartedAt is null ? null : DevelopmentControlCellCodec.FormatTimestamp(change.StartedAt.Value));
        WriteCell(WorkbookColumns.LastHeartbeat, change.LastHeartbeat is null ? null : DevelopmentControlCellCodec.FormatTimestamp(change.LastHeartbeat.Value));
        WriteCell(WorkbookColumns.CompletedAt, change.CompletedAt is null ? null : DevelopmentControlCellCodec.FormatTimestamp(change.CompletedAt.Value));
        WriteCell(WorkbookColumns.ResultOrEvidence, change.ResultOrEvidence);
        WriteCell(WorkbookColumns.ChangeVersion, change.ChangeVersion);
        WriteCell(WorkbookColumns.SessionOrChat, change.SessionOrChat);
        WriteCell(WorkbookColumns.Notes, change.Notes);
        WriteCell(WorkbookColumns.VersionHistoryId, change.VersionHistoryId);
        WriteCell(WorkbookColumns.AdrId, change.AdrId);
        WriteCell(WorkbookColumns.AffectedNodes, DevelopmentControlCellCodec.JoinList(change.AffectedNodes));
        WriteCell(WorkbookColumns.ChangeType, change.ChangeType);
        WriteCell(WorkbookColumns.ValidationResult, change.ValidationResult);
    }

    private static bool IsOpenChange(ActiveChange change)
    {
        var status = change.Status.Trim();
        return status.Length == 0
            || (!status.StartsWith("Completed", StringComparison.OrdinalIgnoreCase)
                && !status.StartsWith("Cancelled", StringComparison.OrdinalIgnoreCase));
    }

    // ------------------------------------------------------------------ shared mutation engine

    private static MutationResult<Node> ApplyNodeMutation(
        WorkbookSnapshot snap, Node? current, Node desired, MutationEnvelope envelope,
        string operation, string changeType, string changeSummary)
    {
        var errors = ValidateEnvelope(envelope);
        if (errors.Count > 0) return new MutationResult<Node>(false, null, false, null, errors, null);

        // Idempotency: a retried mutation under the same change id + operation + entity is a
        // no-op returning the current state (checked before the optimistic token so a retry
        // carrying the ORIGINAL ExpectedRowVersion still dedupes).
        if (current is not null && envelope.IdempotencyKey is not null)
        {
            var alreadyApplied = FindAppliedActivity(snap, operation, envelope.ChangeId, desired.NodeId.Value);
            if (alreadyApplied is not null)
                return new MutationResult<Node>(true, current, false, null, Array.Empty<string>(), alreadyApplied.ActivityId);
        }

        if (current is not null && envelope.ExpectedRowVersion is not null && envelope.ExpectedRowVersion != current.RowVersion)
            return new MutationResult<Node>(false, null, true, current, Array.Empty<string>(), null);

        var (resultNode, activityId) = ApplyNodeWrite(snap, current, desired, envelope, operation, changeType, changeSummary);
        return new MutationResult<Node>(true, resultNode, false, null, Array.Empty<string>(), activityId);
    }

    // The atomic single-node write: current-state row + Version History record + Activity Log
    // entry, all in-memory, saved once by the caller. Throws InvalidOperationException on any
    // anomaly before anything is written, so a failure leaves no partial governed change.
    private static (Node ResultNode, string ActivityLogEntryId) ApplyNodeWrite(
        WorkbookSnapshot snap, Node? current, Node desired, MutationEnvelope envelope,
        string operation, string changeType, string changeSummary)
    {
        var targetRow = current is null
            ? NextFreeRow(snap.Master, MasterRoadmapDataStart)
            : FindMasterRoadmapRow(snap, current.NodeId.Value);
        if (current is not null && targetRow == 0)
            throw new InvalidOperationException($"The current row for node '{current.NodeId.Value}' disappeared between read and write.");

        WriteNodeToMasterRoadmap(snap.Master, snap.MasterMap, targetRow, desired);

        var info = snap.Versions.For(desired.NodeId.Value);
        var previousHistoryRow = current is null ? 0 : info.LatestHistoryRow;
        var previousVersionString = current is null ? null : info.LatestVersionString;
        var baselineVersion = current is null ? null : info.BaselineVersion;
        AppendVersionHistory(snap.VersionHistory, snap.VersionHistoryMap, desired,
            previousVersionString, previousHistoryRow, baselineVersion, envelope, changeType, changeSummary);

        var now = DateTimeOffset.UtcNow;
        var newRowVersion = current is null ? 1 : current.RowVersion + 1;
        var resultNode = desired with { RowVersion = newRowVersion, UpdatedAt = now };
        var activityId = AppendActivityLog(snap, BuildActivityEntry(
            NextActivityId(snap, now), operation, "Node", desired.NodeId.Value,
            current, resultNode, envelope, newRowVersion: newRowVersion, result: changeSummary));
        return (resultNode, activityId);
    }

    // ------------------------------------------------------------------ sort key / path generation

    private static Node EnsureSortKeyAndPath(WorkbookSnapshot snap, Node node)
    {
        var result = node;
        if (string.IsNullOrWhiteSpace(result.SortKey)) result = result with { SortKey = GenerateSortKey(snap, result.ParentId) };
        if (string.IsNullOrWhiteSpace(result.Path)) result = result with { Path = GeneratePath(snap, result.ParentId, result.Name) };
        return result;
    }

    private static string GenerateSortKey(WorkbookSnapshot snap, NodeId? parentId)
    {
        var parentSortKey = parentId is null ? "" : ReadNodeField(snap, parentId.Value.Value, WorkbookColumns.SortKey);
        var maxOrdinal = 0;
        var colParent = snap.MasterMap.Required(WorkbookColumns.ParentId);
        var colSortKey = snap.MasterMap.Required(WorkbookColumns.SortKey);
        var last = snap.Master.LastRowUsed()?.RowNumber() ?? MasterRoadmapDataStart - 1;
        for (var row = MasterRoadmapDataStart; row <= last; row++)
        {
            if (DevelopmentControlCellCodec.GetNullableString(snap.Master.Cell(row, colParent)) != parentId?.Value) continue;
            var ordinal = ParseTrailingOrdinal(DevelopmentControlCellCodec.GetString(snap.Master.Cell(row, colSortKey)));
            if (ordinal > maxOrdinal) maxOrdinal = ordinal;
        }
        var next = maxOrdinal + 1;
        return parentId is null
            ? next.ToString("00", CultureInfo.InvariantCulture)
            : parentSortKey + "." + next.ToString("000", CultureInfo.InvariantCulture);
    }

    private static int ParseTrailingOrdinal(string sortKey)
    {
        var dot = sortKey.LastIndexOf('.');
        var tail = dot >= 0 ? sortKey.Substring(dot + 1) : sortKey;
        return int.TryParse(tail, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;
    }

    private static string GeneratePath(WorkbookSnapshot snap, NodeId? parentId, string name)
    {
        var parentPath = parentId is null ? "" : ReadNodeField(snap, parentId.Value.Value, WorkbookColumns.HierarchyPath);
        return string.IsNullOrEmpty(parentPath) ? name : parentPath + " > " + name;
    }

    private static string ReadNodeField(WorkbookSnapshot snap, string nodeId, string normalizedHeader)
    {
        var row = FindMasterRoadmapRow(snap, nodeId);
        if (row == 0) return "";
        return DevelopmentControlCellCodec.GetString(snap.Master.Cell(row, snap.MasterMap.Required(normalizedHeader)));
    }

    private static string? AppendNote(string? notes, string? append)
    {
        if (string.IsNullOrWhiteSpace(append)) return notes;
        return string.IsNullOrWhiteSpace(notes) ? append : notes + Environment.NewLine + append;
    }

    // ------------------------------------------------------------------ control state

    private ControlState GetControlStateCore(CancellationToken cancellationToken)
        => WithSnapshotRead(cancellationToken, snap => GetControlStateFromSnapshot(snap, snap.Master.Workbook));

    private static ControlState GetControlStateFromSnapshot(WorkbookSnapshot snap, IXLWorkbook workbook)
    {
        var all = ReadAllNodes(snap);
        var live = all.Where(t => !t.Node.IsDeleted).ToList();
        var openChanges = ReadActiveChanges(snap).Select(t => t.Change).Where(IsOpenChange).ToList();
        var roots = live.Where(n => n.Node.ParentId is null).Select(n => n.Node.NodeId).ToList();
        var workbookVersion = ParseWorkbookVersion(workbook);

        // Counts are recomputed from the source sheets over LIVE nodes only; the Control
        // Center's manual summary values (column L) are stale and never trusted (ADR-005).
        return new ControlState(
            WorkbookVersion: workbookVersion,
            ControlBaselineVersion: workbookVersion,
            RoadmapVersion: ParseRoadmapVersion(workbook),
            RootNodeId: roots.Count == 1 ? roots[0] : null,
            CurrentNodeCount: live.Count,
            MilestoneCount: live.Count(t => t.Node.NodeType == NodeType.Milestone),
            WorkItemCount: live.Count(t => t.Node.NodeType == NodeType.WorkItem),
            BlockedNodeCount: live.Count(t => t.Node.Status == Status.Blocked),
            ActiveChangeCount: openChanges.Count,
            OpenAuditFindingCount: CountOpenAuditFindings(snap),
            LastUpdatedAt: LatestActivityTimestamp(snap));
    }

    private static string ParseWorkbookVersion(IXLWorkbook workbook)
    {
        var narrative = DevelopmentControlCellCodec.GetString(Sheet(workbook, SheetControlCenter).Cell(ControlCenterNarrativeRow, 1));
        var match = Regex.Match(narrative, @"\bv\d+(?:\.\d+)+", RegexOptions.CultureInvariant);
        return match.Success ? match.Value.TrimStart('v', 'V') : "";
    }

    private static string ParseRoadmapVersion(IXLWorkbook workbook)
    {
        var narrative = DevelopmentControlCellCodec.GetString(Sheet(workbook, SheetControlCenter).Cell(ControlCenterNarrativeRow, 1));
        var match = Regex.Match(narrative, @"roadmap\s+v?(\d+(?:\.\d+)+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? match.Groups[1].Value : "";
    }

    private static int CountOpenAuditFindings(WorkbookSnapshot snap)
    {
        var map = snap.AuditFindingsMap;
        var colFindingId = map.Required(WorkbookColumns.FindingId);
        var colStatus = map.Required(WorkbookColumns.Status);
        var last = snap.AuditFindings.LastRowUsed()?.RowNumber() ?? AuditFindingsDataStart - 1;
        var count = 0;
        for (var row = AuditFindingsDataStart; row <= last; row++)
        {
            if (DevelopmentControlCellCodec.GetString(snap.AuditFindings.Cell(row, colFindingId)).Length == 0) continue;
            var status = DevelopmentControlCellCodec.GetString(snap.AuditFindings.Cell(row, colStatus));
            if (status.Length == 0
                || (!status.StartsWith("Resolved", StringComparison.OrdinalIgnoreCase)
                    && !status.StartsWith("Closed", StringComparison.OrdinalIgnoreCase)
                    && !status.StartsWith("Cancelled", StringComparison.OrdinalIgnoreCase)))
                count++;
        }
        return count;
    }

    private static DateTimeOffset? LatestActivityTimestamp(WorkbookSnapshot snap)
    {
        var colActivity = snap.ActivityLogMap.Required(WorkbookColumns.ActivityId);
        var colTimestamp = snap.ActivityLogMap.Required(WorkbookColumns.TimestampUtc);
        DateTimeOffset? latest = null;
        var last = snap.ActivityLog.LastRowUsed()?.RowNumber() ?? ActivityLogDataStart - 1;
        for (var row = ActivityLogDataStart; row <= last; row++)
        {
            if (DevelopmentControlCellCodec.GetString(snap.ActivityLog.Cell(row, colActivity)).Length == 0) continue;
            var timestamp = DevelopmentControlCellCodec.GetDateTimeOffset(snap.ActivityLog.Cell(row, colTimestamp));
            if (timestamp.HasValue && (latest is null || timestamp > latest)) latest = timestamp;
        }
        return latest;
    }

    // ------------------------------------------------------------------ subtree / search / next-executable

    private IReadOnlyList<Node> GetSubtreeCore(CancellationToken cancellationToken, NodeId rootNodeId)
        => WithSnapshotRead(cancellationToken, snap => GetSubtreeFromSnapshot(snap, rootNodeId));

    private static IReadOnlyList<Node> GetSubtreeFromSnapshot(WorkbookSnapshot snap, NodeId rootNodeId)
    {
        var all = ReadAllNodes(snap);
        var liveById = all.Where(t => !t.Node.IsDeleted).ToDictionary(t => t.Node.NodeId.Value, t => t.Node, StringComparer.Ordinal);
        if (!liveById.ContainsKey(rootNodeId.Value)) return Array.Empty<Node>();

        var children = new Dictionary<string, List<Node>>(StringComparer.Ordinal);
        foreach (var (_, node) in all.Where(t => !t.Node.IsDeleted))
        {
            if (node.ParentId is null) continue;
            if (!children.TryGetValue(node.ParentId.Value.Value, out var list))
            {
                list = new List<Node>();
                children.Add(node.ParentId.Value.Value, list);
            }
            list.Add(node);
        }
        foreach (var list in children.Values)
            list.Sort((a, b) => DevelopmentControlCellCodec.CompareSortKeys(a.SortKey, b.SortKey));

        var result = new List<Node>();
        var queue = new Queue<Node>();
        queue.Enqueue(liveById[rootNodeId.Value]);
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            result.Add(node);
            if (children.TryGetValue(node.NodeId.Value, out var kids))
                foreach (var kid in kids) queue.Enqueue(kid);
        }
        return result;
    }

    private IReadOnlyList<Node> SearchNodesCore(CancellationToken cancellationToken, NodeSearchCriteria criteria)
        => WithSnapshotRead(cancellationToken, snap => SearchNodesFromSnapshot(snap, criteria));

    private static IReadOnlyList<Node> SearchNodesFromSnapshot(WorkbookSnapshot snap, NodeSearchCriteria criteria)
    {
        var all = ReadAllNodes(snap).Select(t => t.Node).Where(n => !n.IsDeleted);
        if (criteria is null) criteria = new NodeSearchCriteria(null, null, null, null, false);

        IEnumerable<Node> query = all;
        var text = criteria.Text?.Trim();
        if (!string.IsNullOrEmpty(text))
            query = query.Where(n =>
                n.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
                || n.Path.Contains(text, StringComparison.OrdinalIgnoreCase)
                || (n.Notes?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false));
        if (criteria.NodeType is not null) query = query.Where(n => n.NodeType == criteria.NodeType.Value);
        if (criteria.Status is not null) query = query.Where(n => n.Status == criteria.Status.Value);
        if (criteria.ParentId is not null) query = query.Where(n => n.ParentId?.Value == criteria.ParentId.Value.Value);
        if (criteria.RootNodesOnly) query = query.Where(n => n.ParentId is null);

        return query
            .OrderBy(n => n.SortKey, Comparer<string>.Create(DevelopmentControlCellCodec.CompareSortKeys))
            .ToArray();
    }

    private Node? GetNextExecutableWorkItemCore(CancellationToken cancellationToken)
        => WithSnapshotRead(cancellationToken, GetNextExecutableWorkItemFromSnapshot);

    private static Node? GetNextExecutableWorkItemFromSnapshot(WorkbookSnapshot snap)
    {
        var all = ReadAllNodes(snap);
        var byId = new Dictionary<string, Node>(StringComparer.Ordinal);
        foreach (var (_, node) in all)
            if (!byId.ContainsKey(node.NodeId.Value)) byId.Add(node.NodeId.Value, node);

        var reservedNodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var change in ReadActiveChanges(snap).Select(t => t.Change).Where(IsOpenChange))
        {
            if (change.NodeId.Length > 0) reservedNodeIds.Add(change.NodeId);
            foreach (var id in change.AffectedNodes) reservedNodeIds.Add(id);
        }

        return all
            .Select(t => t.Node)
            .Where(n => !n.IsDeleted)
            .Where(n => n.NodeType == NodeType.WorkItem || n.NodeType == NodeType.Task || n.NodeType == NodeType.Subtask)
            .Where(n => n.Status == Status.Ready || n.Status == Status.Planned)
            .Where(n => !reservedNodeIds.Contains(n.NodeId.Value))
            .Where(n => n.Dependencies.All(dep => byId.TryGetValue(dep.Value, out var depNode) && depNode.Status == Status.Completed))
            .Where(n => n.ParentId is not null && byId.TryGetValue(n.ParentId.Value.Value, out var parent)
                && (parent.BreakdownComplete || parent.Status == Status.Completed))
            .OrderBy(n => n.SortKey, Comparer<string>.Create(DevelopmentControlCellCodec.CompareSortKeys))
            .FirstOrDefault();
    }

    private IReadOnlyList<ActiveChange> GetActiveChangesCore(CancellationToken cancellationToken)
        => WithSnapshotRead(cancellationToken, GetActiveChangesFromSnapshot);

    private static IReadOnlyList<ActiveChange> GetActiveChangesFromSnapshot(WorkbookSnapshot snap)
    {
        var rows = ReadActiveChanges(snap); // ascending by row
        var result = new List<ActiveChange>();
        for (var i = rows.Count - 1; i >= 0; i--) // newest first
        {
            var change = rows[i].Change;
            if (IsOpenChange(change)) result.Add(change);
        }
        return result;
    }

    private IReadOnlyList<ActivityLogEntry> GetActivityLogCore(CancellationToken cancellationToken)
        => WithSnapshotRead(cancellationToken, ReadActivityLog); // newest first

    // ------------------------------------------------------------------ preflight

    private PreflightResult RunPreflightCore(CancellationToken cancellationToken, PreflightDeclaration declaration)
        => WithSnapshotRead(cancellationToken, snap => RunPreflightFromSnapshot(snap, declaration));

    private static PreflightResult RunPreflightFromSnapshot(WorkbookSnapshot snap, PreflightDeclaration declaration)
    {
        var openChanges = ReadActiveChanges(snap).Select(t => t.Change).Where(IsOpenChange).ToList();

        var declaredNodeId = declaration.RoadmapNodeId.Value;
        var declaredNode = GetNodeCore(snap, declaration.RoadmapNodeId);
        var declaredDependencies = new HashSet<string>(declaration.Dependencies, StringComparer.OrdinalIgnoreCase);
        if (declaredNode is not null)
            foreach (var dep in declaredNode.Dependencies) declaredDependencies.Add(dep.Value);

        var findings = new List<string>();
        var verdict = PreflightVerdict.Clear;
        foreach (var change in openChanges)
        {
            var (candidate, finding) = CompareAgainst(declaration, declaredDependencies, declaredNodeId, change);
            if ((int)candidate > (int)verdict)
            {
                verdict = candidate;
                findings.Add(finding);
            }
            else if ((int)candidate == (int)verdict && candidate != PreflightVerdict.Clear)
            {
                findings.Add(finding);
            }
        }
        return new PreflightResult(verdict, findings.Count == 0 ? null : findings[0], findings);
    }

    // Conservative, documented preflight heuristics (a first pass over the workbook's
    // Session-Protocol rules). Severity ranks by the enum's own order: Clear < DependencyFound
    // < OverlapFound < ConflictFound < ArchitectureConflict.
    private static (PreflightVerdict Verdict, string Finding) CompareAgainst(
        PreflightDeclaration declaration, HashSet<string> declaredDependencies, string declaredNodeId, ActiveChange change)
    {
        var changeNodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (change.NodeId.Length > 0) changeNodeIds.Add(change.NodeId);
        foreach (var id in change.AffectedNodes) changeNodeIds.Add(id);

        if (changeNodeIds.Contains(declaredNodeId))
            return (PreflightVerdict.ConflictFound, $"Declared node {declaredNodeId} is already covered by open change {change.ChangeId}.");
        if (GlobsOverlap(declaration.FilesGlobs, change.FilesGlobs))
            return (PreflightVerdict.ConflictFound, $"Files/globs for open change {change.ChangeId} overlap the declared scope.");
        if (declaration.SchemaOrDbContextMutation && change.SchemaContexts.Count > 0)
            return (PreflightVerdict.ArchitectureConflict, $"Shared schema/DbContext mutation with open change {change.ChangeId}.");
        if (Overlaps(declaration.ContractsApis, change.ContractsApis))
            return (PreflightVerdict.ArchitectureConflict, $"Shared contract/API surface with open change {change.ChangeId}.");
        if (declaredDependencies.Overlaps(changeNodeIds))
            return (PreflightVerdict.DependencyFound, $"Declared work depends on (or is depended on by) open change {change.ChangeId}.");
        if (Overlaps(declaration.Repositories, change.Repositories))
            return (PreflightVerdict.OverlapFound, $"Repository-level overlap with open change {change.ChangeId}; no file-level conflict detected.");
        return (PreflightVerdict.Clear, "");
    }

    // Naive path-prefix overlap (documented first pass): identical paths, or one path is a
    // directory-prefix of the other. No glob-wildcard expansion in this pass.
    private static bool GlobsOverlap(IReadOnlyList<string> a, IReadOnlyList<string> b)
    {
        if (a.Count == 0 || b.Count == 0) return false;
        foreach (var x in a)
            foreach (var y in b)
                if (GlobOverlaps(x, y)) return true;
        return false;
    }

    private static bool GlobOverlaps(string x, string y)
    {
        var nx = x.Trim().TrimEnd('/', '\\');
        var ny = y.Trim().TrimEnd('/', '\\');
        if (nx.Length == 0 || ny.Length == 0) return false;
        if (string.Equals(nx, ny, StringComparison.OrdinalIgnoreCase)) return true;
        if (nx.StartsWith(ny + "/", StringComparison.OrdinalIgnoreCase) || nx.StartsWith(ny + "\\", StringComparison.OrdinalIgnoreCase)) return true;
        if (ny.StartsWith(nx + "/", StringComparison.OrdinalIgnoreCase) || ny.StartsWith(nx + "\\", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static bool Overlaps(IReadOnlyList<string> a, IReadOnlyList<string> b)
    {
        if (a.Count == 0 || b.Count == 0) return false;
        var set = new HashSet<string>(a, StringComparer.OrdinalIgnoreCase);
        return b.Any(set.Contains);
    }

    // ------------------------------------------------------------------ validation

    private ValidationResult ValidateControlStoreCore(CancellationToken cancellationToken)
        => WithSnapshotRead(cancellationToken, ValidateControlStoreFromSnapshot);

    private static ValidationResult ValidateControlStoreFromSnapshot(WorkbookSnapshot snap)
    {
        var errors = new List<string>();
        var all = ReadAllNodes(snap);
        var byId = new Dictionary<string, Node>(StringComparer.Ordinal);
        foreach (var (_, node) in all)
        {
            if (!byId.TryAdd(node.NodeId.Value, node))
                errors.Add($"Duplicate node id '{node.NodeId.Value}' in Master Roadmap.");
        }

        foreach (var node in byId.Values)
        {
            if (node.ParentId is not null && !byId.ContainsKey(node.ParentId.Value.Value))
                errors.Add($"Node '{node.NodeId.Value}' has ParentId '{node.ParentId.Value.Value}' that does not resolve to a roadmap node.");
            // Dependencies are deliberately NOT required to resolve to roadmap nodes: the live
            // workbook references non-roadmap decision entities (e.g. DEC-002, DEC-003) as
            // dependencies -- legitimate data, not corruption (AF-010).
        }

        // No Version History integrity rule is enforced: the workbook does not maintain
        // "Is Current=Yes" consistently (live Layer-08 nodes carry newest records marked No;
        // planned nodes like M-07-0.2/WI-07-0.2.x have no records at all). Both are legitimate
        // data per the workbook's own conventions -- AF-010 (report the workbook as it is,
        // don't impose an idealized model). Validation is limited to structural integrity.

        var siblingSortKeys = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (var node in byId.Values)
        {
            if (node.SortKey.Length == 0) continue;
            var parentKey = node.ParentId?.Value ?? "(root)";
            if (!siblingSortKeys.TryGetValue(parentKey, out var bySortKey))
            {
                bySortKey = new Dictionary<string, string>(StringComparer.Ordinal);
                siblingSortKeys.Add(parentKey, bySortKey);
            }
            if (bySortKey.TryGetValue(node.SortKey, out var existing))
                errors.Add($"Nodes '{existing}' and '{node.NodeId.Value}' share SortKey '{node.SortKey}' under parent '{parentKey}'.");
            else
                bySortKey.Add(node.SortKey, node.NodeId.Value);
        }

        return new ValidationResult(errors.Count == 0, errors);
    }
}
