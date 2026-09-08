using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Dependencies;
using Xunit;

namespace Nexus.Developer.Core.Tests;

public class DependencyGraphTraversalTests
{
    private static readonly WorkItemDependencyNodeType T = WorkItemDependencyNodeType.Task;

    [Fact]
    public void GetBlockingChain_MultiHop_ReturnsNearestFirst()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var edges = new[]
        {
            Edge(T, a, T, b),
            Edge(T, b, T, c)
        };

        var chain = DependencyGraphTraversal.GetBlockingChain(edges, T, c);

        Assert.Equal(
            new[] { (T, b), (T, a) },
            chain);
    }

    [Fact]
    public void GetBlockingChain_Diamond_ReturnsAllPredecessorsWithoutRepeat()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var d = Guid.NewGuid();
        var edges = new[]
        {
            Edge(T, a, T, b),
            Edge(T, a, T, c),
            Edge(T, b, T, d),
            Edge(T, c, T, d)
        };

        var chain = DependencyGraphTraversal.GetBlockingChain(edges, T, d);

        Assert.Equal(
            new[] { (T, b), (T, c), (T, a) },
            chain);
    }

    [Fact]
    public void GetBlockingChain_NoBlockers_ReturnsEmpty()
    {
        var edges = new[] { Edge(T, Guid.NewGuid(), T, Guid.NewGuid()) };

        var chain = DependencyGraphTraversal.GetBlockingChain(edges, T, Guid.NewGuid());

        Assert.Empty(chain);
    }

    [Fact]
    public void GetBlockingChain_MixedTypes_DistinguishesNodesByTypeAndId()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var edges = new[]
        {
            Edge(WorkItemDependencyNodeType.Feature, a, T, b),
            Edge(T, b, WorkItemDependencyNodeType.Subtask, c)
        };

        var chain = DependencyGraphTraversal.GetBlockingChain(
            edges, WorkItemDependencyNodeType.Subtask, c);

        Assert.Equal(
            new[] { (T, b), (WorkItemDependencyNodeType.Feature, a) },
            chain);
    }

    [Fact]
    public void GetBlockingChain_IgnoresNonBlockingEdges()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var d = Guid.NewGuid();
        var edges = new[]
        {
            Edge(T, a, T, b),
            Edge(T, c, T, b, WorkItemDependencyKind.Parallel),
            Edge(T, d, T, b, WorkItemDependencyKind.Informational)
        };

        var chainOfB = DependencyGraphTraversal.GetBlockingChain(edges, T, b);
        var chainOfC = DependencyGraphTraversal.GetBlockingChain(edges, T, c);

        Assert.Equal(new[] { (T, a) }, chainOfB);
        Assert.Empty(chainOfC);
    }

    [Fact]
    public void FindCycleIfAdded_NoPath_ReturnsEmpty()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var d = Guid.NewGuid();
        var edges = new[] { Edge(T, a, T, b) };

        var cycle = DependencyGraphTraversal.FindCycleIfAdded(edges, T, c, T, d);

        Assert.Empty(cycle);
    }

    [Fact]
    public void FindCycleIfAdded_TwoNodeCycle_ReturnsFullPath()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var edges = new[] { Edge(T, a, T, b) };

        var cycle = DependencyGraphTraversal.FindCycleIfAdded(edges, upstreamType: T, upstreamId: b, downstreamType: T, downstreamId: a);

        Assert.Equal(new[] { (T, a), (T, b), (T, a) }, cycle);
    }

    [Fact]
    public void FindCycleIfAdded_ThreeNodeCycle_ReturnsFullPath()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var edges = new[]
        {
            Edge(T, a, T, b),
            Edge(T, b, T, c)
        };

        var cycle = DependencyGraphTraversal.FindCycleIfAdded(edges, upstreamType: T, upstreamId: c, downstreamType: T, downstreamId: a);

        Assert.Equal(new[] { (T, a), (T, b), (T, c), (T, a) }, cycle);
    }

    [Fact]
    public void FindCycleIfAdded_LongerChain_ReturnsFullPath()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var d = Guid.NewGuid();
        var edges = new[]
        {
            Edge(T, a, T, b),
            Edge(T, b, T, c),
            Edge(T, c, T, d)
        };

        var cycle = DependencyGraphTraversal.FindCycleIfAdded(edges, upstreamType: T, upstreamId: d, downstreamType: T, downstreamId: a);

        Assert.Equal(new[] { (T, a), (T, b), (T, c), (T, d), (T, a) }, cycle);
    }

    [Fact]
    public void FindCycleIfAdded_DiamondAddThatDoesNotCloseACycle_ReturnsEmpty()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var d = Guid.NewGuid();
        var edges = new[]
        {
            Edge(T, a, T, b),
            Edge(T, a, T, c),
            Edge(T, b, T, d),
            Edge(T, c, T, d)
        };

        // B -> C within the diamond creates no cycle: no path C -> ... -> B exists.
        var cycle = DependencyGraphTraversal.FindCycleIfAdded(edges, upstreamType: T, upstreamId: b, downstreamType: T, downstreamId: c);

        Assert.Empty(cycle);
    }

    [Fact]
    public void FindCycleIfAdded_IgnoresNonBlockingEdges()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var edges = new[]
        {
            Edge(T, b, T, a, WorkItemDependencyKind.Parallel)
        };

        // Adding A -> B: B cannot reach A through Blocking edges (B -> A is
        // Parallel and ignored), so no cycle even though the reverse edge exists.
        var cycle = DependencyGraphTraversal.FindCycleIfAdded(edges, upstreamType: T, upstreamId: a, downstreamType: T, downstreamId: b);

        Assert.Empty(cycle);
    }

    [Fact]
    public void FindCycleIfAdded_SelfLoop_ReturnsSelfCycle()
    {
        var a = Guid.NewGuid();

        var cycle = DependencyGraphTraversal.FindCycleIfAdded(Array.Empty<WorkItemDependency>(), upstreamType: T, upstreamId: a, downstreamType: T, downstreamId: a);

        Assert.Equal(new[] { (T, a), (T, a) }, cycle);
    }

    [Fact]
    public void FindCycleIfAdded_ExistingCycleElsewhere_DoesNotAffectUnrelatedAdd()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var d = Guid.NewGuid();
        var edges = new[]
        {
            Edge(T, a, T, b),
            Edge(T, b, T, a) // already a two-node cycle, outside the add
        };

        var cycle = DependencyGraphTraversal.FindCycleIfAdded(edges, upstreamType: T, upstreamId: c, downstreamType: T, downstreamId: d);

        Assert.Empty(cycle);
    }

    private static WorkItemDependency Edge(
        WorkItemDependencyNodeType upstreamType,
        Guid upstreamId,
        WorkItemDependencyNodeType downstreamType,
        Guid downstreamId,
        WorkItemDependencyKind kind = WorkItemDependencyKind.Blocking,
        WorkItemDependencyRequiredState? requiredState = null)
        => new(
            WorkItemDependencyId.New(),
            upstreamType,
            upstreamId,
            downstreamType,
            downstreamId,
            kind,
            requiredState,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
}
