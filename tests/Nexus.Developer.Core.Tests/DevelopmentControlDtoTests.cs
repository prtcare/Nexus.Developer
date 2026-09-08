using Nexus.Developer.Core.DevelopmentControl;
using Xunit;

namespace Nexus.Developer.Core.Tests;

// Construction/validation tests for the WI-07-0.2.1 Development Control DTOs and enums,
// in the same style as WorkItemDependencyTests.cs: NodeId rejects empty/whitespace, each
// enum's members match the specification exactly, and the record field counts lock the
// contracts (Node 31, ActiveChange 30, AuditFinding 13, ActivityLogEntry 34) so a later
// adapter can never silently drift from them.
public class DevelopmentControlDtoTests
{
    // --- NodeId ---------------------------------------------------------------------

    [Fact]
    public void NodeId_RejectsNull() =>
        Assert.Throws<ArgumentException>(() => new NodeId(null!));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData(" \r\n ")]
    public void NodeId_RejectsEmptyOrWhitespace(string value) =>
        Assert.Throws<ArgumentException>(() => new NodeId(value));

    [Theory]
    [InlineData("WI-07-2.1.1")]
    [InlineData("M-07-2.1")]
    [InlineData("F-07-10")]
    [InlineData("01")]
    [InlineData("CHG-20260830-014")]
    public void NodeId_AcceptsTheRoadmapIdSchemeVerbatim(string value) =>
        Assert.Equal(value, new NodeId(value).Value);

    [Fact]
    public void NodeId_TrimsSurroundingWhitespace() =>
        Assert.Equal("M-07-2.1", new NodeId("  M-07-2.1 ").Value);

    [Fact]
    public void NodeId_ToStringReturnsTheRawId() =>
        Assert.Equal("F-07-10", new NodeId("F-07-10").ToString());

    [Fact]
    public void NodeId_BacksOntoAStringNotAGuid()
    {
        // Every other aggregate id in this repo is a Guid wrapper; the Development
        // Control node id is deliberately the human-authored roadmap string instead.
        var id = new NodeId("WI-07-2.1.1");
        Assert.Equal(typeof(string), id.Value.GetType());
    }

    // --- Enums ----------------------------------------------------------------------

    [Fact]
    public void NodeType_MembersMatchSpecificationExactly() =>
        Assert.Equal(
            new[] { "Layer", "Release", "Feature", "Milestone", "WorkItem", "Task", "Subtask" },
            Enum.GetNames<NodeType>());

    [Fact]
    public void Status_MembersMatchSpecificationIncludingSuperseded() =>
        Assert.Equal(
            new[]
            {
                "Proposed", "Planned", "Ready", "InProgress", "Blocked", "InReview",
                "Completed", "Cancelled", "Deferred", "Obsolete", "Superseded"
            },
            Enum.GetNames<Status>());

    [Fact]
    public void PreflightVerdict_MembersMatchAgentsMdExactly() =>
        Assert.Equal(
            new[] { "Clear", "DependencyFound", "OverlapFound", "ConflictFound", "ArchitectureConflict" },
            Enum.GetNames<PreflightVerdict>());

    [Fact]
    public void ActorType_HasExactlyHumanAndAgent() =>
        Assert.Equal(new[] { "Human", "Agent" }, Enum.GetNames<ActorType>());

    // --- Record field counts (contract lock) ----------------------------------------

    [Fact]
    public void Node_HasExactlyThe31SpecifiedFields() =>
        Assert.Equal(31, ParameterCount<Node>());

    [Fact]
    public void ActiveChange_HasExactlyThe30WorkbookSheetColumns() =>
        Assert.Equal(30, ParameterCount<ActiveChange>());

    [Fact]
    public void AuditFinding_HasExactlyThe13WorkbookSheetColumns() =>
        Assert.Equal(13, ParameterCount<AuditFinding>());

    [Fact]
    public void ActivityLogEntry_HasExactlyThe34SpecifiedFields() =>
        Assert.Equal(34, ParameterCount<ActivityLogEntry>());

    // --- Construction / round-trip --------------------------------------------------

    [Fact]
    public void Node_ConstructsAndRoundTripsEveryField()
    {
        var createdAt = new DateTimeOffset(2026, 8, 30, 10, 0, 0, TimeSpan.Zero);
        var node = new Node(
            NodeId: new NodeId("WI-07-2.1.1"),
            ParentId: new NodeId("M-07-2.1"),
            NodeType: NodeType.WorkItem,
            SortKey: "03.001.001",
            Path: "03 DEVELOPMENT > M-07-2.1 Dependency graph > WI-07-2.1.1",
            Layer: "03",
            Phase: "P1",
            Name: "Dependency model and traversal",
            Outcome: "The dependency graph is modeled and traversable.",
            Dependencies: new[] { new NodeId("F-07-10") },
            ParallelSafe: false,
            Projects: new[] { "Nexus.Developer" },
            FilesGlobs: new[] { "src/Nexus.Developer.Core/**" },
            SchemaContexts: new[] { "NexusDeveloperDbContext" },
            ContractsApis: new[] { "No public contract changed" },
            Gate: "GATE_A",
            AcceptanceCriteria: "Four acceptance criteria",
            Status: Status.InProgress,
            BreakdownComplete: true,
            ManualProgress: 100,
            DerivedProgress: 50,
            ReportedProgress: 75,
            Owner: "Durai",
            Priority: "High",
            Risk: "Medium",
            RowVersion: 3,
            IsDeleted: false,
            Source: "nexus-roadmap.yaml v2.2",
            Notes: "Append-only control",
            CreatedAt: createdAt,
            UpdatedAt: createdAt.AddDays(1));

        Assert.Equal(new NodeId("WI-07-2.1.1"), node.NodeId);
        Assert.Equal(new NodeId("M-07-2.1"), node.ParentId);
        Assert.Equal(NodeType.WorkItem, node.NodeType);
        Assert.Equal("03.001.001", node.SortKey);
        Assert.Equal("03", node.Layer);
        Assert.Equal("P1", node.Phase);
        Assert.Equal("Dependency model and traversal", node.Name);
        Assert.False(node.ParallelSafe);
        Assert.Equal(new NodeId("F-07-10"), Assert.Single(node.Dependencies));
        Assert.Equal(Status.InProgress, node.Status);
        Assert.True(node.BreakdownComplete);
        Assert.Equal(100, node.ManualProgress);
        Assert.Equal(50, node.DerivedProgress);
        Assert.Equal(75, node.ReportedProgress);
        Assert.Equal(3, node.RowVersion);
        Assert.False(node.IsDeleted);
        Assert.Equal(createdAt, node.CreatedAt);
        Assert.Equal(createdAt.AddDays(1), node.UpdatedAt);
    }

    [Fact]
    public void MutationResult_CarriesSuccessValueConflictAndLogId()
    {
        var result = new MutationResult<Node>(
            Success: true,
            Value: new Node(
                new NodeId("WI-07-2.1.1"), null, NodeType.WorkItem, "03.001.001", "Path",
                "03", "P1", "Name", null, Array.Empty<NodeId>(), false,
                Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(),
                Array.Empty<string>(), null, null, Status.Planned, false, null, null,
                null, "Durai", null, null, 1, false, null, null,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
            Conflict: false,
            ConflictDetails: null,
            ValidationErrors: Array.Empty<string>(),
            ActivityLogEntryId: "ACT-20260830-001");

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.False(result.Conflict);
        Assert.Null(result.ConflictDetails);
        Assert.Equal("ACT-20260830-001", result.ActivityLogEntryId);
    }

    [Fact]
    public void MutationEnvelope_CarriesExpectedRowVersionAndProvenance()
    {
        var envelope = new MutationEnvelope(
            ExpectedRowVersion: 3,
            Actor: new ActorRef(ActorType.Agent, "codex-1", "Codex"),
            Source: "Nexus.Developer",
            ChatPlatform: null,
            SessionId: "session-1",
            PromptId: "prompt-1",
            ChangeId: "CHG-20260830-014",
            CorrelationId: "correlation-1",
            IdempotencyKey: "idem-1",
            Reason: "Reviewer correction");

        Assert.Equal(3, envelope.ExpectedRowVersion);
        Assert.Equal(ActorType.Agent, envelope.Actor.Type);
        Assert.Equal("Codex", envelope.Actor.Name);
        Assert.Equal("CHG-20260830-014", envelope.ChangeId);
        Assert.Equal("idem-1", envelope.IdempotencyKey);
    }

    [Fact]
    public void PreflightDeclaration_And_Result_RoundTrip()
    {
        var declaration = new PreflightDeclaration(
            ChangeId: "CHG-20260830-014",
            RoadmapNodeId: new NodeId("WI-07-2.1.1"),
            Repositories: new[] { "Nexus.Developer" },
            Projects: new[] { "Control and dependency contracts" },
            FilesGlobs: new[] { "src/Nexus.Developer.Core/DevelopmentControl/**" },
            SchemaOrDbContextMutation: false,
            ContractsApis: Array.Empty<string>(),
            Dependencies: new[] { "Nexus.ProductCore.Contracts" },
            Risk: "Low",
            Worker: "Codex",
            Branch: "feat/wI-07-0-2-1",
            SiblingWorktree: null);

        var result = new PreflightResult(
            PreflightVerdict.Clear,
            Detail: null,
            Findings: Array.Empty<string>());

        Assert.Equal("CHG-20260830-014", declaration.ChangeId);
        Assert.Equal(new NodeId("WI-07-2.1.1"), declaration.RoadmapNodeId);
        Assert.False(declaration.SchemaOrDbContextMutation);
        Assert.Equal(PreflightVerdict.Clear, result.Verdict);
        Assert.True(result.Findings.Count == 0);
    }

    // --- IDevelopmentControlStore contract ------------------------------------------

    [Fact]
    public void IDevelopmentControlStore_DeclaresExactlyThe22NamedOperations()
    {
        var expected = new[]
        {
            "GetControlStateAsync", "GetNodeAsync", "GetSubtreeAsync", "SearchNodesAsync",
            "CreateNodeAsync", "UpdateNodeAsync", "ReparentNodeAsync", "RetireNodeAsync",
            "AddDependencyAsync", "RemoveDependencyAsync", "ReserveWorkItemAsync",
            "StartActivityAsync", "RecordHeartbeatAsync", "CompleteActivityAsync",
            "FailActivityAsync", "ReleaseReservationAsync", "CompleteWorkItemAsync",
            "RunPreflightAsync", "GetActiveChangesAsync", "GetNextExecutableWorkItemAsync",
            "GetActivityLogAsync", "ValidateControlStoreAsync"
        };

        var actual = typeof(IDevelopmentControlStore)
            .GetMethods()
            .Select(method => method.Name)
            .ToArray();

        Assert.Equal(expected.OrderBy(name => name), actual.OrderBy(name => name));
    }

    [Theory]
    [InlineData("CreateNodeAsync")]
    [InlineData("UpdateNodeAsync")]
    [InlineData("ReparentNodeAsync")]
    [InlineData("RetireNodeAsync")]
    [InlineData("AddDependencyAsync")]
    [InlineData("RemoveDependencyAsync")]
    [InlineData("ReserveWorkItemAsync")]
    [InlineData("StartActivityAsync")]
    [InlineData("RecordHeartbeatAsync")]
    [InlineData("CompleteActivityAsync")]
    [InlineData("FailActivityAsync")]
    [InlineData("ReleaseReservationAsync")]
    [InlineData("CompleteWorkItemAsync")]
    public void EveryMutatingOperation_TakesAMutationEnvelopeAndReturnsAMutationResult(string operationName)
    {
        var method = typeof(IDevelopmentControlStore).GetMethod(operationName);
        Assert.NotNull(method);

        Assert.Contains(
            method!.GetParameters(),
            parameter => parameter.ParameterType == typeof(MutationEnvelope));

        // The operations are async, so the declared return type is Task<MutationResult<...>>;
        // unwrap the Task<> to assert the payload is MutationResult<T>.
        var payloadType = method.ReturnType;
        if (payloadType.IsGenericType &&
            payloadType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            payloadType = payloadType.GetGenericArguments()[0];
        }

        Assert.True(payloadType.IsGenericType);
        Assert.Equal(typeof(MutationResult<>).Name, payloadType.Name);
        Assert.Equal(typeof(MutationResult<>).Namespace, payloadType.Namespace);
    }

    [Theory]
    [InlineData("RunPreflightAsync")]
    [InlineData("ValidateControlStoreAsync")]
    public void CheckOnlyOperations_TakeNoMutationEnvelope(string operationName)
    {
        var method = typeof(IDevelopmentControlStore).GetMethod(operationName);
        Assert.NotNull(method);
        Assert.DoesNotContain(
            method!.GetParameters(),
            parameter => parameter.ParameterType == typeof(MutationEnvelope));
    }

    private static int ParameterCount<T>() =>
        typeof(T).GetConstructors().Single().GetParameters().Length;
}
