using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Dependencies;
using Xunit;

namespace Nexus.Developer.Core.Tests;

public class WorkItemDependencyTests
{
    [Fact]
    public void Create_StoresAllFields()
    {
        var id = WorkItemDependencyId.New();
        var upstreamId = Guid.NewGuid();
        var downstreamId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var edge = new WorkItemDependency(
            id,
            WorkItemDependencyNodeType.Feature,
            upstreamId,
            WorkItemDependencyNodeType.Issue,
            downstreamId,
            WorkItemDependencyKind.Blocking,
            WorkItemDependencyRequiredState.PrApproved,
            createdByUserId,
            createdAt);

        Assert.Equal(id, edge.Id);
        Assert.Equal(WorkItemDependencyNodeType.Feature, edge.UpstreamType);
        Assert.Equal(upstreamId, edge.UpstreamId);
        Assert.Equal(WorkItemDependencyNodeType.Issue, edge.DownstreamType);
        Assert.Equal(downstreamId, edge.DownstreamId);
        Assert.Equal(WorkItemDependencyKind.Blocking, edge.Kind);
        Assert.Equal(WorkItemDependencyRequiredState.PrApproved, edge.RequiredState);
        Assert.Equal(createdByUserId, edge.CreatedByUserId);
        Assert.Equal(createdAt, edge.CreatedAt);
    }

