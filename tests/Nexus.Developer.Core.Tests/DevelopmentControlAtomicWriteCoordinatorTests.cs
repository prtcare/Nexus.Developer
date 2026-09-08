using Nexus.Developer.Core.DevelopmentControl;
using Xunit;

namespace Nexus.Developer.Core.Tests;

// WI-07-0.2.4 stabilization (SP1-M00): the DevelopmentControlAtomicWriteCoordinator -- the
// default Core coordinator implementing the governed atomic-write sequence (acquire bounded
// writer lock -> verify expected RowVersion against authoritative state -> run the work unit
// inside the critical section -> controlled AtomicWriteResult). A small in-memory store
// double supplies the authoritative state and counts how many mutations reached it, so these
// tests prove the coordinator's control flow: stale/unknown verifications abort BEFORE the
// work unit runs, a held lock surfaces as LockTimeout, and a runner-capable store is routed
// to its single-save work-unit entry point.
public class DevelopmentControlAtomicWriteCoordinatorTests
{
    private static DevelopmentControlMutexIdentity NewIdentity() =>
        // Slash-free token used verbatim (an identity containing '/' or '\' is path-normalized).
        DevelopmentControlMutexIdentity.FromStoreIdentity("test-store-coord-" + Guid.NewGuid().ToString("N"));

