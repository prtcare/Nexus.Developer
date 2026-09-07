using Microsoft.EntityFrameworkCore;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.DevelopmentRuns;
using Nexus.Developer.Infrastructure.Sql;
using Xunit;

namespace Nexus.Developer.Core.Tests;

// SP1-M03 (Lane B1) "DevelopmentRun Phase-1 expansion": the governed lifecycle
// (Start/Succeed/Fail/Cancel), execution-session fields, the structured result
// summary, terminal-state immutability, and store round-trips.
public class DevelopmentRunLifecycleTests
{
    private static DevelopmentRun NewRun(
        DevelopmentRunStatus? forcedRestoreStatus = null,
        string? workerId = null,
        string? workerType = null,
        DateTimeOffset? startedAt = null,
        DateTimeOffset? completedAt = null,
        string? resultSummary = null)
    {
        var createdAt = new DateTimeOffset(2026, 9, 7, 9, 0, 0, TimeSpan.Zero);
        var id = DevelopmentRunId.New();
        var targetId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        if (forcedRestoreStatus is null)
        {
            return new DevelopmentRun(id, DevelopmentRunTargetType.Feature, targetId, createdBy, createdAt);
        }

        return DevelopmentRun.Restore(
            id,
            DevelopmentRunTargetType.Feature,
            targetId,
            forcedRestoreStatus.Value,
            createdBy,
            createdAt,
            "RUN-000999",
            planId: null,
            promptId: null,
            resultId: null,
            reportId: null,
            checkSetId: null,
            verificationId: null,
            workerId: workerId,
            workerType: workerType,
            startedAt: startedAt,
            completedAt: completedAt,
            resultSummary: resultSummary);
    }

    // --- Start: NotStarted -> InProgress ---------------------------------------

    [Fact]
    public void Start_OnNotStarted_SetsInProgressAndStampsWorkerAndStartedAt()
    {
        var run = NewRun();

        var before = DateTimeOffset.UtcNow;
        run.Start("agent-7", "claude");
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(DevelopmentRunStatus.InProgress, run.Status);
        Assert.Equal("agent-7", run.WorkerId);
        Assert.Equal("claude", run.WorkerType);
        Assert.NotNull(run.StartedAt);
        Assert.InRange(run.StartedAt!.Value, before.AddSeconds(-1), after.AddSeconds(1));
        Assert.Null(run.CompletedAt);
        Assert.Null(run.ResultSummary);
        Assert.Null(run.Result);
    }