    [Fact]
    public void Create_SelfLoop_Throws()
    {
        var id = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() =>
            new WorkItemDependency(
                WorkItemDependencyId.New(),
                WorkItemDependencyNodeType.Task, id,
                WorkItemDependencyNodeType.Task, id,
                WorkItemDependencyKind.Blocking,
                requiredState: null,
                Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_SameIdDifferentTypes_IsAllowed()
    {
        // The self-loop check is (type + id), not id alone: a Feature and a Task
        // sharing a Guid are two distinct nodes and may be connected.
        var id = Guid.NewGuid();

        var edge = new WorkItemDependency(
            WorkItemDependencyId.New(),
            WorkItemDependencyNodeType.Feature, id,
            WorkItemDependencyNodeType.Task, id,
            WorkItemDependencyKind.Parallel,
            requiredState: null,
            Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(WorkItemDependencyNodeType.Feature, edge.UpstreamType);
        Assert.Equal(WorkItemDependencyNodeType.Task, edge.DownstreamType);
    }

    [Theory]
    [InlineData(WorkItemDependencyKind.Parallel)]
    [InlineData(WorkItemDependencyKind.Informational)]
    public void Create_RequiredStateOnNonBlocking_Throws(WorkItemDependencyKind kind)
    {
        Assert.Throws<ArgumentException>(() =>
            new WorkItemDependency(
                WorkItemDependencyId.New(),
                WorkItemDependencyNodeType.Feature, Guid.NewGuid(),
                WorkItemDependencyNodeType.Task, Guid.NewGuid(),
                kind,
                WorkItemDependencyRequiredState.Merged,
                Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_RequiredStateOnBlocking_IsAllowed()
    {
        var edge = new WorkItemDependency(
            WorkItemDependencyId.New(),
            WorkItemDependencyNodeType.Feature, Guid.NewGuid(),
            WorkItemDependencyNodeType.Task, Guid.NewGuid(),
            WorkItemDependencyKind.Blocking,
            WorkItemDependencyRequiredState.PlanStable,
            Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(WorkItemDependencyRequiredState.PlanStable, edge.RequiredState);
    }

    [Fact]
    public void Create_BlockingWithNullRequiredState_MeansFullCompletion()
    {
        var edge = new WorkItemDependency(
            WorkItemDependencyId.New(),
            WorkItemDependencyNodeType.Feature, Guid.NewGuid(),
            WorkItemDependencyNodeType.Task, Guid.NewGuid(),
            WorkItemDependencyKind.Blocking,
            requiredState: null,
            Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Null(edge.RequiredState);
    }

    [Theory]
    [InlineData(WorkItemDependencyNodeType.Feature)]
    [InlineData(WorkItemDependencyNodeType.Task)]
    [InlineData(WorkItemDependencyNodeType.Subtask)]
    [InlineData(WorkItemDependencyNodeType.Milestone)]
    [InlineData(WorkItemDependencyNodeType.Issue)]
    public void Create_AcceptsEveryAllowedNodeType(WorkItemDependencyNodeType nodeType)
    {
        var edge = new WorkItemDependency(
            WorkItemDependencyId.New(),
            nodeType, Guid.NewGuid(),
            WorkItemDependencyNodeType.Task, Guid.NewGuid(),
            WorkItemDependencyKind.Informational,
            requiredState: null,
            Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(nodeType, edge.UpstreamType);
    }

    [Fact]
    public void Create_RejectsUndefinedKind()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WorkItemDependency(
                WorkItemDependencyId.New(),
                WorkItemDependencyNodeType.Feature, Guid.NewGuid(),
                WorkItemDependencyNodeType.Task, Guid.NewGuid(),
                (WorkItemDependencyKind)0,
                requiredState: null,
                Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_RejectsUndefinedNodeType()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WorkItemDependency(
                WorkItemDependencyId.New(),
                (WorkItemDependencyNodeType)0, Guid.NewGuid(),
                WorkItemDependencyNodeType.Task, Guid.NewGuid(),
                WorkItemDependencyKind.Blocking,
                requiredState: null,
                Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_RejectsUndefinedRequiredState()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WorkItemDependency(
                WorkItemDependencyId.New(),
                WorkItemDependencyNodeType.Feature, Guid.NewGuid(),
                WorkItemDependencyNodeType.Task, Guid.NewGuid(),
                WorkItemDependencyKind.Blocking,
                (WorkItemDependencyRequiredState)0,
                Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    // --- Reason (SP1-M04): optional additive descriptive text --------------------

    [Fact]
    public void Create_WhenNoReasonSupplied_DefaultsToNull()
    {
        var edge = new WorkItemDependency(
            WorkItemDependencyId.New(),
            WorkItemDependencyNodeType.Feature, Guid.NewGuid(),
            WorkItemDependencyNodeType.Task, Guid.NewGuid(),
            WorkItemDependencyKind.Blocking,
            requiredState: null,
            Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Null(edge.Reason);
    }

    [Fact]
    public void Create_WithAReason_RoundTripsTheTrimmedValue()
    {
        var edge = new WorkItemDependency(
            WorkItemDependencyId.New(),
            WorkItemDependencyNodeType.Feature, Guid.NewGuid(),
            WorkItemDependencyNodeType.Task, Guid.NewGuid(),
            WorkItemDependencyKind.Informational,
            requiredState: null,
            Guid.NewGuid(), DateTimeOffset.UtcNow,
            reason: "  Blocks the P1 rollout until the API contract is stable.  ");

        Assert.Equal("Blocks the P1 rollout until the API contract is stable.", edge.Reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData(" \r\n ")]
    public void Create_BlankOrWhitespaceReason_IsNormalizedToNull(string? reason)
    {
        var edge = new WorkItemDependency(
            WorkItemDependencyId.New(),
            WorkItemDependencyNodeType.Feature, Guid.NewGuid(),
            WorkItemDependencyNodeType.Task, Guid.NewGuid(),
            WorkItemDependencyKind.Parallel,
            requiredState: null,
            Guid.NewGuid(), DateTimeOffset.UtcNow,
            reason: reason);

        Assert.Null(edge.Reason);
    }

    [Fact]
    public void Create_WithReason_DoesNotAffectGraphOrGuardSemantics()
    {
        // Reason is purely descriptive: the self-dependency guard, RequiredState rules and kind
        // semantics are unchanged when a reason is present.
        var selfLoopId = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() =>
            new WorkItemDependency(
                WorkItemDependencyId.New(),
                WorkItemDependencyNodeType.Task, selfLoopId,
                WorkItemDependencyNodeType.Task, selfLoopId,
                WorkItemDependencyKind.Blocking,
                requiredState: null,
                Guid.NewGuid(), DateTimeOffset.UtcNow,
                reason: "self loop with reason"));

        Assert.Throws<ArgumentException>(() =>
            new WorkItemDependency(
                WorkItemDependencyId.New(),
                WorkItemDependencyNodeType.Feature, Guid.NewGuid(),
                WorkItemDependencyNodeType.Task, Guid.NewGuid(),
                WorkItemDependencyKind.Parallel,
                WorkItemDependencyRequiredState.Merged,
                Guid.NewGuid(), DateTimeOffset.UtcNow,
                reason: "required state on parallel edge with reason"));

        var edge = new WorkItemDependency(
            WorkItemDependencyId.New(),
            WorkItemDependencyNodeType.Feature, Guid.NewGuid(),
            WorkItemDependencyNodeType.Task, Guid.NewGuid(),
            WorkItemDependencyKind.Blocking,
            WorkItemDependencyRequiredState.PrApproved,
            Guid.NewGuid(), DateTimeOffset.UtcNow,
            reason: "pr approved reason");
        Assert.Equal(WorkItemDependencyKind.Blocking, edge.Kind);
        Assert.Equal(WorkItemDependencyRequiredState.PrApproved, edge.RequiredState);
        Assert.Equal("pr approved reason", edge.Reason);
    }
}