    private static Node Node(string id, int rowVersion) => new(
        new NodeId(id), new NodeId("M-07-2.1"), NodeType.WorkItem, "01.001", "03 > " + id,
        "03", "P1", "Node " + id, null, Array.Empty<NodeId>(), false,
        Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(),
        Array.Empty<string>(), null, null, Status.InProgress, false, null, null, null,
        "Durai", null, null, rowVersion, false, "test", null,
        DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    private static MutationEnvelope Envelope(int? expectedRowVersion = null) => new(
        expectedRowVersion,
        new ActorRef(ActorType.Agent, "test-agent", "Test Agent"),
        "Nexus.Developer.Core.Tests",
        null, "session-1", null, "CHG-0000-0000-000", null, null, "test reason");

    private static readonly Node T01 = Node("T-01", 3);

    private static AtomicWriteRequest<Node> Request(
        DevelopmentControlMutexIdentity identity,
        Node? updated = null,
        int? expectedRowVersion = null,
        string? verifyNodeId = null) => new(
        Identity: identity,
        LockTimeout: TimeSpan.FromSeconds(5),
        Envelope: Envelope(expectedRowVersion),
        VerifyEntityNodeId: verifyNodeId,
        WorkUnit: store => store.UpdateNodeAsync(updated ?? T01, Envelope(expectedRowVersion)));

    [Fact]
    public async Task ExecuteAsync_WithAMatchingExpectedRowVersion_RunsTheWorkUnitAndSucceeds()
    {
        var store = new PlainStore();
        store.Current[T01.NodeId.Value] = T01;
        var coordinator = new DevelopmentControlAtomicWriteCoordinator(store, new NamedDevelopmentControlWriteLockFactory());

        var result = await coordinator.ExecuteAsync(Request(NewIdentity(), expectedRowVersion: 3, verifyNodeId: "T-01"));

        Assert.Equal(DevelopmentControlConcurrencyOutcome.Success, result.Outcome);
        Assert.True(result.Success);
        Assert.Equal(1, store.MutatingCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WithAStaleExpectedRowVersion_ReturnsConcurrencyConflictWithoutRunningTheWorkUnit()
    {
        var store = new PlainStore();
        store.Current[T01.NodeId.Value] = T01; // current RowVersion 3
        var coordinator = new DevelopmentControlAtomicWriteCoordinator(store, new NamedDevelopmentControlWriteLockFactory());

        var result = await coordinator.ExecuteAsync(Request(NewIdentity(), expectedRowVersion: 2, verifyNodeId: "T-01"));

        Assert.Equal(DevelopmentControlConcurrencyOutcome.ConcurrencyConflict, result.Outcome);
        Assert.False(result.Success);
        Assert.Equal(3, ((Node)result.ConflictDetails!).RowVersion);
        Assert.Equal(0, store.MutatingCalls); // aborted before the work unit ran
    }

    [Fact]
    public async Task ExecuteAsync_ForAnUnknownNode_ReturnsNotFoundWithoutRunningTheWorkUnit()
    {
        var store = new PlainStore();
        var coordinator = new DevelopmentControlAtomicWriteCoordinator(store, new NamedDevelopmentControlWriteLockFactory());

        var result = await coordinator.ExecuteAsync(Request(NewIdentity(), expectedRowVersion: 1, verifyNodeId: "T-01"));

        Assert.Equal(DevelopmentControlConcurrencyOutcome.NotFound, result.Outcome);
        Assert.False(result.Success);
        Assert.Contains("No current version of node 'T-01'", Assert.Single(result.ValidationErrors));
        Assert.Equal(0, store.MutatingCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WithTheLockHeldElsewhere_ReturnsLockTimeout()
    {
        var identity = NewIdentity();
        var store = new PlainStore();
        store.Current[T01.NodeId.Value] = T01;
        var coordinator = new DevelopmentControlAtomicWriteCoordinator(store, new NamedDevelopmentControlWriteLockFactory());

        var holder = new LockHolder(identity);
        try
        {
            Assert.True(holder.Acquired.Wait(TimeSpan.FromSeconds(10)), "holder never acquired the lock");

            var request = new AtomicWriteRequest<Node>(
                Identity: identity,
                LockTimeout: TimeSpan.FromMilliseconds(250),
                Envelope: null,
                VerifyEntityNodeId: null,
                WorkUnit: s => Task.FromResult(new MutationResult<Node>(true, T01, false, null, Array.Empty<string>(), null)));

            var result = await coordinator.ExecuteAsync(request);

            Assert.Equal(DevelopmentControlConcurrencyOutcome.LockTimeout, result.Outcome);
            Assert.False(result.Success);
            Assert.Equal(0, store.MutatingCalls);
        }
        finally
        {
            holder.Release();
        }
    }

    [Fact]
    public async Task ExecuteAsync_RoutesToTheRunnerEntryPoint_WhenTheStoreSupportsIt()
    {
        var store = new RunnerStore();
        var coordinator = new DevelopmentControlAtomicWriteCoordinator(store, new NamedDevelopmentControlWriteLockFactory());

        // No verification requested, so the generic verify is skipped and -- because the store
        // is runner-capable -- the unit must be executed via its single-save work-unit entry.
        var result = await coordinator.ExecuteAsync(new AtomicWriteRequest<Node>(
            Identity: NewIdentity(),
            LockTimeout: TimeSpan.FromSeconds(5),
            Envelope: null,
            VerifyEntityNodeId: null,
            WorkUnit: s => { store.WorkUnitInvocations++; return Task.FromResult(new MutationResult<Node>(true, T01, false, null, Array.Empty<string>(), null)); }));

        Assert.Equal(DevelopmentControlConcurrencyOutcome.Success, result.Outcome);
        Assert.Equal(1, store.RunnerInvocations);
        Assert.Equal(0, store.WorkUnitInvocations); // the coordinator delegated, not run the unit itself
    }

    [Fact]
    public void Kind_ReflectsTheLockFactoryKind()
    {
        var coordinator = new DevelopmentControlAtomicWriteCoordinator(new PlainStore(), new NamedDevelopmentControlWriteLockFactory());

        Assert.Equal("named-mutex", coordinator.Kind);
    }

    // ------------------------------------------------------------------ doubles

    private class PlainStore : IDevelopmentControlStore
    {
        public Dictionary<string, Node> Current { get; } = new(StringComparer.Ordinal);
        public int MutatingCalls;

        public Task<ControlState?> GetControlStateAsync(CancellationToken ct = default) => Task.FromResult<ControlState?>(null);
        public Task<Node?> GetNodeAsync(NodeId nodeId, CancellationToken ct = default)
            => Task.FromResult(Current.TryGetValue(nodeId.Value, out var node) ? node : null);
        public Task<IReadOnlyList<Node>> GetSubtreeAsync(NodeId rootNodeId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Node>>(Array.Empty<Node>());
        public Task<IReadOnlyList<Node>> SearchNodesAsync(NodeSearchCriteria criteria, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Node>>(Array.Empty<Node>());
        public Task<PreflightResult> RunPreflightAsync(PreflightDeclaration declaration, CancellationToken ct = default)
            => Task.FromResult(new PreflightResult(PreflightVerdict.Clear, null, Array.Empty<string>()));
        public Task<IReadOnlyList<ActiveChange>> GetActiveChangesAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ActiveChange>>(Array.Empty<ActiveChange>());
        public Task<Node?> GetNextExecutableWorkItemAsync(CancellationToken ct = default) => Task.FromResult<Node?>(null);
        public Task<IReadOnlyList<ActivityLogEntry>> GetActivityLogAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ActivityLogEntry>>(Array.Empty<ActivityLogEntry>());
        public Task<ValidationResult> ValidateControlStoreAsync(CancellationToken ct = default)
            => Task.FromResult(new ValidationResult(true, Array.Empty<string>()));

        private MutationResult<T> Hit<T>(T value) where T : class
        {
            Interlocked.Increment(ref MutatingCalls);
            return new MutationResult<T>(true, value, false, null, Array.Empty<string>(), null);
        }

        public Task<MutationResult<Node>> CreateNodeAsync(Node node, MutationEnvelope envelope, CancellationToken ct = default) => Task.FromResult(Hit(node));
        public Task<MutationResult<Node>> UpdateNodeAsync(Node updatedNode, MutationEnvelope envelope, CancellationToken ct = default) => Task.FromResult(Hit(updatedNode));
        public Task<MutationResult<Node>> ReparentNodeAsync(NodeId nodeId, NodeId? newParentId, MutationEnvelope envelope, CancellationToken ct = default) => Task.FromResult(Hit(Current[nodeId.Value]));
        public Task<MutationResult<Node>> RetireNodeAsync(NodeId nodeId, MutationEnvelope envelope, CancellationToken ct = default) => Task.FromResult(Hit(Current[nodeId.Value]));
        public Task<MutationResult<Node>> AddDependencyAsync(NodeId nodeId, NodeId dependencyNodeId, MutationEnvelope envelope, CancellationToken ct = default) => Task.FromResult(Hit(Current[nodeId.Value]));
        public Task<MutationResult<Node>> RemoveDependencyAsync(NodeId nodeId, NodeId dependencyNodeId, MutationEnvelope envelope, CancellationToken ct = default) => Task.FromResult(Hit(Current[nodeId.Value]));
        public Task<MutationResult<Node>> ReserveWorkItemAsync(NodeId nodeId, ActorRef worker, string? branch, string? worktree, MutationEnvelope envelope, CancellationToken ct = default) => Task.FromResult(Hit(Current[nodeId.Value]));
        public Task<MutationResult<ActivityLogEntry>> StartActivityAsync(NodeId nodeId, ActorRef worker, string operation, MutationEnvelope envelope, CancellationToken ct = default) => Task.FromResult(Hit(FakeActivity()));
        public Task<MutationResult<ActivityLogEntry>> RecordHeartbeatAsync(string activityId, MutationEnvelope envelope, CancellationToken ct = default) => Task.FromResult(Hit(FakeActivity()));
        public Task<MutationResult<ActivityLogEntry>> CompleteActivityAsync(string activityId, string result, string? evidence, MutationEnvelope envelope, CancellationToken ct = default) => Task.FromResult(Hit(FakeActivity()));
        public Task<MutationResult<ActivityLogEntry>> FailActivityAsync(string activityId, string errorCode, string errorMessage, MutationEnvelope envelope, CancellationToken ct = default) => Task.FromResult(Hit(FakeActivity()));
        public Task<MutationResult<Node>> ReleaseReservationAsync(NodeId nodeId, MutationEnvelope envelope, CancellationToken ct = default) => Task.FromResult(Hit(Current[nodeId.Value]));
        public Task<MutationResult<Node>> CompleteWorkItemAsync(NodeId nodeId, string resultOrEvidence, MutationEnvelope envelope, CancellationToken ct = default) => Task.FromResult(Hit(Current[nodeId.Value]));

        private static ActivityLogEntry FakeActivity() => new(
            "ACT-FAKE-001", DateTimeOffset.UtcNow, ActorType.Agent, "id", "name", "source",
            null, null, null, null, null, "op", "Node", "T-01", null, null, null, null,
            null, null, null, null, null, null, null, Array.Empty<string>(), null, null,
            null, null, null, null, null, DateTimeOffset.UtcNow);
    }

    // A store that also claims the atomic single-save runner capability, so the coordinator's
    // routing branch can be observed in isolation.
    private sealed class RunnerStore : PlainStore, IDevelopmentControlAtomicWorkUnitRunner
    {
        public int RunnerInvocations;
        public int WorkUnitInvocations;

        public Task<AtomicWriteResult<T>> ExecuteAtomicWorkUnitAsync<T>(
            Func<IDevelopmentControlStore, Task<MutationResult<T>>> workUnit,
            MutationEnvelope? envelope,
            string? verifyEntityNodeId,
            CancellationToken cancellationToken = default) where T : class
        {
            RunnerInvocations++;
            return Task.FromResult(new AtomicWriteResult<T>(
                DevelopmentControlConcurrencyOutcome.Success, true, default, null,
                Array.Empty<string>(), null, null));
        }
    }

    // Holds the supplied writer lock on a dedicated background thread (named mutexes are
    // thread-affine) until Release() is called, so a test can observe a second acquire timing
    // out against a genuinely-held kernel object.
    private sealed class LockHolder
    {
        private readonly ManualResetEventSlim _acquired = new();
        private readonly ManualResetEventSlim _release = new();
        private readonly Thread _thread;
        private IDevelopmentControlWriteLock? _held;

        public LockHolder(DevelopmentControlMutexIdentity identity)
        {
            _thread = new Thread(() =>
            {
                var attempt = NamedDevelopmentControlMutex.TryAcquire(identity, TimeSpan.FromSeconds(10));
                _held = attempt.Lock;
                _acquired.Set();
                _release.Wait(TimeSpan.FromSeconds(30));
                _held?.Dispose();
            }) { IsBackground = true };
            _thread.Start();
        }

        public ManualResetEventSlim Acquired => _acquired;

        public void Release()
        {
            _release.Set();
            Assert.True(_thread.Join(TimeSpan.FromSeconds(10)), "holder thread did not exit");
        }
    }
}
