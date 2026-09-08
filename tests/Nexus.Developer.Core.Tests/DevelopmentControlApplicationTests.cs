using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Developer.Application.DevelopmentControl.Commands.CompleteDevelopmentControlWorkItem;
using Nexus.Developer.Application.DevelopmentControl.Commands.ReleaseDevelopmentControlReservation;
using Nexus.Developer.Application.DevelopmentControl.Commands.ReserveDevelopmentControlWorkItem;
using Nexus.Developer.Application.DevelopmentControl.Queries.GetActiveDevelopmentChanges;
using Nexus.Developer.Application.DevelopmentControl.Queries.GetDevelopmentControlNode;
using Nexus.Developer.Application.DevelopmentControl.Queries.GetDevelopmentControlState;
using Nexus.Developer.Application.DevelopmentControl.Queries.RunDevelopmentControlPreflight;
using Nexus.Developer.Core.DevelopmentControl;
using Nexus.Developer.Infrastructure;
using Nexus.Developer.Infrastructure.DevelopmentControl;
using Xunit;

namespace Nexus.Developer.Core.Tests;

// SP1-M05 (Lane A): the DevelopmentControl Application handlers and the Infrastructure
// composition root. These tests exercise the handlers over the REAL guarded store + REAL
// Excel adapter bound to a DISPOSABLE temp workbook -- the same governed write path a host
// caller drives (guarded atomic write, verify-while-locked) -- never the live
// NEXUS_DEVELOPMENT_CONTROL.xlsx. The composition-root facts prove the AddDevelopmentControl
// binding resolves the guarded store / lock factory / atomic-write coordinator and that a
// governed write through the DI-resolved store persists.
public class DevelopmentControlApplicationTests
{
    // ------------------------------------------------------------------ composition root

    [Fact]
    public void CompositionRoot_ResolvesGuardedStoreLockFactoryCoordinatorAndIdentity()
    {
        using var wb = new ControlTestWorkbook();
        var services = new ServiceCollection();
        services.AddDevelopmentControl(wb.FilePath, TimeSpan.FromSeconds(10));

        using var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<IDevelopmentControlStore>();
        var guarded = provider.GetRequiredService<IConcurrencyGuardedDevelopmentControlStore>();
        var factory = provider.GetRequiredService<IDevelopmentControlWriteLockFactory>();
        var coordinator = provider.GetRequiredService<IDevelopmentControlAtomicWriteCoordinator>();
        var identity = provider.GetRequiredService<DevelopmentControlMutexIdentity>();

        // The public IDevelopmentControlStore IS the guarded decorator; reads pass through,
        // writes are governed. The inner adapter is the Excel store over the canonical path.
        Assert.Same(guarded, store);
        Assert.IsType<NamedDevelopmentControlWriteLockFactory>(factory);
        Assert.IsType<ExcelDevelopmentControlStore>(guarded.Inner);
        Assert.NotNull(coordinator);
        Assert.Equal(DevelopmentControlMutexIdentity.FromWorkbookPath(Path.GetFullPath(wb.FilePath)), identity);
    }

    [Fact]
    public async Task CompositionRoot_DiResolvedStore_RunsAGuardedWrite()
    {
        using var wb = new ControlTestWorkbook();
        var services = new ServiceCollection();
        services.AddDevelopmentControl(wb.FilePath);

        using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<IDevelopmentControlStore>();

        var created = await store.CreateNodeAsync(
            NewNode("M-07-9", null, NodeType.Milestone, Status.Planned),
            Envelope(changeId: NextChangeId()));

        Assert.True(created.Success);
        var read = await store.GetNodeAsync(new NodeId("M-07-9"));
        Assert.NotNull(read);
        Assert.Equal("M-07-9", read!.NodeId.Value);
        Assert.Equal(1, read.RowVersion);
    }

    [Fact]
    public void CompositionRoot_ConfigurationWithoutWorkbookPathKey_ThrowsInvalidOperation()
    {
        var services = new ServiceCollection();
        var emptyConfig = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.AddDevelopmentControl(emptyConfig));