    [Theory]
    [InlineData(DevelopmentRunStatus.InProgress)]
    [InlineData(DevelopmentRunStatus.Completed)]
    [InlineData(DevelopmentRunStatus.Failed)]
    [InlineData(DevelopmentRunStatus.Cancelled)]
    public void Start_OnAnyNonNotStartedState_Throws(DevelopmentRunStatus status)
    {
        var run = NewRun(status);

        Assert.Throws<InvalidOperationException>(() => run.Start("agent-7", "claude"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Start_WithBlankWorkerId_Throws(string? workerId)
    {
        var run = NewRun();

        Assert.ThrowsAny<ArgumentException>(() => run.Start(workerId!, "claude"));
        Assert.Equal(DevelopmentRunStatus.NotStarted, run.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Start_WithBlankWorkerType_Throws(string? workerType)
    {
        var run = NewRun();

        Assert.ThrowsAny<ArgumentException>(() => run.Start("agent-7", workerType!));
        Assert.Equal(DevelopmentRunStatus.NotStarted, run.Status);
    }

    // --- Succeed: InProgress -> Completed -------------------------------------

    [Fact]
    public void Succeed_OnInProgress_SetsCompletedAndStampsCompletedAtAndSummary()
    {
        var run = NewRun();
        run.Start("agent-7", "claude");

        var before = DateTimeOffset.UtcNow;
        run.Succeed("  Built the aggregate and its tests.  ");
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(DevelopmentRunStatus.Completed, run.Status);
        Assert.Equal("Built the aggregate and its tests.", run.ResultSummary);
        Assert.NotNull(run.CompletedAt);
        Assert.InRange(run.CompletedAt!.Value, before.AddSeconds(-1), after.AddSeconds(1));
        Assert.NotNull(run.Result);
        Assert.Equal(DevelopmentRunOutcome.Success, run.Result!.Outcome);
        Assert.Equal("Built the aggregate and its tests.", run.Result.Summary);
    }

    [Fact]
    public void Succeed_OnNotStarted_Throws()
    {
        var run = NewRun();

        Assert.Throws<InvalidOperationException>(() => run.Succeed("done"));
    }

    [Fact]
    public void Succeed_WithBlankSummary_ThrowsAndStaysInProgress()
    {
        var run = NewRun();
        run.Start("agent-7", "claude");

        Assert.Throws<ArgumentException>(() => run.Succeed("   "));

        Assert.Equal(DevelopmentRunStatus.InProgress, run.Status);
        Assert.Null(run.ResultSummary);
        Assert.Null(run.CompletedAt);
    }

    // --- Fail: InProgress -> Failed -------------------------------------------

    [Fact]
    public void Fail_OnInProgress_SetsFailedAndRecordsReason()
    {
        var run = NewRun();
        run.Start("agent-7", "claude");

        var before = DateTimeOffset.UtcNow;
        run.Fail("Test flake: the store round-trip assertion timed out.");
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(DevelopmentRunStatus.Failed, run.Status);
        Assert.Equal("Test flake: the store round-trip assertion timed out.", run.ResultSummary);
        Assert.NotNull(run.CompletedAt);
        Assert.InRange(run.CompletedAt!.Value, before.AddSeconds(-1), after.AddSeconds(1));
        Assert.NotNull(run.Result);
        Assert.Equal(DevelopmentRunOutcome.Failed, run.Result!.Outcome);
    }

    [Fact]
    public void Fail_OnInProgress_WithNoReason_StillFailsWithEmptySummary()
    {
        var run = NewRun();
        run.Start("agent-7", "claude");

        run.Fail();

        Assert.Equal(DevelopmentRunStatus.Failed, run.Status);
        Assert.Null(run.ResultSummary);
        Assert.NotNull(run.Result);
        Assert.Equal(string.Empty, run.Result!.Summary);
    }

    [Fact]
    public void Fail_OnNotStarted_Throws()
    {
        var run = NewRun();

        Assert.Throws<InvalidOperationException>(() => run.Fail("boom"));
    }

    // --- Cancel: NotStarted | InProgress -> Cancelled --------------------------

    [Fact]
    public void Cancel_OnInProgress_SetsCancelledAndStampsCompletedAt()
    {
        var run = NewRun();
        run.Start("agent-7", "claude");

        var before = DateTimeOffset.UtcNow;
        run.Cancel("Superseded by a newer change.");
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(DevelopmentRunStatus.Cancelled, run.Status);
        Assert.Equal("Superseded by a newer change.", run.ResultSummary);
        Assert.NotNull(run.CompletedAt);
        Assert.InRange(run.CompletedAt!.Value, before.AddSeconds(-1), after.AddSeconds(1));
        Assert.NotNull(run.Result);
        Assert.Equal(DevelopmentRunOutcome.Cancelled, run.Result!.Outcome);
    }

    [Fact]
    public void Cancel_OnNotStarted_SetsCancelledWithoutStartedAt()
    {
        var run = NewRun();

        run.Cancel();

        Assert.Equal(DevelopmentRunStatus.Cancelled, run.Status);
        Assert.Null(run.StartedAt);
        Assert.NotNull(run.CompletedAt);
    }

    [Fact]
    public void Cancel_WithNoSummary_UsesStandardSummary()
    {
        var run = NewRun();
        run.Start("agent-7", "claude");

        run.Cancel();

        Assert.Equal(DevelopmentRun.DefaultCancellationSummary, run.ResultSummary);
        Assert.Equal(DevelopmentRun.DefaultCancellationSummary, run.Result!.Summary);
    }

    [Theory]
    [InlineData(DevelopmentRunStatus.Completed)]
    [InlineData(DevelopmentRunStatus.Failed)]
    [InlineData(DevelopmentRunStatus.Cancelled)]
    public void Cancel_OnAnyTerminalState_Throws(DevelopmentRunStatus status)
    {
        var run = NewRun(status);

        Assert.Throws<InvalidOperationException>(() => run.Cancel());
    }

    // --- Terminal-state immutability ------------------------------------------

    [Theory]
    [InlineData(DevelopmentRunStatus.Completed)]
    [InlineData(DevelopmentRunStatus.Failed)]
    [InlineData(DevelopmentRunStatus.Cancelled)]
    public void TerminalState_RejectsEveryFurtherTransition(DevelopmentRunStatus status)
    {
        var run = NewRun(status);

        Assert.Throws<InvalidOperationException>(() => run.Start("agent-7", "claude"));
        Assert.Throws<InvalidOperationException>(() => run.Succeed("late success"));
        Assert.Throws<InvalidOperationException>(() => run.Fail("late failure"));
        Assert.Throws<InvalidOperationException>(() => run.Cancel());

        Assert.Equal(status, run.Status);
    }

    // --- Derived result projection --------------------------------------------

    [Theory]
    [InlineData(DevelopmentRunStatus.NotStarted)]
    [InlineData(DevelopmentRunStatus.Planned)]
    [InlineData(DevelopmentRunStatus.InProgress)]
    public void Result_IsNullForNonTerminalStates(DevelopmentRunStatus status)
    {
        var run = NewRun(status);

        Assert.Null(run.Result);
    }

    // --- Restore / store round-trips ------------------------------------------

    [Fact]
    public void Restore_RoundTripsTerminalExecutionAndResultFields()
    {
        var createdAt = new DateTimeOffset(2026, 9, 7, 9, 0, 0, TimeSpan.Zero);
        var startedAt = createdAt.AddMinutes(5);
        var completedAt = createdAt.AddHours(1);
        var id = DevelopmentRunId.New();
        var targetId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        var restored = DevelopmentRun.Restore(
            id,
            DevelopmentRunTargetType.Feature,
            targetId,
            DevelopmentRunStatus.Completed,
            createdBy,
            createdAt,
            "RUN-000999",
            planId: null,
            promptId: null,
            resultId: null,
            reportId: null,
            checkSetId: null,
            verificationId: null,
            workerId: "agent-7",
            workerType: "claude",
            startedAt: startedAt,
            completedAt: completedAt,
            resultSummary: "Delivered.");

        Assert.Equal(DevelopmentRunStatus.Completed, restored.Status);
        Assert.Equal("agent-7", restored.WorkerId);
        Assert.Equal("claude", restored.WorkerType);
        Assert.Equal(startedAt, restored.StartedAt);
        Assert.Equal(completedAt, restored.CompletedAt);
        Assert.Equal("Delivered.", restored.ResultSummary);
        Assert.Equal("RUN-000999", restored.Reference);
        Assert.NotNull(restored.Result);
        Assert.Equal(DevelopmentRunOutcome.Success, restored.Result!.Outcome);
        Assert.Equal("Delivered.", restored.Result.Summary);
    }

    [Fact]
    public async Task Repository_RoundTripsStartedAndCompletedRunThroughContract()
    {
        var repo = new InMemoryDevelopmentRunRepository();
        var id = DevelopmentRunId.New();
        var targetId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 9, 7, 9, 0, 0, TimeSpan.Zero);
        var run = new DevelopmentRun(id, DevelopmentRunTargetType.Feature, targetId, createdBy, createdAt);
        run.Start("agent-7", "claude");
        run.Succeed("Built the aggregate and its tests.");

        await repo.AddAsync(run);
        var loaded = await repo.GetAsync(id);

        Assert.NotNull(loaded);
        Assert.Equal(DevelopmentRunStatus.Completed, loaded!.Status);
        Assert.Equal("agent-7", loaded.WorkerId);
        Assert.Equal("claude", loaded.WorkerType);
        Assert.NotNull(loaded.StartedAt);
        Assert.NotNull(loaded.CompletedAt);
        Assert.Equal("Built the aggregate and its tests.", loaded.ResultSummary);
        Assert.Equal(DevelopmentRunOutcome.Success, loaded.Result!.Outcome);
    }

    [Fact]
    public void EfModel_MapsExecutionSessionAndResultColumns()
    {
        var options = new DbContextOptionsBuilder<NexusDeveloperDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=UnusedModelBuild;Trusted_Connection=True")
            .Options;

        using var context = new NexusDeveloperDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(DevelopmentRun));

        Assert.NotNull(entityType);

        var mappedColumns = entityType!.GetProperties()
            .Select(property => property.Name)
            .ToHashSet();

        Assert.Contains(nameof(DevelopmentRun.WorkerId), mappedColumns);
        Assert.Contains(nameof(DevelopmentRun.WorkerType), mappedColumns);
        Assert.Contains(nameof(DevelopmentRun.StartedAt), mappedColumns);
        Assert.Contains(nameof(DevelopmentRun.CompletedAt), mappedColumns);
        Assert.Contains(nameof(DevelopmentRun.ResultSummary), mappedColumns);
        // The derived result projection is intentionally not a column (see
        // DevelopmentRunResult remarks) -- it must never appear in the store model.
        Assert.DoesNotContain(nameof(DevelopmentRun.Result), mappedColumns);
    }

    private sealed class InMemoryDevelopmentRunRepository : IDevelopmentRunRepository
    {
        private readonly List<DevelopmentRun> _runs = new();

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
}
