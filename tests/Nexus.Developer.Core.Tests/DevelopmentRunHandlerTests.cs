using Nexus.Developer.Application.DevelopmentRuns;
using Nexus.Developer.Application.DevelopmentRuns.Commands.CreateDevelopmentRun;
using Nexus.Developer.Application.DevelopmentRuns.Queries.GetDevelopmentRun;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.DevelopmentRuns;
using Nexus.Developer.Core.Features;
using Nexus.Developer.Core.Issues;
using DeveloperTask = Nexus.Developer.Core.Tasks.Task;
using ITaskRepository = Nexus.Developer.Core.Tasks.ITaskRepository;
using Xunit;

namespace Nexus.Developer.Core.Tests;

public class DevelopmentRunHandlerTests
{
    [Fact]
    public async Task Create_WhenFeatureTargetExists_CreatesNotStartedRun()
    {
        var targetId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();
        var runRepo = new InMemoryDevelopmentRunRepository();
        var handler = CreateHandler(feature: new FakeFeatureRepository(), runs: runRepo);

        var result = await handler.HandleAsync(
            new CreateDevelopmentRunCommand(
                TargetType: DevelopmentRunTargetType.Feature,
                TargetId: targetId,
                CreatedByUserId: createdByUserId));

        var run = Assert.Single(runRepo.Runs);

        // Result shape.
        Assert.Equal(run.Id, result.DevelopmentRunId);
        Assert.Equal(DevelopmentRunTargetType.Feature, result.TargetType);
        Assert.Equal(targetId, result.TargetId);
        Assert.Equal(run.Reference, result.Reference);

        // Run carried the command's fields.
        Assert.Equal(DevelopmentRunTargetType.Feature, run.TargetType);
        Assert.Equal(targetId, run.TargetId);
        Assert.Equal(createdByUserId, run.CreatedByUserId);
        Assert.Equal(DevelopmentRunStatus.NotStarted, run.Status);
    }

    [Theory]
    [InlineData(DevelopmentRunTargetType.Feature)]
    [InlineData(DevelopmentRunTargetType.Task)]
    [InlineData(DevelopmentRunTargetType.Issue)]
    public async Task Create_WithExistingTargetOfAnyAllowedType_CreatesRun(DevelopmentRunTargetType targetType)
    {
        var runRepo = new InMemoryDevelopmentRunRepository();
        var handler = CreateHandler(
            feature: targetType == DevelopmentRunTargetType.Feature ? new FakeFeatureRepository() : null,
            task: targetType == DevelopmentRunTargetType.Task ? new FakeTaskRepository() : null,
            issue: targetType == DevelopmentRunTargetType.Issue ? new FakeIssueRepository() : null,
            runs: runRepo);
        var targetId = Guid.NewGuid();

        var result = await handler.HandleAsync(
            new CreateDevelopmentRunCommand(
                TargetType: targetType,
                TargetId: targetId,
                CreatedByUserId: Guid.NewGuid()));

        Assert.Equal(targetType, result.TargetType);
        Assert.Single(runRepo.Runs);
        Assert.Equal(targetType, runRepo.Runs[0].TargetType);
        Assert.Equal(targetId, runRepo.Runs[0].TargetId);
    }

    [Theory]
    [InlineData(DevelopmentRunTargetType.Feature)]
    [InlineData(DevelopmentRunTargetType.Task)]
    [InlineData(DevelopmentRunTargetType.Issue)]
    public async Task Create_WhenTargetOfAnyAllowedTypeDoesNotExist_ThrowsAndCreatesNothing(DevelopmentRunTargetType targetType)
    {
        var runRepo = new InMemoryDevelopmentRunRepository();
        var handler = CreateHandler(
            feature: targetType == DevelopmentRunTargetType.Feature ? new FakeFeatureRepository(exists: false) : null,
            task: targetType == DevelopmentRunTargetType.Task ? new FakeTaskRepository(exists: false) : null,
            issue: targetType == DevelopmentRunTargetType.Issue ? new FakeIssueRepository(exists: false) : null,
            runs: runRepo);
        var targetId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<DevelopmentRunTargetNotFoundException>(() =>
            handler.HandleAsync(
                new CreateDevelopmentRunCommand(
                    TargetType: targetType,
                    TargetId: targetId,
                    CreatedByUserId: Guid.NewGuid())));

        Assert.Equal(targetType, ex.TargetType);
        Assert.Equal(targetId, ex.TargetId);
        Assert.Equal($"The {targetType} '{targetId}' does not exist.", ex.Message);
        Assert.Empty(runRepo.Runs);
    }

