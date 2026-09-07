using System.Linq;
using ClosedXML.Excel;
using Nexus.Developer.Core.DevelopmentControl;
using Nexus.Developer.Infrastructure.DevelopmentControl;
using Xunit;

namespace Nexus.Developer.Core.Tests;

// WI-07-0.2.3 / WI-07-0.2.4 stabilization (SP1-M00): integration tests for the Excel
// persistence adapter against DISPOSABLE constructed workbooks -- never the live
// NEXUS_DEVELOPMENT_CONTROL.xlsx. The fixture reproduces the governed sheets' live header
// layouts (Master Roadmap / Active Changes / Audit Findings copied verbatim from the schema
// validator tests; Version History and the 34-column Activity Log shaped to the store's
// normalized column names), so mutations exercise the real temp-write -> validate ->
// atomic-replace save path. Atomicity is asserted by byte-comparing the workbook file before
// and after a governed write: a failed mutation or an aborted atomic work unit must leave the
// canonical file byte-for-byte untouched.
public class ExcelDevelopmentControlStoreTests
{
    private static readonly string[] MasterRoadmapHeaders =
    {
        "Node ID", "Parent ID", "Node Type", "Sort Key", "Hierarchy Path", "Layer", "Phase",
        "Name", "Outcome / Purpose", "Dependencies", "Parallel Safe", "Projects",
        "Files / Globs", "Schema Contexts", "Contracts / APIs", "Gate",
        "Acceptance Criteria", "Status", "Breakdown Complete", "Manual Progress",
        "Derived Progress", "Reported Progress", "Owner", "Priority", "Risk", "Source",
        "Notes", "Simple Goal", "Current Evidence", "Next Action", "Column1", "Column2",
        "Column3"
    };

    // The store appends Version History records by writing the same 27 node columns the
    // Master Roadmap carries, then the record-specific columns.
    private static readonly string[] VersionHistoryHeaders =
    {
        "Node ID", "Parent ID", "Node Type", "Sort Key", "Hierarchy Path", "Layer", "Phase",
        "Name", "Outcome / Purpose", "Dependencies", "Parallel Safe", "Projects",
        "Files / Globs", "Schema Contexts", "Contracts / APIs", "Gate",
        "Acceptance Criteria", "Status", "Breakdown Complete", "Manual Progress",
        "Derived Progress", "Reported Progress", "Owner", "Priority", "Risk", "Source",
        "Notes", "Baseline Version", "Record Version", "Effective From", "Is Current",
        "Change ID", "Supersedes Version", "Change Type", "Change Summary",
        "ADR / Decision Link"
    };

    private static readonly string[] ActiveChangesHeaders =
    {
        "Change ID", "Node ID", "Milestone / Feature", "Summary", "Requested By", "Worker",
        "Repositories", "Projects", "Files / Globs", "Schema Contexts", "Contracts / APIs",
        "Status", "Preflight Verdict", "Conflicts With", "Dependency On", "Risk", "Branch",
        "Worktree", "Started At", "Last Heartbeat", "Completed At", "Result / Evidence",
        "Change Version", "Session / Chat", "Notes", "Version History ID", "ADR ID",
        "Affected Nodes", "Change Type", "Validation Result"
    };

    private static readonly string[] AuditFindingsHeaders =
    {
        "Finding ID", "Severity", "Area", "Repository", "Evidence", "Impact",
        "Required Action", "Roadmap Link", "Status", "Owner", "Due Gate", "Verification",
        "Notes"
    };

    // The 34-column Activity Log contract (header row 4), named to the store's normalized map.
    private static readonly string[] ActivityLogHeaders =
    {
        "Activity ID", "Timestamp UTC", "Actor Type", "Actor ID", "Actor Name", "Source",
        "Chat Platform", "Chat / Session ID", "Prompt ID", "Change ID", "Correlation ID",
        "Operation", "Entity Type", "Entity ID", "Parent ID", "Expected Row Version",
        "Previous Row Version", "New Row Version", "Before Value", "After Value", "Reason",
        "Repository", "Project", "Branch", "Worktree", "Files / Globs",
        "Preflight Verdict", "Result", "Evidence", "Error Code", "Error Message",
        "Duration", "Human Review Status", "Created At"
    };

