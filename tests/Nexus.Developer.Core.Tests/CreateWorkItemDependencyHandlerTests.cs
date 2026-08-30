using Nexus.Developer.Application.Dependencies;
using Nexus.Developer.Application.Dependencies.Commands.CreateWorkItemDependency;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Dependencies;
using Nexus.Developer.Core.Features;
using Nexus.Developer.Core.Issues;
using Nexus.Developer.Core.Milestones;
using Nexus.Developer.Core.Subtasks;
using DeveloperTask = Nexus.Developer.Core.Tasks.Task;
using ITaskRepository = Nexus.Developer.Core.Tasks.ITaskRepository;
using Xunit;

namespace Nexus.Developer.Core.Tests;

public class CreateWorkItemDependencyHandlerTests
{
    private static readonly WorkItemDependencyNodeType T = WorkItemDependencyNodeType.Task;

    [Fact]
    public async Task Create_WhenBothTargetsExist_CreatesEdge()
    {
        var upstreamId = Guid.NewGuid();
        var downstreamId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();
        var dependencyRepo = new InMemoryWorkItemDependencyRepository();
        var handler = CreateHandler(
            feature: new FakeFeatureRepository(),
            task: new FakeTaskRepository(),
            dependencies: dependencyRepo);

        var result = await handler.HandleAsync(
            new CreateWorkItemDependencyCommand(
                WorkItemDependencyNodeType.Feature,
                upstreamId,
                T,
                downstreamId,
                WorkItemDependencyKind.Blocking,
                WorkItemDependencyRequiredState.Merged,
                createdByUserId));

        var edge = Assert.Single(dependencyRepo.Dependencies);

        // Result shape.
        Assert.Equal(edge.Id, result.WorkItemDependencyId);
        Assert.Equal(WorkItemDependencyNodeType.Feature, result.UpstreamType);
        Assert.Equal(upstreamId, result.UpstreamId);
        Assert.Equal(T, result.DownstreamType);
        Assert.Equal(downstreamId, result.DownstreamId);
        Assert.Equal(WorkItemDependencyKind.Blocking, result.Kind);
        Assert.Equal(WorkItemDependencyRequiredState.Merged, result.RequiredState);
        Assert.Equal(createdByUserId, result.CreatedByUserId);

        // Edge carried the command's fields.
        Assert.Equal(WorkItemDependencyNodeType.Feature, edge.UpstreamType);
        Assert.Equal(upstreamId, edge.UpstreamId);
        Assert.Equal(T, edge.DownstreamType);
        Assert.Equal(downstreamId, edge.DownstreamId);
        Assert.Equal(WorkItemDependencyKind.Blocking, edge.Kind);
        Assert.Equal(WorkItemDependencyRequiredState.Merged, edge.RequiredState);
    }

    [Fact]
    public async Task Create_BlockingEdgeWithoutRequiredState_MeansFullCompletion()
    {
        var handler = CreateHandler(dependencies: new InMemoryWorkItemDependencyRepository());

        var result = await handler.HandleAsync(
            new CreateWorkItemDependencyCommand(
                WorkItemDependencyNodeType.Feature,
                Guid.NewGuid(),
                T,
                Guid.NewGuid(),
                WorkItemDependencyKind.Blocking,
                RequiredState: null,
                Guid.NewGuid()));

        Assert.Null(result.RequiredState);
    }

    [Theory]
    [InlineData(WorkItemDependencyNodeType.Feature)]
    [InlineData(WorkItemDependencyNodeType.Task)]
    [InlineData(WorkItemDependencyNodeType.Subtask)]
    [InlineData(WorkItemDependencyNodeType.Milestone)]
    [InlineData(WorkItemDependencyNodeType.Issue)]
    public async Task Create_WithExistingTargetOfAnyType_AsUpstream_CreatesEdge(WorkItemDependencyNodeType upstreamType)
    {
        var dependencyRepo = new InMemoryWorkItemDependencyRepository();
        var handler = CreateHandler(
            feature: upstreamType == WorkItemDependencyNodeType.Feature ? new FakeFeatureRepository() : null,
            task: upstreamType == T ? new FakeTaskRepository() : null,
            subtask: upstreamType == WorkItemDependencyNodeType.Subtask ? new FakeSubtaskRepository() : null,
            milestone: upstreamType == WorkItemDependencyNodeType.Milestone ? new FakeMilestoneRepository() : null,
            issue: upstreamType == WorkItemDependencyNodeType.Issue ? new FakeIssueRepository() : null,
            dependencies: dependencyRepo);

        var result = await handler.HandleAsync(
            new CreateWorkItemDependencyCommand(
                upstreamType,
                Guid.NewGuid(),
                T,
                Guid.NewGuid(),
                WorkItemDependencyKind.Parallel,
                RequiredState: null,
                Guid.NewGuid()));

        Assert.Equal(upstreamType, result.UpstreamType);
        Assert.Single(dependencyRepo.Dependencies);
    }