        Assert.Contains("WorkbookPath", ex.Message);
    }

    // ------------------------------------------------------------------ read queries

    [Fact]
    public async Task GetControlState_OverAnEmptyWorkbook_ReturnsTheZeroedSnapshot()
    {
        using var wb = new ControlTestWorkbook();
        var guard = await GuardAsync(wb);
        var handler = new GetDevelopmentControlStateHandler(guard);

        var state = await handler.HandleAsync(new GetDevelopmentControlStateQuery());

        Assert.NotNull(state);
        Assert.Equal(0, state!.CurrentNodeCount);
        Assert.Equal(0, state.ActiveChangeCount);
        Assert.Null(state.RootNodeId);
    }

    [Fact]
    public async Task GetNode_WhenTheNodeExists_ReturnsItsCurrentVersion()
    {
        using var wb = new ControlTestWorkbook();
        var guard = await GuardAsync(wb);
        await guard.CreateNodeAsync(
            NewNode("M-07-9", null, NodeType.Milestone, Status.Planned),
            Envelope(changeId: NextChangeId()));
        var handler = new GetDevelopmentControlNodeHandler(guard);

        var node = await handler.HandleAsync(new GetDevelopmentControlNodeQuery("M-07-9"));

        Assert.NotNull(node);
        Assert.Equal("M-07-9", node!.NodeId.Value);
    }

    [Fact]
    public async Task GetNode_WhenTheNodeIsUnknown_ReturnsNull()
    {
        using var wb = new ControlTestWorkbook();
        var guard = await GuardAsync(wb);
        var handler = new GetDevelopmentControlNodeHandler(guard);

        var node = await handler.HandleAsync(new GetDevelopmentControlNodeQuery("WI-07-NOPE"));

        Assert.Null(node);
    }

    [Fact]
    public async Task GetActiveChanges_BeforeAnyReservation_IsEmpty()
    {
        using var wb = new ControlTestWorkbook();
        var guard = await GuardAsync(wb);
        var handler = new GetActiveDevelopmentChangesHandler(guard);

        var changes = await handler.HandleAsync(new GetActiveDevelopmentChangesQuery());

        Assert.Empty(changes);
    }

    [Fact]
    public async Task RunPreflight_WithNoOpenChanges_IsClear()
    {
        using var wb = new ControlTestWorkbook();
        var guard = await GuardAsync(wb);
        await guard.CreateNodeAsync(
            NewNode("M-07-9", null, NodeType.Milestone, Status.Planned),
            Envelope(changeId: NextChangeId()));
        var handler = new RunDevelopmentControlPreflightHandler(guard);

        var result = await handler.HandleAsync(
            new RunDevelopmentControlPreflightQuery(
                new PreflightDeclaration(
                    ChangeId: NextChangeId(),
                    RoadmapNodeId: new NodeId("M-07-9"),
                    Repositories: new[] { "Nexus.Developer" },
                    Projects: Array.Empty<string>(),
                    FilesGlobs: Array.Empty<string>(),
                    SchemaOrDbContextMutation: false,
                    ContractsApis: Array.Empty<string>(),
                    Dependencies: Array.Empty<string>(),
                    Risk: "Low",
                    Worker: "test-agent",
                    Branch: "feature/x",
                    SiblingWorktree: null)));

        Assert.Equal(PreflightVerdict.Clear, result.Verdict);
    }

    // ------------------------------------------------------------------ governed mutations

    [Fact]
    public async Task Reserve_WithAReadyWorkItem_ReservesItAsAGuardedAtomicWrite()
    {
        using var wb = new ControlTestWorkbook();
        var guard = await GuardAsync(wb);
        await SeedWorkItemAsync(guard, "WI-07-2.1.1", parentId: "M-07-9");
        var handler = new ReserveDevelopmentControlWorkItemHandler(guard);
        var changeId = NextChangeId();

        var result = await handler.HandleAsync(
            new ReserveDevelopmentControlWorkItemCommand(
                NodeId: "WI-07-2.1.1",
                ChangeId: changeId,
                ActorName: "test-agent",
                Branch: "feature/m-08",
                Worktree: "w3",
                Reason: "SP1-M05 governed reserve"));

        Assert.True(result.Success);
        Assert.Equal(DevelopmentControlConcurrencyOutcome.Success, result.Outcome);
        Assert.Equal(Status.InProgress, result.Value!.Status);

        // The open change is on the register, owned by the worker, on the branch/worktree.
        var open = await guard.GetActiveChangesAsync();
        var change = Assert.Single(open);
        Assert.Equal(changeId, change.ChangeId);
        Assert.Equal("WI-07-2.1.1", change.NodeId);
        Assert.Equal("test-agent", change.Worker);
        Assert.Equal("feature/m-08", change.Branch);
        Assert.Equal("w3", change.Worktree);
    }

    [Fact]
    public async Task Release_AfterReserve_ReleasesTheOpenChange()
    {
        using var wb = new ControlTestWorkbook();
        var guard = await GuardAsync(wb);
        await SeedWorkItemAsync(guard, "WI-07-2.1.1", parentId: "M-07-9");
        var changeId = NextChangeId();
        await new ReserveDevelopmentControlWorkItemHandler(guard).HandleAsync(
            new ReserveDevelopmentControlWorkItemCommand("WI-07-2.1.1", changeId, "test-agent"));
        var handler = new ReleaseDevelopmentControlReservationHandler(guard);

        var result = await handler.HandleAsync(
            new ReleaseDevelopmentControlReservationCommand(
                NodeId: "WI-07-2.1.1",
                ChangeId: changeId,
                ActorName: "test-agent",
                Reason: "gave the work up"));

        Assert.True(result.Success);
        Assert.Equal(DevelopmentControlConcurrencyOutcome.Success, result.Outcome);
        var change = Assert.Single(await guard.GetActiveChangesAsync());
        Assert.StartsWith("Released --", change.Status);
    }

    [Fact]
    public async Task Complete_AfterReserve_MarksTheNodeCompletedAndClosesTheChange()
    {
        using var wb = new ControlTestWorkbook();
        var guard = await GuardAsync(wb);
        await SeedWorkItemAsync(guard, "WI-07-2.1.1", parentId: "M-07-9");
        var changeId = NextChangeId();
        await new ReserveDevelopmentControlWorkItemHandler(guard).HandleAsync(
            new ReserveDevelopmentControlWorkItemCommand("WI-07-2.1.1", changeId, "test-agent"));
        var handler = new CompleteDevelopmentControlWorkItemHandler(guard);

        var result = await handler.HandleAsync(
            new CompleteDevelopmentControlWorkItemCommand(
                NodeId: "WI-07-2.1.1",
                ChangeId: changeId,
                ActorName: "test-agent",
                ResultOrEvidence: "All gates green; integration verified."));

        Assert.True(result.Success);
        Assert.Equal(DevelopmentControlConcurrencyOutcome.Success, result.Outcome);
        Assert.Equal(Status.Completed, result.Value!.Status);
        Assert.Contains("All gates green", result.Value.Notes);

        // Completion closed the reservation, so the OPEN register (GetActiveChangesAsync =
        // not Completed/Cancelled) no longer lists it.
        Assert.Empty(await guard.GetActiveChangesAsync());
    }

    [Fact]
    public async Task Reserve_WhenTheNodeDoesNotExist_ReturnsNotFoundOutcome()
    {
        using var wb = new ControlTestWorkbook();
        var guard = await GuardAsync(wb);
        var handler = new ReserveDevelopmentControlWorkItemHandler(guard);

        var result = await handler.HandleAsync(
            new ReserveDevelopmentControlWorkItemCommand(
                NodeId: "WI-07-NOPE",
                ChangeId: NextChangeId(),
                ActorName: "test-agent"));

        Assert.False(result.Success);
        Assert.Equal(DevelopmentControlConcurrencyOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Reserve_WithAMalformedNodeId_ReturnsInvalidRequestOutcome()
    {
        using var wb = new ControlTestWorkbook();
        var guard = await GuardAsync(wb);
        var handler = new ReserveDevelopmentControlWorkItemHandler(guard);

        var result = await handler.HandleAsync(
            new ReserveDevelopmentControlWorkItemCommand(
                NodeId: "   ",
                ChangeId: NextChangeId(),
                ActorName: "test-agent"));

        Assert.False(result.Success);
        Assert.Equal(DevelopmentControlConcurrencyOutcome.InvalidRequest, result.Outcome);
    }

    [Fact]
    public async Task Reserve_WithoutAChangeId_ReturnsInvalidRequestOutcome()
    {
        using var wb = new ControlTestWorkbook();
        var guard = await GuardAsync(wb);
        await SeedWorkItemAsync(guard, "WI-07-2.1.1", parentId: "M-07-9");
        var handler = new ReserveDevelopmentControlWorkItemHandler(guard);

        var result = await handler.HandleAsync(
            new ReserveDevelopmentControlWorkItemCommand(
                NodeId: "WI-07-2.1.1",
                ChangeId: " ",
                ActorName: "test-agent"));

        Assert.False(result.Success);
        Assert.Equal(DevelopmentControlConcurrencyOutcome.InvalidRequest, result.Outcome);
    }

    [Fact]
    public async Task Complete_WithBlankEvidence_ReturnsInvalidRequestOutcome()
    {
        using var wb = new ControlTestWorkbook();
        var guard = await GuardAsync(wb);
        await SeedWorkItemAsync(guard, "WI-07-2.1.1", parentId: "M-07-9");
        var handler = new CompleteDevelopmentControlWorkItemHandler(guard);

        var result = await handler.HandleAsync(
            new CompleteDevelopmentControlWorkItemCommand(
                NodeId: "WI-07-2.1.1",
                ChangeId: NextChangeId(),
                ActorName: "test-agent",
                ResultOrEvidence: " "));

        Assert.False(result.Success);
        Assert.Equal(DevelopmentControlConcurrencyOutcome.InvalidRequest, result.Outcome);
    }

    // ------------------------------------------------------------------ helpers

    private static async Task<IConcurrencyGuardedDevelopmentControlStore> GuardAsync(ControlTestWorkbook wb)
    {
        // Each guard is built over its own Excel adapter instance but the SAME canonical path,
        // so the named-mutex identity is deterministic and guarded writes serialize correctly.
        var inner = new ExcelDevelopmentControlStore(wb.FilePath);
        var guard = new ConcurrencyGuardedDevelopmentControlStore(
            inner,
            new NamedDevelopmentControlWriteLockFactory(),
            DevelopmentControlMutexIdentity.FromWorkbookPath(wb.FilePath));
        await Task.CompletedTask;
        return guard;
    }

    private static async Task SeedWorkItemAsync(
        IConcurrencyGuardedDevelopmentControlStore guard,
        string workItemId,
        string parentId)
    {
        var parent = await guard.CreateNodeAsync(
            NewNode(parentId, null, NodeType.Milestone, Status.Planned),
            Envelope(changeId: NextChangeId()));
        Assert.True(parent.Success);

        var workItem = await guard.CreateNodeAsync(
            NewNode(workItemId, new NodeId(parentId), NodeType.WorkItem, Status.Ready),
            Envelope(changeId: NextChangeId()));
        Assert.True(workItem.Success);
    }

    private static Node NewNode(string id, NodeId? parent, NodeType type, Status status) => new(
        new NodeId(id), parent, type, "", "", "03", null, "Node " + id, null,
        Array.Empty<NodeId>(), false, Array.Empty<string>(), Array.Empty<string>(),
        Array.Empty<string>(), Array.Empty<string>(), null, null, status, false,
        null, null, null, "Durai", null, null, 0, false, "test", null,
        DateTimeOffset.MinValue, DateTimeOffset.MinValue);

    private static MutationEnvelope Envelope(string changeId, int? expectedRowVersion = null) => new(
        expectedRowVersion,
        new ActorRef(ActorType.Agent, "test-agent", "Test Agent"),
        "Nexus.Developer.Core.Tests", null, "session-1", null, changeId, null, null,
        "test reason");

    private static string NextChangeId() => "CHG-20260907-" + Guid.NewGuid().ToString("N").Substring(0, 6);

    // Builds the six-sheet development-control workbook (live header layouts) at a unique
    // temp path and deletes it on Dispose -- the same layout the ExcelDevelopmentControlStore
    // integration tests use. Tests never touch the real NEXUS_DEVELOPMENT_CONTROL.xlsx.
    private sealed class ControlTestWorkbook : IDisposable
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

        public ControlTestWorkbook()
        {
            var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"nexus-dev-app-{Guid.NewGuid():N}"));
            FilePath = Path.Combine(dir.FullName, "NEXUS_DEVELOPMENT_CONTROL.xlsx");
            using var workbook = new XLWorkbook();

            WriteHeaders(workbook.AddWorksheet("Master Roadmap"), 5, MasterRoadmapHeaders);
            WriteHeaders(workbook.AddWorksheet("Version History"), 5, VersionHistoryHeaders);
            WriteHeaders(workbook.AddWorksheet("Active Changes"), 5, ActiveChangesHeaders);
            WriteHeaders(workbook.AddWorksheet("Audit Findings"), 5, AuditFindingsHeaders);
            WriteHeaders(workbook.AddWorksheet("Activity Log"), 4, ActivityLogHeaders);

            var controlCenter = workbook.AddWorksheet("Control Center");
            controlCenter.Cell(2, 1).SetValue("Development Control\nWorkbook v1.0\nRoadmap v1.0");

            workbook.SaveAs(FilePath);
        }

        public string FilePath { get; }

        public void Dispose()
        {
            var dir = Path.GetDirectoryName(FilePath);
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
