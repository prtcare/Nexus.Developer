using Nexus.Developer.Application.DevelopmentRuns;
using Nexus.Developer.Application.DevelopmentRuns.Commands.CancelDevelopmentRun;
using Nexus.Developer.Application.DevelopmentRuns.Commands.FailDevelopmentRun;
using Nexus.Developer.Application.DevelopmentRuns.Commands.StartDevelopmentRun;
using Nexus.Developer.Application.DevelopmentRuns.Commands.SucceedDevelopmentRun;
using Nexus.Developer.Application.DevelopmentRuns.Queries.GetDevelopmentRun;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.DevelopmentRuns;
using Xunit;

namespace Nexus.Developer.Core.Tests;

// SP1-M05 (Lane A): the DevelopmentRun lifecycle Application surface over the SP1-M03 Core
// aggregate. Each transition is a governed command handler that loads the run, applies the
// transition (which records only what the caller supplies), persists, and returns the
// resulting lifecycle read model. An unknown run maps to DevelopmentRunNotFoundException
// (endpoint 404) and an illegal transition for the run's current status maps to
// DevelopmentRunStateException (endpoint 409) -- never an unhandled 500.
public class DevelopmentRunLifecycleHandlerTests
{
    [Fact]
    public async Task Start_WithWorker_TransitionsToInProgressAndRecordsTheSession()
    {
        var run = Run(status: DevelopmentRunStatus.NotStarted);
        var repo = new InMemoryDevelopmentRunRepository(run);
        var handler = new StartDevelopmentRunHandler(repo);

        var result = await handler.HandleAsync(
            new StartDevelopmentRunCommand(run.Id, WorkerId: "claude", WorkerType: "coding-agent"));

        Assert.Equal(DevelopmentRunStatus.InProgress, result.Status);
        Assert.Equal("claude", result.WorkerId);
        Assert.Equal("coding-agent", result.WorkerType);
        Assert.NotNull(result.StartedAt);
        Assert.Null(result.CompletedAt);
        Assert.Null(result.ResultSummary);

        var persisted = repo.Single();
        Assert.Equal(DevelopmentRunStatus.InProgress, persisted.Status);
        Assert.Equal("claude", persisted.WorkerId);
        Assert.Equal("coding-agent", persisted.WorkerType);
        Assert.NotNull(persisted.StartedAt);
    }

    [Fact]
    public async Task Start_WhenTheRunIsAlreadyInProgress_ThrowsStateException()
    {
        var run = Run(status: DevelopmentRunStatus.InProgress, workerId: "claude", workerType: "coding-agent");
        var repo = new InMemoryDevelopmentRunRepository(run);
        var handler = new StartDevelopmentRunHandler(repo);

        var ex = await Assert.ThrowsAsync<DevelopmentRunStateException>(() =>
            handler.HandleAsync(new StartDevelopmentRunCommand(run.Id, "codex", "coding-agent")));

        Assert.Contains("cannot be started", ex.Message);
    }

    [Fact]
    public async Task Start_WhenTheRunDoesNotExist_ThrowsNotFoundException()
    {
        var id = DevelopmentRunId.New();
        var handler = new StartDevelopmentRunHandler(new InMemoryDevelopmentRunRepository());

        var ex = await Assert.ThrowsAsync<DevelopmentRunNotFoundException>(() =>
            handler.HandleAsync(new StartDevelopmentRunCommand(id, "claude", "coding-agent")));

        Assert.Equal(id, ex.DevelopmentRunId);
    }

    [Fact]
    public async Task Succeed_AfterStart_CompletesWithTheCallerSuppliedSummary()
    {
        var run = Run(status: DevelopmentRunStatus.NotStarted);
        var repo = new InMemoryDevelopmentRunRepository(run);
        var start = new StartDevelopmentRunHandler(repo);
        await start.HandleAsync(new StartDevelopmentRunCommand(run.Id, "claude", "coding-agent"));
        var handler = new SucceedDevelopmentRunHandler(repo);

        var result = await handler.HandleAsync(
            new SucceedDevelopmentRunCommand(run.Id, "Lane A wired and green; 327+ tests pass."));

        Assert.Equal(DevelopmentRunStatus.Completed, result.Status);
        Assert.Equal("Lane A wired and green; 327+ tests pass.", result.ResultSummary);
        Assert.NotNull(result.StartedAt);
        Assert.NotNull(result.CompletedAt);
        Assert.True(result.CompletedAt >= result.StartedAt);

        var persisted = repo.Single();
        Assert.Equal(DevelopmentRunStatus.Completed, persisted.Status);
        Assert.Equal("Lane A wired and green; 327+ tests pass.", persisted.ResultSummary);
    }

    [Fact]
    public async Task Succeed_WhenTheRunIsNotInProgress_ThrowsStateException()
    {
        var run = Run(status: DevelopmentRunStatus.Completed, resultSummary: "already done");
        var handler = new SucceedDevelopmentRunHandler(new InMemoryDevelopmentRunRepository(run));

        var ex = await Assert.ThrowsAsync<DevelopmentRunStateException>(() =>
            handler.HandleAsync(new SucceedDevelopmentRunCommand(run.Id, "cannot happen")));

        Assert.Contains("cannot be completed", ex.Message);
    }

    [Fact]
    public async Task Cancel_OnANotStartedRun_CancelsWithTheStandardSummary()
    {
        var run = Run(status: DevelopmentRunStatus.NotStarted);
        var handler = new CancelDevelopmentRunHandler(new InMemoryDevelopmentRunRepository(run));

        var result = await handler.HandleAsync(new CancelDevelopmentRunCommand(run.Id));

        Assert.Equal(DevelopmentRunStatus.Cancelled, result.Status);
        Assert.Equal(DevelopmentRun.DefaultCancellationSummary, result.ResultSummary);
        Assert.NotNull(result.CompletedAt);
    }