    [Fact]
    public async Task Get_WhenRunExists_ReturnsMappedRun()
    {
        var id = DevelopmentRunId.New();
        var targetId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var runRepo = new InMemoryDevelopmentRunRepository();
        await runRepo.AddAsync(DevelopmentRun.Restore(
            id,
            DevelopmentRunTargetType.Issue,
            targetId,
            DevelopmentRunStatus.InProgress,
            createdByUserId,
            createdAt,
            "RUN-000042",
            planId: null, promptId: null, resultId: null, reportId: null, checkSetId: null, verificationId: null));
        var handler = new GetDevelopmentRunHandler(runRepo);

        var result = await handler.HandleAsync(new GetDevelopmentRunQuery(id));

        Assert.NotNull(result);
        Assert.Equal(id, result!.DevelopmentRunId);
        Assert.Equal(DevelopmentRunTargetType.Issue, result.TargetType);
        Assert.Equal(targetId, result.TargetId);
        Assert.Equal(DevelopmentRunStatus.InProgress, result.Status);
        Assert.Equal(createdByUserId, result.CreatedByUserId);
        Assert.Equal(createdAt, result.CreatedAt);
        Assert.Equal("RUN-000042", result.Reference);
    }

    [Fact]
    public async Task Get_WhenRunDoesNotExist_ReturnsNull()
    {
        var handler = new GetDevelopmentRunHandler(new InMemoryDevelopmentRunRepository());

        var result = await handler.HandleAsync(new GetDevelopmentRunQuery(DevelopmentRunId.New()));

        Assert.Null(result);
    }

    private static CreateDevelopmentRunHandler CreateHandler(
        IFeatureRepository? feature = null,
        ITaskRepository? task = null,
        IIssueRepository? issue = null,
        IDevelopmentRunRepository? runs = null)
        => new(
            feature ?? new FakeFeatureRepository(),
            task ?? new FakeTaskRepository(),
            issue ?? new FakeIssueRepository(),
            runs ?? new InMemoryDevelopmentRunRepository());

    private sealed class InMemoryDevelopmentRunRepository : IDevelopmentRunRepository
    {
        private readonly List<DevelopmentRun> _runs = new();

        public IReadOnlyList<DevelopmentRun> Runs => _runs;

        public Task AddAsync(DevelopmentRun domain, CancellationToken cancellationToken = default)
        {
            _runs.Add(domain);
            return Task.CompletedTask;
        }

        public Task<DevelopmentRun?> GetAsync(DevelopmentRunId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_runs.FirstOrDefault(run => run.Id == id));

        public Task UpdateAsync(DevelopmentRun domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<DevelopmentRun>> ListByTargetAsync(
            DevelopmentRunTargetType targetType,
            Guid targetId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DevelopmentRun>>(
                _runs.Where(run => run.TargetType == targetType && run.TargetId == targetId).ToList());
    }

    private sealed class FakeFeatureRepository : IFeatureRepository
    {
        private readonly bool _exists;

        public FakeFeatureRepository(bool exists = true) => _exists = exists;

        public Task AddAsync(Feature domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Feature?> GetAsync(FeatureId id, CancellationToken cancellationToken = default)
            => Task.FromResult<Feature?>(_exists
                ? new Feature(id, SubprojectId.New(), "Title", "Description", Guid.NewGuid(), DateTimeOffset.UtcNow)
                : null);

        public Task UpdateAsync(Feature domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<Feature>> ListBySubprojectAsync(
            SubprojectId subprojectId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Feature>>(Array.Empty<Feature>());
    }

    private sealed class FakeTaskRepository : ITaskRepository
    {
        private readonly bool _exists;

        public FakeTaskRepository(bool exists = true) => _exists = exists;

        public System.Threading.Tasks.Task AddAsync(DeveloperTask task, CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.CompletedTask;

        public System.Threading.Tasks.Task<DeveloperTask?> GetAsync(
            TaskId id,
            CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.FromResult<DeveloperTask?>(_exists
                ? new DeveloperTask(id, FeatureId.New(), "Title", "Description", Guid.NewGuid(), DateTimeOffset.UtcNow)
                : null);

        public System.Threading.Tasks.Task UpdateAsync(DeveloperTask task, CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.CompletedTask;

        public System.Threading.Tasks.Task<IReadOnlyList<DeveloperTask>> ListByFeatureAsync(
            FeatureId featureId,
            CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.FromResult<IReadOnlyList<DeveloperTask>>(Array.Empty<DeveloperTask>());
    }

    private sealed class FakeIssueRepository : IIssueRepository
    {
        private readonly bool _exists;

        public FakeIssueRepository(bool exists = true) => _exists = exists;

        public Task AddAsync(Issue domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Issue?> GetAsync(IssueId id, CancellationToken cancellationToken = default)
            => Task.FromResult<Issue?>(_exists
                ? new Issue(id, "Title", "Description", Guid.NewGuid(), DateTimeOffset.UtcNow)
                : null);

        public Task UpdateAsync(Issue domain, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