    private static Node NewNode(string id, NodeId? parent, NodeType type, Status status,
        bool breakdownComplete = false, IReadOnlyList<string>? dependencies = null, string? phase = null) => new(
        new NodeId(id), parent, type, "", "", "03", phase, "Node " + id, null,
        (dependencies ?? Array.Empty<string>()).Select(d => new NodeId(d)).ToArray(), false, Array.Empty<string>(),
        Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), null, null,
        status, breakdownComplete, null, null, null, "Durai", null, null, 0, false, "test",
        null, DateTimeOffset.MinValue, DateTimeOffset.MinValue);

    private static MutationEnvelope Envelope(string changeId, int? expectedRowVersion = null) => new(
        expectedRowVersion,
        new ActorRef(ActorType.Agent, "test-agent", "Test Agent"),
        "Nexus.Developer.Core.Tests", null, "session-1", null, changeId, null, null,
        "test reason");

    private static string NextChangeId() => "CHG-20260907-" + Guid.NewGuid().ToString("N").Substring(0, 6);

    [Fact]
    public async Task EmptyWorkbook_ControlStateIsZeroed()
    {
        using var wb = new ExcelTestWorkbook();
        var store = new ExcelDevelopmentControlStore(wb.FilePath);

        var state = await store.GetControlStateAsync();

        Assert.NotNull(state);
        Assert.Equal(0, state!.CurrentNodeCount);
        Assert.Equal(0, state.MilestoneCount);
        Assert.Equal(0, state.WorkItemCount);
        Assert.Equal(0, state.BlockedNodeCount);
        Assert.Equal(0, state.ActiveChangeCount);
        Assert.Equal(0, state.OpenAuditFindingCount);
        Assert.Null(state.RootNodeId);
        Assert.Equal("1.0", state.WorkbookVersion);
    }

    [Fact]
    public async Task Create_ThenRead_RoundTripsTheNodeAtRowVersion1()
    {
        using var wb = new ExcelTestWorkbook();
        var store = new ExcelDevelopmentControlStore(wb.FilePath);
        var change = NextChangeId();

        var root = await store.CreateNodeAsync(NewNode("M-07-9", null, NodeType.Milestone, Status.Planned), Envelope(change));
        Assert.True(root.Success);
        Assert.Equal(1, root.Value!.RowVersion);
        Assert.NotNull(root.ActivityLogEntryId);

        var child = await store.CreateNodeAsync(
            NewNode("WI-07-9-1", new NodeId("M-07-9"), NodeType.WorkItem, Status.Ready), Envelope(NextChangeId()));
        Assert.True(child.Success);

        var read = await store.GetNodeAsync(new NodeId("WI-07-9-1"));
        Assert.NotNull(read);
        Assert.Equal(1, read!.RowVersion);
        Assert.Equal("M-07-9", read.ParentId!.Value.Value);
        Assert.Equal(Status.Ready, read.Status);
        Assert.Equal("Node M-07-9 > Node WI-07-9-1", read.Path); // hierarchy path is built from names

        // One Activity Log entry per create, in addition to Version History records.
        var log = await store.GetActivityLogAsync();
        Assert.Equal(2, log.Count);

        var validation = await store.ValidateControlStoreAsync();
        Assert.True(validation.IsValid);
    }

    [Fact]
    public async Task Update_WithAMatchingExpectedRowVersion_AdvancesTheRowVersion()
    {
        using var wb = new ExcelTestWorkbook();
        var store = new ExcelDevelopmentControlStore(wb.FilePath);
        var change = NextChangeId();

        await store.CreateNodeAsync(NewNode("M-07-9", null, NodeType.Milestone, Status.Planned), Envelope(change));

        var updated = await store.UpdateNodeAsync(
            NewNode("M-07-9", null, NodeType.Milestone, Status.InProgress), Envelope(NextChangeId(), expectedRowVersion: 1));

        Assert.True(updated.Success);
        Assert.Equal(2, updated.Value!.RowVersion);
        Assert.Equal(Status.InProgress, updated.Value.Status);
    }

    [Fact]
    public async Task Update_WithAStaleExpectedRowVersion_ConflictsAndLeavesTheFileUntouched()
    {
        using var wb = new ExcelTestWorkbook();
        var store = new ExcelDevelopmentControlStore(wb.FilePath);
        var change = NextChangeId();
        await store.CreateNodeAsync(NewNode("M-07-9", null, NodeType.Milestone, Status.Planned), Envelope(change));
        var afterCreate = File.ReadAllBytes(wb.FilePath);

        var stale = await store.UpdateNodeAsync(
            NewNode("M-07-9", null, NodeType.Milestone, Status.InProgress), Envelope(NextChangeId(), expectedRowVersion: 99));

        Assert.False(stale.Success);
        Assert.True(stale.Conflict);
        Assert.Equal(1, ((Node)stale.ConflictDetails!).RowVersion);
        Assert.Equal(afterCreate, File.ReadAllBytes(wb.FilePath)); // no partial write: file byte-identical
    }

    [Fact]
    public async Task Create_OfAnExistingNode_ReportsADuplicateValidationError()
    {
        using var wb = new ExcelTestWorkbook();
        var store = new ExcelDevelopmentControlStore(wb.FilePath);
        var change = NextChangeId();
        await store.CreateNodeAsync(NewNode("M-07-9", null, NodeType.Milestone, Status.Planned), Envelope(change));

        var again = await store.CreateNodeAsync(NewNode("M-07-9", null, NodeType.Milestone, Status.Planned), Envelope(change));

        Assert.False(again.Success);
        Assert.Contains("already exists", Assert.Single(again.ValidationErrors));
    }

    [Fact]
    public async Task RetireNode_HidesTheNodeFromReadsAndSearch()
    {
        using var wb = new ExcelTestWorkbook();
        var store = new ExcelDevelopmentControlStore(wb.FilePath);
        var change = NextChangeId();
        await store.CreateNodeAsync(NewNode("M-07-9", null, NodeType.Milestone, Status.Planned), Envelope(change));

        var retired = await store.RetireNodeAsync(new NodeId("M-07-9"), Envelope(NextChangeId()));
        Assert.True(retired.Success);

        Assert.Null(await store.GetNodeAsync(new NodeId("M-07-9")));
        Assert.DoesNotContain(
            await store.SearchNodesAsync(new NodeSearchCriteria("M-07-9", null, null, null, false)),
            n => n.NodeId.Value == "M-07-9");

        var state = await store.GetControlStateAsync();
        Assert.Equal(0, state!.CurrentNodeCount);
    }

    [Fact]
    public async Task ReserveWorkItem_ConflictsWhenTheNodeIsAlreadyReserved()
    {
        using var wb = new ExcelTestWorkbook();
        var store = new ExcelDevelopmentControlStore(wb.FilePath);
        var change = NextChangeId();
        await store.CreateNodeAsync(NewNode("M-07-9", null, NodeType.Milestone, Status.Planned), Envelope(change));
        await store.CreateNodeAsync(
            NewNode("WI-07-9-1", new NodeId("M-07-9"), NodeType.WorkItem, Status.Ready), Envelope(NextChangeId()));
        var worker = new ActorRef(ActorType.Agent, "worker-1", "Worker One");

        var first = await store.ReserveWorkItemAsync(
            new NodeId("WI-07-9-1"), worker, "feature/branch", null, Envelope(NextChangeId()));
        Assert.True(first.Success);

        var afterFirstReserve = File.ReadAllBytes(wb.FilePath);
        var node = await store.GetNodeAsync(new NodeId("WI-07-9-1"));
        Assert.Equal(Status.InProgress, node!.Status);

        var second = await store.ReserveWorkItemAsync(
            new NodeId("WI-07-9-1"), worker, "feature/branch", null, Envelope(NextChangeId()));
        Assert.False(second.Success);
        Assert.True(second.Conflict);
        Assert.Equal(afterFirstReserve, File.ReadAllBytes(wb.FilePath)); // the conflicting reserve wrote nothing
    }

    [Fact]
    public async Task GetNextExecutableWorkItem_RespectsAnOpenReservation()
    {
        using var wb = new ExcelTestWorkbook();
        var store = new ExcelDevelopmentControlStore(wb.FilePath);
        var change = NextChangeId();
        await store.CreateNodeAsync(
            NewNode("M-07-9", null, NodeType.Milestone, Status.Planned, breakdownComplete: true), Envelope(change));
        await store.CreateNodeAsync(
            NewNode("WI-07-9-1", new NodeId("M-07-9"), NodeType.WorkItem, Status.Ready), Envelope(NextChangeId()));
        var worker = new ActorRef(ActorType.Agent, "worker-1", "Worker One");

        var next = await store.GetNextExecutableWorkItemAsync();
        Assert.NotNull(next);
        Assert.Equal("WI-07-9-1", next!.NodeId.Value);

        await store.ReserveWorkItemAsync(new NodeId("WI-07-9-1"), worker, "feature/branch", null, Envelope(NextChangeId()));
        Assert.Null(await store.GetNextExecutableWorkItemAsync()); // the reserved item is no longer executable
    }

    [Fact]
    public async Task RunPreflight_IsClearWithNoOpenChanges_AndConflictsOnceTheNodeIsReserved()
    {
        using var wb = new ExcelTestWorkbook();
        var store = new ExcelDevelopmentControlStore(wb.FilePath);
        await store.CreateNodeAsync(
            NewNode("M-07-9", null, NodeType.Milestone, Status.Planned), Envelope(NextChangeId()));
        var worker = new ActorRef(ActorType.Agent, "worker-1", "Worker One");

        var declaration = new PreflightDeclaration(
            ChangeId: NextChangeId(),
            RoadmapNodeId: new NodeId("M-07-9"),
            Repositories: Array.Empty<string>(),
            Projects: Array.Empty<string>(),
            FilesGlobs: new[] { "src/Nexus.Developer.Core/" },
            SchemaOrDbContextMutation: false,
            ContractsApis: Array.Empty<string>(),
            Dependencies: Array.Empty<string>(),
            Risk: "Low", Worker: worker.Name, Branch: null, SiblingWorktree: null);

        var clear = await store.RunPreflightAsync(declaration);
        Assert.Equal(PreflightVerdict.Clear, clear.Verdict);

        await store.ReserveWorkItemAsync(new NodeId("M-07-9"), worker, "feature/branch", null, Envelope(NextChangeId()));
        var conflict = await store.RunPreflightAsync(declaration);
        Assert.Equal(PreflightVerdict.ConflictFound, conflict.Verdict);
    }

    [Fact]
    public async Task AtomicWorkUnit_ExecutesMultipleCreatesAsOneSave()
    {
        using var wb = new ExcelTestWorkbook();
        var store = new ExcelDevelopmentControlStore(wb.FilePath);
        var change = NextChangeId();

        var result = await store.ExecuteAtomicWorkUnitAsync<Node>(
            async s =>
            {
                var first = await s.CreateNodeAsync(NewNode("M-07-9", null, NodeType.Milestone, Status.Planned), Envelope(change));
                if (!first.Success) return first;
                return await s.CreateNodeAsync(NewNode("WI-07-9-1", new NodeId("M-07-9"), NodeType.WorkItem, Status.Ready), Envelope(change));
            },
            envelope: Envelope(change),
            verifyEntityNodeId: null);

        Assert.Equal(DevelopmentControlConcurrencyOutcome.Success, result.Outcome);
        Assert.NotNull(await store.GetNodeAsync(new NodeId("M-07-9")));
        Assert.NotNull(await store.GetNodeAsync(new NodeId("WI-07-9-1")));
        Assert.Equal(2, (await store.GetActivityLogAsync()).Count);
    }

    [Fact]
    public async Task AtomicWorkUnit_WithAFailingSecondOperation_RollsBackTheWholeUnit_LeavingTheFileUntouched()
    {
        using var wb = new ExcelTestWorkbook();
        var store = new ExcelDevelopmentControlStore(wb.FilePath);
        var change = NextChangeId();
        var before = File.ReadAllBytes(wb.FilePath);

        // Operation 1 creates a node (succeeds in-memory); operation 2 tries to create the SAME
        // node again, which the in-memory snapshot now sees as a duplicate and fails. Because the
        // unit aborts, operation 1 must NOT be persisted.
        var result = await store.ExecuteAtomicWorkUnitAsync<Node>(
            async s =>
            {
                var first = await s.CreateNodeAsync(NewNode("M-07-9", null, NodeType.Milestone, Status.Planned), Envelope(change));
                if (!first.Success) return first;
                return await s.CreateNodeAsync(NewNode("M-07-9", null, NodeType.Milestone, Status.Planned), Envelope(change));
            },
            envelope: Envelope(change),
            verifyEntityNodeId: null);

        Assert.Equal(DevelopmentControlConcurrencyOutcome.ValidationFailure, result.Outcome);
        Assert.Contains("already exists", Assert.Single(result.ValidationErrors));
        Assert.Equal(before, File.ReadAllBytes(wb.FilePath)); // byte-for-byte unchanged: no partial Operation 1
        Assert.Null(await store.GetNodeAsync(new NodeId("M-07-9")));
    }

    [Fact]
    public async Task AtomicWorkUnit_Precondition_StaleExpectedRowVersion_AbortsWithoutSaving()
    {
        using var wb = new ExcelTestWorkbook();
        var store = new ExcelDevelopmentControlStore(wb.FilePath);
        await store.CreateNodeAsync(NewNode("M-07-9", null, NodeType.Milestone, Status.Planned), Envelope(NextChangeId()));
        var before = File.ReadAllBytes(wb.FilePath);

        var result = await store.ExecuteAtomicWorkUnitAsync<Node>(
            s => Task.FromResult(new MutationResult<Node>(true, NewNode("M-07-9", null, NodeType.Milestone, Status.InProgress), false, null, Array.Empty<string>(), null)),
            envelope: Envelope(NextChangeId(), expectedRowVersion: 99),
            verifyEntityNodeId: "M-07-9");

        Assert.Equal(DevelopmentControlConcurrencyOutcome.ConcurrencyConflict, result.Outcome);
        Assert.Equal(1, ((Node)result.ConflictDetails!).RowVersion);
        Assert.Equal(before, File.ReadAllBytes(wb.FilePath));
        Assert.Equal(1, (await store.GetNodeAsync(new NodeId("M-07-9")))!.RowVersion);
    }

    [Fact]
    public async Task AtomicWorkUnit_Precondition_UnknownNode_ReportsNotFoundWithoutSaving()
    {
        using var wb = new ExcelTestWorkbook();
        var store = new ExcelDevelopmentControlStore(wb.FilePath);
        var before = File.ReadAllBytes(wb.FilePath);

        var result = await store.ExecuteAtomicWorkUnitAsync<Node>(
            s => Task.FromResult(new MutationResult<Node>(true, NewNode("M-99", null, NodeType.Milestone, Status.Planned), false, null, Array.Empty<string>(), null)),
            envelope: Envelope(NextChangeId(), expectedRowVersion: 1),
            verifyEntityNodeId: "M-99");

        Assert.Equal(DevelopmentControlConcurrencyOutcome.NotFound, result.Outcome);
        Assert.Equal(before, File.ReadAllBytes(wb.FilePath));
    }

    [Fact]
    public void Constructor_ThrowsWhenTheWorkbookDoesNotExist()
    {
        var missing = Path.Combine(Path.GetTempPath(), "no-such-" + Guid.NewGuid().ToString("N") + ".xlsx");
        Assert.Throws<FileNotFoundException>(() => new ExcelDevelopmentControlStore(missing));
    }

    // ------------------------------------------------------- SP1-M04 schema version

    [Fact]
    public async Task GovernedWrite_OnALegacyWorkbook_SucceedsAndStampsTheCurrentSchemaVersion()
    {
        // A workbook with NO marker is the documented legacy baseline: it reads exactly as it
        // always has (no required-column throw), a governed write succeeds, and that write
        // stamps the current schema-version marker so the file becomes self-declaring.
        using var wb = new ExcelTestWorkbook();
        using (var fresh = new XLWorkbook(wb.FilePath))
            Assert.Equal(DevelopmentControlSchemaCategory.Legacy, DevelopmentControlWorkbookSchema.Read(fresh).Category);

        var store = new ExcelDevelopmentControlStore(wb.FilePath);
        var created = await store.CreateNodeAsync(
            NewNode("M-07-9", null, NodeType.Milestone, Status.Planned), Envelope(NextChangeId()));
        Assert.True(created.Success);

        using var reopened = new XLWorkbook(wb.FilePath);
        var schema = DevelopmentControlWorkbookSchema.Read(reopened);
        Assert.Equal(DevelopmentControlSchemaCategory.Current, schema.Category);
        Assert.Equal(DevelopmentControlWorkbookSchema.CurrentVersion, schema.DeclaredVersion);
    }

    [Fact]
    public async Task GovernedWrite_OnAFutureVersionWorkbook_IsAControlledRefusal_LeavingTheFileUntouched()
    {
        using var wb = new ExcelTestWorkbook(declaredSchemaVersion: 999);
        using (var fresh = new XLWorkbook(wb.FilePath))
            Assert.Equal(DevelopmentControlSchemaCategory.Future, DevelopmentControlWorkbookSchema.Read(fresh).Category);
        var before = File.ReadAllBytes(wb.FilePath);

        var store = new ExcelDevelopmentControlStore(wb.FilePath);
        var created = await store.CreateNodeAsync(
            NewNode("M-07-9", null, NodeType.Milestone, Status.Planned), Envelope(NextChangeId()));

        // The schema anomaly is surfaced as a failed result carrying the documented message --
        // never an uncaught exception thrown mid-write.
        Assert.False(created.Success);
        Assert.Contains("newer than this build supports", Assert.Single(created.ValidationErrors));
        Assert.Equal(before, File.ReadAllBytes(wb.FilePath)); // controlled refusal: canonical file byte-identical
    }

    [Fact]
    public async Task AtomicWorkUnit_OnAFutureVersionWorkbook_AbortsAsAControlledFailure_LeavingTheFileUntouched()
    {
        using var wb = new ExcelTestWorkbook(declaredSchemaVersion: 999);
        var before = File.ReadAllBytes(wb.FilePath);
        var store = new ExcelDevelopmentControlStore(wb.FilePath);

        var result = await store.ExecuteAtomicWorkUnitAsync<Node>(
            async s => await s.CreateNodeAsync(
                NewNode("M-07-9", null, NodeType.Milestone, Status.Planned), Envelope(NextChangeId())),
            envelope: Envelope(NextChangeId()),
            verifyEntityNodeId: null);

        Assert.Equal(DevelopmentControlConcurrencyOutcome.ValidationFailure, result.Outcome);
        Assert.Contains("newer than this build supports", Assert.Single(result.ValidationErrors));
        Assert.Equal(before, File.ReadAllBytes(wb.FilePath)); // the unit saved nothing
        Assert.Null(await store.GetNodeAsync(new NodeId("M-07-9")));
    }

    // ------------------------------------------- SP1-M04 node strategic-phase reading

    [Fact]
    public async Task Read_StrategicWavePhaseOnAFeature_AndOnADescendant_IsNonBlockingAndPreservesTheRawToken()
    {
        // SP1/SP2/SP3 are feature-level tokens; a descendant carrying one is an informational,
        // NON-blocking observation. Reading a node with a strategic-wave phase must never throw
        // and must preserve the raw free-text token verbatim on the Node (no column added).
        using var wb = new ExcelTestWorkbook();
        var store = new ExcelDevelopmentControlStore(wb.FilePath);
        var change = NextChangeId();
        await store.CreateNodeAsync(
            NewNode("F-07-SP1", null, NodeType.Feature, Status.Planned, phase: "SP1"), Envelope(change));
        await store.CreateNodeAsync(
            NewNode("WI-07-SP1-1", new NodeId("F-07-SP1"), NodeType.WorkItem, Status.Ready, phase: "SP1"),
            Envelope(NextChangeId()));

        var feature = await store.GetNodeAsync(new NodeId("F-07-SP1"));
        var workItem = await store.GetNodeAsync(new NodeId("WI-07-SP1-1"));

        Assert.NotNull(feature);
        Assert.Equal("SP1", feature!.Phase);
        Assert.NotNull(workItem);
        Assert.Equal("SP1", workItem!.Phase);
    }

    // ------------------------------------------------------------------ fixture

    // Builds a six-sheet development-control workbook (live header layouts) at a unique temp
    // path and deletes it on Dispose. Tests never touch the real NEXUS_DEVELOPMENT_CONTROL.xlsx.
    private sealed class ExcelTestWorkbook : IDisposable
    {
        // SP1-M04: the fixture can OPTIONALLY stamp the schema-version marker pair onto the
        // Control Center. Null (the default) builds a LEGACY workbook with no marker -- the
        // documented pre-versioning baseline every governed write must keep reading unchanged.
        // A non-null value declares that schema version, letting tests exercise the current-
        // and future-version read/write paths against disposable copies only.
        public ExcelTestWorkbook(int? declaredSchemaVersion = null)
        {
            var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"nexus-dev-store-{Guid.NewGuid():N}"));
            FilePath = System.IO.Path.Combine(dir.FullName, "NEXUS_DEVELOPMENT_CONTROL.xlsx");
            using var workbook = new XLWorkbook();

            WriteHeaders(workbook.AddWorksheet("Master Roadmap"), 5, MasterRoadmapHeaders);
            WriteHeaders(workbook.AddWorksheet("Version History"), 5, VersionHistoryHeaders);
            WriteHeaders(workbook.AddWorksheet("Active Changes"), 5, ActiveChangesHeaders);
            WriteHeaders(workbook.AddWorksheet("Audit Findings"), 5, AuditFindingsHeaders);
            WriteHeaders(workbook.AddWorksheet("Activity Log"), 4, ActivityLogHeaders);

            var controlCenter = workbook.AddWorksheet("Control Center");
            controlCenter.Cell(2, 1).SetValue("Development Control\nWorkbook v1.0\nRoadmap v1.0");
            if (declaredSchemaVersion is not null)
            {
                controlCenter.Cell(3, 1).SetValue("Schema Version");
                controlCenter.Cell(3, 2).SetValue(declaredSchemaVersion.Value);
            }

            workbook.SaveAs(FilePath);
        }

        public string FilePath { get; }

        public void Dispose()
        {
            var dir = System.IO.Path.GetDirectoryName(FilePath);
            try
            {
                if (dir is not null && Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            }
            catch
            {
                // best-effort cleanup of a temp directory
            }
        }

        private static void WriteHeaders(IXLWorksheet sheet, int headerRow, IReadOnlyList<string> headers)
        {
            for (var column = 0; column < headers.Count; column++)
            {
                sheet.Cell(headerRow, column + 1).SetValue(headers[column]);
            }
        }
    }
}