    [Theory]
    [InlineData(WorkItemDependencyNodeType.Feature)]
    [InlineData(WorkItemDependencyNodeType.Task)]
    [InlineData(WorkItemDependencyNodeType.Subtask)]
    [InlineData(WorkItemDependencyNodeType.Milestone)]
    [InlineData(WorkItemDependencyNodeType.Issue)]
    public async Task Create_WhenUpstreamTargetDoesNotExist_ThrowsNamingUpstreamAndCreatesNothing(WorkItemDependencyNodeType upstreamType)
    {
        var upstreamId = Guid.NewGuid();
        var dependencyRepo = new InMemoryWorkItemDependencyRepository();
        var handler = CreateHandler(
            feature: upstreamType == WorkItemDependencyNodeType.Feature ? new FakeFeatureRepository(upstreamId) : new FakeFeatureRepository(),
            task: upstreamType == T ? new FakeTaskRepository(upstreamId) : new FakeTaskRepository(),
            subtask: upstreamType == WorkItemDependencyNodeType.Subtask ? new FakeSubtaskRepository(upstreamId) : new FakeSubtaskRepository(),
            milestone: upstreamType == WorkItemDependencyNodeType.Milestone ? new FakeMilestoneRepository(upstreamId) : new FakeMilestoneRepository(),
            issue: upstreamType == WorkItemDependencyNodeType.Issue ? new FakeIssueRepository(upstreamId) : new FakeIssueRepository(),
            dependencies: dependencyRepo);

        var ex = await Assert.ThrowsAsync<WorkItemDependencyTargetNotFoundException>(() =>
            handler.HandleAsync(
                new CreateWorkItemDependencyCommand(
                    upstreamType,
                    upstreamId,
                    T,
                    Guid.NewGuid(),
                    WorkItemDependencyKind.Blocking,
                    RequiredState: null,
                    Guid.NewGuid())));

        Assert.Equal("upstream", ex.Side);
        Assert.Equal(upstreamType, ex.NodeType);
        Assert.Equal(upstreamId, ex.NodeId);
        Assert.Equal($"The upstream {upstreamType} '{upstreamId}' does not exist.", ex.Message);
        Assert.Empty(dependencyRepo.Dependencies);
    }

    // The upstream in this theory is always a Feature, so the missing-id set must
    // be id-scoped (not type-scoped): when downstreamType is also Feature, both
    // ends resolve through the same fake repository, and only the downstream id
    // may be missing.

    [Theory]
    [InlineData(WorkItemDependencyNodeType.Feature)]
    [InlineData(WorkItemDependencyNodeType.Task)]
    [InlineData(WorkItemDependencyNodeType.Subtask)]
    [InlineData(WorkItemDependencyNodeType.Milestone)]
    [InlineData(WorkItemDependencyNodeType.Issue)]
    public async Task Create_WhenDownstreamTargetDoesNotExist_ThrowsNamingDownstreamAndCreatesNothing(WorkItemDependencyNodeType downstreamType)
    {
        var downstreamId = Guid.NewGuid();
        var dependencyRepo = new InMemoryWorkItemDependencyRepository();
        var handler = CreateHandler(
            feature: downstreamType == WorkItemDependencyNodeType.Feature ? new FakeFeatureRepository(downstreamId) : new FakeFeatureRepository(),
            task: downstreamType == T ? new FakeTaskRepository(downstreamId) : new FakeTaskRepository(),
            subtask: downstreamType == WorkItemDependencyNodeType.Subtask ? new FakeSubtaskRepository(downstreamId) : new FakeSubtaskRepository(),
            milestone: downstreamType == WorkItemDependencyNodeType.Milestone ? new FakeMilestoneRepository(downstreamId) : new FakeMilestoneRepository(),
            issue: downstreamType == WorkItemDependencyNodeType.Issue ? new FakeIssueRepository(downstreamId) : new FakeIssueRepository(),
            dependencies: dependencyRepo);

        var ex = await Assert.ThrowsAsync<WorkItemDependencyTargetNotFoundException>(() =>
            handler.HandleAsync(
                new CreateWorkItemDependencyCommand(
                    WorkItemDependencyNodeType.Feature,
                    Guid.NewGuid(),
                    downstreamType,
                    downstreamId,
                    WorkItemDependencyKind.Blocking,
                    RequiredState: null,
                    Guid.NewGuid())));

        Assert.Equal("downstream", ex.Side);
        Assert.Equal(downstreamType, ex.NodeType);
        Assert.Equal(downstreamId, ex.NodeId);
        Assert.Equal($"The downstream {downstreamType} '{downstreamId}' does not exist.", ex.Message);
        Assert.Empty(dependencyRepo.Dependencies);
    }