    [Fact]
    public async Task Cancel_WithACustomSummary_RecordsTheCallersReason()
    {
        var run = Run(status: DevelopmentRunStatus.InProgress, workerId: "claude", workerType: "coding-agent");
        var handler = new CancelDevelopmentRunHandler(new InMemoryDevelopmentRunRepository(run));

        var result = await handler.HandleAsync(new CancelDevelopmentRunCommand(run.Id, "Superseded by SP1-M06."));

        Assert.Equal(DevelopmentRunStatus.Cancelled, result.Status);
        Assert.Equal("Superseded by SP1-M06.", result.ResultSummary);
    }

    [Fact]
    public async Task Fail_AfterStart_TransitionsToFailedWithTheCallersReason()
    {
        var run = Run(status: DevelopmentRunStatus.NotStarted);
        var repo = new InMemoryDevelopmentRunRepository(run);
        await new StartDevelopmentRunHandler(repo).HandleAsync(
            new StartDevelopmentRunCommand(run.Id, "claude", "coding-agent"));
        var handler = new FailDevelopmentRunHandler(repo);

        var result = await handler.HandleAsync(new FailDevelopmentRunCommand(run.Id, "Preflight found an overlap."));

        Assert.Equal(DevelopmentRunStatus.Failed, result.Status);
        Assert.Equal("Preflight found an overlap.", result.ResultSummary);
        Assert.NotNull(result.CompletedAt);
    }

    [Fact]
    public async Task Fail_WhenTheRunIsNotInProgress_ThrowsStateException()
    {
        var run = Run(status: DevelopmentRunStatus.NotStarted);
        var handler = new FailDevelopmentRunHandler(new InMemoryDevelopmentRunRepository(run));

        var ex = await Assert.ThrowsAsync<DevelopmentRunStateException>(() =>
            handler.HandleAsync(new FailDevelopmentRunCommand(run.Id, "nope")));

        Assert.Contains("cannot be failed", ex.Message);
    }

    [Fact]
    public async Task Get_MapsTheExecutionSessionFieldsOntoTheReadModel()
    {
        var id = DevelopmentRunId.New();
        var startedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var run = DevelopmentRun.Restore(
            id,
            DevelopmentRunTargetType.Feature,
            targetId: Guid.NewGuid(),
            DevelopmentRunStatus.InProgress,
            createdByUserId: Guid.NewGuid(),
            createdAt: startedAt.AddHours(-1),
            reference: "RUN-000042",
            planId: null, promptId: null, resultId: null, reportId: null, checkSetId: null, verificationId: null,
            workerId: "claude",
            workerType: "coding-agent",
            startedAt: startedAt);
        var handler = new GetDevelopmentRunHandler(new InMemoryDevelopmentRunRepository(run));

        var result = await handler.HandleAsync(new GetDevelopmentRunQuery(id));

        Assert.NotNull(result);
        Assert.Equal(DevelopmentRunStatus.InProgress, result!.Status);
        Assert.Equal("claude", result.WorkerId);
        Assert.Equal("coding-agent", result.WorkerType);
        Assert.Equal(startedAt, result.StartedAt);
        Assert.Null(result.CompletedAt);
        Assert.Null(result.ResultSummary);
    }

    // ------------------------------------------------------------------ helpers

    private static DevelopmentRun Run(
        DevelopmentRunStatus status,
        string? workerId = null,
        string? workerType = null,
        string? resultSummary = null,
        DateTimeOffset? startedAt = null,
        DateTimeOffset? completedAt = null) => DevelopmentRun.Restore(
        DevelopmentRunId.New(),
        DevelopmentRunTargetType.Feature,
        targetId: Guid.NewGuid(),
        status,
        createdByUserId: Guid.NewGuid(),
        createdAt: DateTimeOffset.UtcNow.AddHours(-1),
        reference: "RUN-000042",
        planId: null, promptId: null, resultId: null, reportId: null, checkSetId: null, verificationId: null,
        workerId: workerId,
        workerType: workerType,
        startedAt: startedAt,
        completedAt: completedAt,
        resultSummary: resultSummary);

    private sealed class InMemoryDevelopmentRunRepository : IDevelopmentRunRepository
    {
        private readonly List<DevelopmentRun> _runs = new();

        public InMemoryDevelopmentRunRepository(params DevelopmentRun[] runs) => _runs.AddRange(runs);

        public IReadOnlyList<DevelopmentRun> Runs => _runs;

        public DevelopmentRun Single() => Assert.Single(_runs);

        public Task AddAsync(DevelopmentRun domain, CancellationToken cancellationToken = default)
        {
            _runs.Add(domain);
            return Task.CompletedTask;
        }

        public Task<DevelopmentRun?> GetAsync(DevelopmentRunId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_runs.FirstOrDefault(run => run.Id == id));

        public Task UpdateAsync(DevelopmentRun domain, CancellationToken cancellationToken = default)
        {
            var index = _runs.FindIndex(run => run.Id == domain.Id);
            if (index >= 0) _runs[index] = domain;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<DevelopmentRun>> ListByTargetAsync(
            DevelopmentRunTargetType targetType,
            Guid targetId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DevelopmentRun>>(
                _runs.Where(run => run.TargetType == targetType && run.TargetId == targetId).ToList());
    }
}