    [Fact]
    public async Task Create_BlockingEdgeThatWouldCloseCycle_ThrowsNamingCycleAndDoesNotPersist()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var dependencyRepo = new InMemoryWorkItemDependencyRepository();
        await dependencyRepo.AddAsync(Edge(a, b));
        await dependencyRepo.AddAsync(Edge(b, c));
        var handler = CreateHandler(dependencies: dependencyRepo);

        var ex = await Assert.ThrowsAsync<WorkItemDependencyCycleException>(() =>
            handler.HandleAsync(
                new CreateWorkItemDependencyCommand(
                    T, c,
                    T, a,
                    WorkItemDependencyKind.Blocking,
                    RequiredState: null,
                    Guid.NewGuid())));

        // The full cycle path is carried, closing back to the start via the
        // proposed edge, and named in the message.
        Assert.Equal(new[] { (T, a), (T, b), (T, c), (T, a) }, ex.Path);
        Assert.Contains($"Task:{a}", ex.Message);
        Assert.Contains($"Task:{b}", ex.Message);
        Assert.Contains($"Task:{c}", ex.Message);
        Assert.Equal(2, dependencyRepo.Dependencies.Count); // not persisted
    }

    [Fact]
    public async Task Create_ParallelEdgeThatWouldFormACycleIfBlocking_IsAllowedAndPersisted()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var dependencyRepo = new InMemoryWorkItemDependencyRepository();
        await dependencyRepo.AddAsync(Edge(a, b));
        var handler = CreateHandler(dependencies: dependencyRepo);

        var result = await handler.HandleAsync(
            new CreateWorkItemDependencyCommand(
                T, b,
                T, a,
                WorkItemDependencyKind.Parallel,
                RequiredState: null,
                Guid.NewGuid()));

        // The reverse edge is only a cycle if Blocking; a Parallel edge never
        // participates in cycle detection.
        Assert.Equal(WorkItemDependencyKind.Parallel, result.Kind);
        Assert.Equal(2, dependencyRepo.Dependencies.Count);
    }

    [Fact]
    public async Task Create_SelfLoop_ThrowsBeforeAnyTraversal()
    {
        var a = Guid.NewGuid();
        var dependencyRepo = new InMemoryWorkItemDependencyRepository();
        var handler = CreateHandler(dependencies: dependencyRepo);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(
                new CreateWorkItemDependencyCommand(
                    T, a,
                    T, a,
                    WorkItemDependencyKind.Blocking,
                    RequiredState: null,
                    Guid.NewGuid())));

        Assert.Empty(dependencyRepo.Dependencies);
    }

    [Theory]
    [InlineData(WorkItemDependencyKind.Parallel)]
    [InlineData(WorkItemDependencyKind.Informational)]
    public async Task Create_RequiredStateOnNonBlocking_Throws(WorkItemDependencyKind kind)
    {
        var dependencyRepo = new InMemoryWorkItemDependencyRepository();
        var handler = CreateHandler(dependencies: dependencyRepo);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(
                new CreateWorkItemDependencyCommand(
                    T, Guid.NewGuid(),
                    T, Guid.NewGuid(),
                    kind,
                    WorkItemDependencyRequiredState.Merged,
                    Guid.NewGuid())));

        Assert.Empty(dependencyRepo.Dependencies);
    }

    private static CreateWorkItemDependencyHandler CreateHandler(
        IFeatureRepository? feature = null,
        ITaskRepository? task = null,
        ISubtaskRepository? subtask = null,
        IMilestoneRepository? milestone = null,
        IIssueRepository? issue = null,
        IWorkItemDependencyRepository? dependencies = null)
        => new(
            feature ?? new FakeFeatureRepository(),
            task ?? new FakeTaskRepository(),
            subtask ?? new FakeSubtaskRepository(),
            milestone ?? new FakeMilestoneRepository(),
            issue ?? new FakeIssueRepository(),
            dependencies ?? new InMemoryWorkItemDependencyRepository());

    private static WorkItemDependency Edge(
        Guid upstreamId,
        Guid downstreamId,
        WorkItemDependencyKind kind = WorkItemDependencyKind.Blocking)
        => new(
            WorkItemDependencyId.New(),
            T, upstreamId,
            T, downstreamId,
            kind,
            requiredState: null,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

    private sealed class InMemoryWorkItemDependencyRepository : IWorkItemDependencyRepository
    {
        private readonly List<WorkItemDependency> _dependencies = new();

        public IReadOnlyList<WorkItemDependency> Dependencies => _dependencies;

        public Task AddAsync(WorkItemDependency dependency, CancellationToken cancellationToken = default)
        {
            _dependencies.Add(dependency);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<WorkItemDependency>> ListAllBlockingAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<WorkItemDependency>>(
                _dependencies.Where(dependency => dependency.Kind == WorkItemDependencyKind.Blocking).ToList());

        public Task<IReadOnlyList<WorkItemDependency>> ListByNodeAsync(
            WorkItemDependencyNodeType nodeType,
            Guid nodeId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<WorkItemDependency>>(
                _dependencies
                    .Where(dependency =>
                        (dependency.UpstreamType == nodeType && dependency.UpstreamId == nodeId) ||
                        (dependency.DownstreamType == nodeType && dependency.DownstreamId == nodeId))
                    .ToList());
    }

    private sealed class FakeFeatureRepository : IFeatureRepository
    {
        private readonly HashSet<Guid> _missingIds;

        public FakeFeatureRepository(params Guid[] missingIds)
            => _missingIds = new HashSet<Guid>(missingIds);

        public Task AddAsync(Feature domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Feature?> GetAsync(FeatureId id, CancellationToken cancellationToken = default)
            => Task.FromResult<Feature?>(_missingIds.Contains(id.Value)
                ? null
                : new Feature(id, SubprojectId.New(), "Title", "Description", Guid.NewGuid(), DateTimeOffset.UtcNow));

        public Task UpdateAsync(Feature domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<Feature>> ListBySubprojectAsync(
            SubprojectId subprojectId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Feature>>(Array.Empty<Feature>());
    }

    private sealed class FakeTaskRepository : ITaskRepository
    {
        private readonly HashSet<Guid> _missingIds;

        public FakeTaskRepository(params Guid[] missingIds)
            => _missingIds = new HashSet<Guid>(missingIds);

        public System.Threading.Tasks.Task AddAsync(DeveloperTask task, CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.CompletedTask;

        public System.Threading.Tasks.Task<DeveloperTask?> GetAsync(
            TaskId id,
            CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.FromResult<DeveloperTask?>(_missingIds.Contains(id.Value)
                ? null
                : new DeveloperTask(id, FeatureId.New(), "Title", "Description", Guid.NewGuid(), DateTimeOffset.UtcNow));

        public System.Threading.Tasks.Task UpdateAsync(DeveloperTask task, CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.CompletedTask;

        public System.Threading.Tasks.Task<IReadOnlyList<DeveloperTask>> ListByFeatureAsync(
            FeatureId featureId,
            CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.FromResult<IReadOnlyList<DeveloperTask>>(Array.Empty<DeveloperTask>());
    }

    private sealed class FakeSubtaskRepository : ISubtaskRepository
    {
        private readonly HashSet<Guid> _missingIds;

        public FakeSubtaskRepository(params Guid[] missingIds)
            => _missingIds = new HashSet<Guid>(missingIds);

        public Task AddAsync(Subtask domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Subtask?> GetAsync(SubtaskId id, CancellationToken cancellationToken = default)
            => Task.FromResult<Subtask?>(_missingIds.Contains(id.Value)
                ? null
                : new Subtask(id, TaskId.New(), "Title", "Description", Guid.NewGuid(), DateTimeOffset.UtcNow));

        public Task UpdateAsync(Subtask domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<Subtask>> ListByTaskAsync(
            TaskId taskId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Subtask>>(Array.Empty<Subtask>());
    }

    private sealed class FakeMilestoneRepository : IMilestoneRepository
    {
        private readonly HashSet<Guid> _missingIds;

        public FakeMilestoneRepository(params Guid[] missingIds)
            => _missingIds = new HashSet<Guid>(missingIds);

        public Task AddAsync(Milestone domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Milestone?> GetAsync(MilestoneId id, CancellationToken cancellationToken = default)
            => Task.FromResult<Milestone?>(_missingIds.Contains(id.Value)
                ? null
                : new Milestone(id, SubprojectId.New(), "Name", "Description", null, Guid.NewGuid(), DateTimeOffset.UtcNow));

        public Task UpdateAsync(Milestone domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<Milestone>> ListBySubprojectAsync(
            SubprojectId subprojectId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Milestone>>(Array.Empty<Milestone>());
    }

    private sealed class FakeIssueRepository : IIssueRepository
    {
        private readonly HashSet<Guid> _missingIds;

        public FakeIssueRepository(params Guid[] missingIds)
            => _missingIds = new HashSet<Guid>(missingIds);

        public Task AddAsync(Issue domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Issue?> GetAsync(IssueId id, CancellationToken cancellationToken = default)
            => Task.FromResult<Issue?>(_missingIds.Contains(id.Value)
                ? null
                : new Issue(id, "Title", "Description", Guid.NewGuid(), DateTimeOffset.UtcNow));

        public Task UpdateAsync(Issue domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
