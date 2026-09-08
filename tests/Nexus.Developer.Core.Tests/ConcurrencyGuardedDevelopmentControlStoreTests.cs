using Nexus.Developer.Core.DevelopmentControl;
using Xunit;

namespace Nexus.Developer.Core.Tests;

// WI-07-0.2.4 stabilization (SP1-M00): the ConcurrencyGuardedDevelopmentControlStore
// decorator. It must serialize guarded writers behind a named cross-process lock, re-verify
// the optimistic RowVersion against authoritative persisted state while the lock is held
// (so a stale writer never reaches the inner store), surface a bounded lock timeout as a
// controlled failure, and leave reads untouched. The inner store is a small in-memory fake,
// so these tests exercise the guard's own control flow -- not the Excel adapter.
public class ConcurrencyGuardedDevelopmentControlStoreTests
{
    private static DevelopmentControlMutexIdentity NewIdentity() =>
        // Slash-free token used verbatim -- an identity containing '/' or '\' would be
        // treated as a filesystem path and normalized by DevelopmentControlMutexIdentity.
        DevelopmentControlMutexIdentity.FromStoreIdentity("test-store-guarded-" + Guid.NewGuid().ToString("N"));

    private static Node Node(string id, int rowVersion) => new(
        new NodeId(id), new NodeId("M-07-2.1"), NodeType.WorkItem, "01.001", "03 > " + id,
        "03", "P1", "Node " + id, null, Array.Empty<NodeId>(), false,
        Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(),
        Array.Empty<string>(), null, null, Status.InProgress, false, null, null, null,
        "Durai", null, null, rowVersion, false, "test", null,
        DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    private static MutationEnvelope Envelope(int? expectedRowVersion) => new(
        expectedRowVersion,
        new ActorRef(ActorType.Agent, "test-agent", "Test Agent"),
        "Nexus.Developer.Core.Tests",
        null, "session-1", null, "CHG-0000-0000-000", null, null, "test reason");

    private static readonly Node T01 = Node("T-01", 3);
    private static readonly Node T02 = Node("T-02", 1);

    [Fact]
    public async Task Reads_PassThroughToTheInnerStore()
    {
        var fake = new FakeDevelopmentControlStore();
        fake.Current[T01.NodeId.Value] = T01;
        var guard = new ConcurrencyGuardedDevelopmentControlStore(
            fake, new NamedDevelopmentControlWriteLockFactory(), NewIdentity());

        var node = await guard.GetNodeAsync(T01.NodeId);

        Assert.NotNull(node);
        Assert.Equal(T01.NodeId, node!.NodeId);
        Assert.Equal(3, node.RowVersion);
    }

    [Fact]
    public async Task Update_WithAMatchingExpectedRowVersion_ReachesTheInnerStore()
    {
        var fake = new FakeDevelopmentControlStore();
        fake.Current[T01.NodeId.Value] = T01;
        var guard = new ConcurrencyGuardedDevelopmentControlStore(
            fake, new NamedDevelopmentControlWriteLockFactory(), NewIdentity());

        var result = await guard.UpdateNodeAsync(
            T01 with { Status = Status.Completed }, Envelope(expectedRowVersion: 3));

        Assert.True(result.Success);
        Assert.Equal(1, fake.MutatingCalls);
    }

    [Fact]
    public async Task Update_WithAStaleExpectedRowVersion_ConflictsBeforeReachingTheInnerStore()
    {
        var fake = new FakeDevelopmentControlStore();
        fake.Current[T01.NodeId.Value] = T01; // current RowVersion is 3
        var guard = new ConcurrencyGuardedDevelopmentControlStore(
            fake, new NamedDevelopmentControlWriteLockFactory(), NewIdentity());

        var result = await guard.UpdateNodeAsync(
            T01 with { Status = Status.Completed }, Envelope(expectedRowVersion: 2));

        Assert.False(result.Success);
        Assert.True(result.Conflict);
        Assert.Equal(3, ((Node)result.ConflictDetails!).RowVersion);
        Assert.Equal(0, fake.MutatingCalls); // the stale write never reached the inner store
    }

    [Fact]
    public async Task Update_OfAnUnknownNode_ReportsNotFoundWithoutReachingTheInnerStore()
    {
        var fake = new FakeDevelopmentControlStore();
        var guard = new ConcurrencyGuardedDevelopmentControlStore(
            fake, new NamedDevelopmentControlWriteLockFactory(), NewIdentity());

        var result = await guard.UpdateNodeAsync(T01, Envelope(expectedRowVersion: 1));

        Assert.False(result.Success);
        Assert.False(result.Conflict);
        Assert.Contains("No current version of node 'T-01'", Assert.Single(result.ValidationErrors));
        Assert.Equal(0, fake.MutatingCalls);
    }

    [Fact]
    public async Task GuardedWrite_WithTheLockHeldElsewhere_ReturnsALockTimeoutResult()
    {
        var identity = NewIdentity();
        var fake = new FakeDevelopmentControlStore();
        fake.BlockNextWrite = true;
        fake.Current[T01.NodeId.Value] = T01;
        fake.Current[T02.NodeId.Value] = T02;
        var guard = new ConcurrencyGuardedDevelopmentControlStore(
            fake, new NamedDevelopmentControlWriteLockFactory(), identity, TimeSpan.FromMilliseconds(250));

        MutationResult<Node>? secondResult = null;
        Exception? threadError = null;

        var holder = new Thread(() =>
        {
            try
            {
                // The holder thread acquires the lock and blocks INSIDE the inner write, so the
                // main thread's guarded write must time out.
                guard.UpdateNodeAsync(T01 with { Status = Status.Blocked }, Envelope(expectedRowVersion: 3))
                    .GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                threadError = ex;
            }
        }) { IsBackground = true };

        holder.Start();
        Assert.True(fake.WriteEntered!.Wait(TimeSpan.FromSeconds(10)), "holder never entered the guarded write");

        try
        {
            secondResult = await guard.UpdateNodeAsync(T02 with { Status = Status.Blocked }, Envelope(expectedRowVersion: 1));
        }
        finally
        {
            fake.ReleaseWrite!.Set();
        }

        Assert.True(holder.Join(TimeSpan.FromSeconds(10)), "holder thread did not exit");
        Assert.Null(threadError);
        Assert.NotNull(secondResult);
        Assert.False(secondResult!.Success);
        Assert.Contains("LOCK_TIMEOUT", Assert.Single(secondResult.ValidationErrors));
    }

    [Fact]
    public async Task ExecuteAtomicWriteAsync_Success_ReturnsAnAtomicWriteResult()
    {
        var fake = new FakeDevelopmentControlStore();
        fake.Current[T01.NodeId.Value] = T01;
        var guard = new ConcurrencyGuardedDevelopmentControlStore(
            fake, new NamedDevelopmentControlWriteLockFactory(), NewIdentity());

        var result = await guard.ExecuteAtomicWriteAsync(new AtomicWriteRequest<Node>(
            Identity: NewIdentity(), // the guarded store locks its OWN identity, not the request's
            LockTimeout: TimeSpan.FromSeconds(5),
            Envelope: Envelope(expectedRowVersion: 3),
            VerifyEntityNodeId: "T-01",
            WorkUnit: store => store.UpdateNodeAsync(T01 with { Status = Status.Completed }, Envelope(expectedRowVersion: 3))));

        Assert.Equal(DevelopmentControlConcurrencyOutcome.Success, result.Outcome);
        Assert.True(result.Success);
        Assert.Equal(1, fake.MutatingCalls);
    }

    [Fact]
    public async Task ExecuteAtomicWriteAsync_WithTheLockHeldElsewhere_ReturnsLockTimeout()
    {
        var identity = NewIdentity();
        var fake = new FakeDevelopmentControlStore();
        fake.BlockNextWrite = true;
        fake.Current[T01.NodeId.Value] = T01;
        fake.Current[T02.NodeId.Value] = T02;
        var guard = new ConcurrencyGuardedDevelopmentControlStore(
            fake, new NamedDevelopmentControlWriteLockFactory(), identity, TimeSpan.FromMilliseconds(250));

        AtomicWriteResult<Node>? outcome = null;

        var holder = new Thread(() =>
        {
            guard.UpdateNodeAsync(T01 with { Status = Status.Blocked }, Envelope(expectedRowVersion: 3))
                .GetAwaiter().GetResult();
        }) { IsBackground = true };

        holder.Start();
        Assert.True(fake.WriteEntered!.Wait(TimeSpan.FromSeconds(10)), "holder never entered the guarded write");

        try
        {
            outcome = await guard.ExecuteAtomicWriteAsync(new AtomicWriteRequest<Node>(
                Identity: identity,
                LockTimeout: TimeSpan.FromMilliseconds(250),
                Envelope: Envelope(expectedRowVersion: 1),
                VerifyEntityNodeId: "T-02",
                WorkUnit: store => Task.FromResult(new MutationResult<Node>(true, T02, false, null, Array.Empty<string>(), null))));
        }
        finally
        {
            fake.ReleaseWrite!.Set();
        }

        Assert.True(holder.Join(TimeSpan.FromSeconds(10)), "holder thread did not exit");
        Assert.NotNull(outcome);
        Assert.Equal(DevelopmentControlConcurrencyOutcome.LockTimeout, outcome!.Outcome);
        Assert.False(outcome.Success);
    }

    [Fact]
    public void ExposesItsIdentityTimeoutAndInnerStore()
    {
        var identity = NewIdentity();
        var fake = new FakeDevelopmentControlStore();
        var guard = new ConcurrencyGuardedDevelopmentControlStore(
            fake, new NamedDevelopmentControlWriteLockFactory(), identity, TimeSpan.FromSeconds(7));

        Assert.Same(fake, guard.Inner);
        Assert.Equal(identity, guard.MutexIdentity);
        Assert.Equal(TimeSpan.FromSeconds(7), guard.LockTimeout);
    }

    // ------------------------------------------------------------------ in-memory fake

    // Minimal IDevelopmentControlStore double: holds a current-node dictionary for the
    // guard's RowVersion verification, counts how many mutating operations actually reached
    // it, and can block inside a write (on BlockOnWrite) while signalling WriteEntered so a
    // test can hold the guard's lock open.
    private sealed class FakeDevelopmentControlStore : IDevelopmentControlStore
    {
        public Dictionary<string, Node> Current { get; } = new(StringComparer.Ordinal);
        public int MutatingCalls;
        public volatile bool BlockNextWrite;
        public ManualResetEventSlim? WriteEntered { get; } = new();
        public ManualResetEventSlim? ReleaseWrite { get; } = new();

        public Task<ControlState?> GetControlStateAsync(CancellationToken ct = default)
            => Task.FromResult<ControlState?>(null);

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

        public Task<Node?> GetNextExecutableWorkItemAsync(CancellationToken ct = default)
            => Task.FromResult<Node?>(null);

        public Task<IReadOnlyList<ActivityLogEntry>> GetActivityLogAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ActivityLogEntry>>(Array.Empty<ActivityLogEntry>());

        public Task<ValidationResult> ValidateControlStoreAsync(CancellationToken ct = default)
            => Task.FromResult(new ValidationResult(true, Array.Empty<string>()));

        private void Block()
        {
            Interlocked.Increment(ref MutatingCalls);
            if (BlockNextWrite)
            {
                // Hold the caller (and therefore the guard's writer lock) open until the
                // test releases it, so the test can observe a second writer timing out.
                WriteEntered!.Set();
                ReleaseWrite!.Wait(TimeSpan.FromSeconds(30));
            }
        }

        private static MutationResult<T> Success<T>(T value) where T : class =>
            new(true, value, false, null, Array.Empty<string>(), "ACT-FAKE-001");

        public Task<MutationResult<Node>> CreateNodeAsync(Node node, MutationEnvelope envelope, CancellationToken ct = default)
        { Block(); return Task.FromResult(Success(node)); }

        public Task<MutationResult<Node>> UpdateNodeAsync(Node updatedNode, MutationEnvelope envelope, CancellationToken ct = default)
        { Block(); return Task.FromResult(Success(updatedNode)); }

        public Task<MutationResult<Node>> ReparentNodeAsync(NodeId nodeId, NodeId? newParentId, MutationEnvelope envelope, CancellationToken ct = default)
        { Block(); return Task.FromResult(Success(Current[nodeId.Value])); }

        public Task<MutationResult<Node>> RetireNodeAsync(NodeId nodeId, MutationEnvelope envelope, CancellationToken ct = default)
        { Block(); return Task.FromResult(Success(Current[nodeId.Value])); }

        public Task<MutationResult<Node>> AddDependencyAsync(NodeId nodeId, NodeId dependencyNodeId, MutationEnvelope envelope, CancellationToken ct = default)
        { Block(); return Task.FromResult(Success(Current[nodeId.Value])); }

        public Task<MutationResult<Node>> RemoveDependencyAsync(NodeId nodeId, NodeId dependencyNodeId, MutationEnvelope envelope, CancellationToken ct = default)
        { Block(); return Task.FromResult(Success(Current[nodeId.Value])); }

        public Task<MutationResult<Node>> ReserveWorkItemAsync(NodeId nodeId, ActorRef worker, string? branch, string? worktree, MutationEnvelope envelope, CancellationToken ct = default)
        { Block(); return Task.FromResult(Success(Current[nodeId.Value])); }

        public Task<MutationResult<ActivityLogEntry>> StartActivityAsync(NodeId nodeId, ActorRef worker, string operation, MutationEnvelope envelope, CancellationToken ct = default)
        { Block(); return Task.FromResult(Success(FakeActivity())); }

        public Task<MutationResult<ActivityLogEntry>> RecordHeartbeatAsync(string activityId, MutationEnvelope envelope, CancellationToken ct = default)
        { Block(); return Task.FromResult(Success(FakeActivity())); }

        public Task<MutationResult<ActivityLogEntry>> CompleteActivityAsync(string activityId, string result, string? evidence, MutationEnvelope envelope, CancellationToken ct = default)
        { Block(); return Task.FromResult(Success(FakeActivity())); }

        public Task<MutationResult<ActivityLogEntry>> FailActivityAsync(string activityId, string errorCode, string errorMessage, MutationEnvelope envelope, CancellationToken ct = default)
        { Block(); return Task.FromResult(Success(FakeActivity())); }

        public Task<MutationResult<Node>> ReleaseReservationAsync(NodeId nodeId, MutationEnvelope envelope, CancellationToken ct = default)
        { Block(); return Task.FromResult(Success(Current[nodeId.Value])); }

        public Task<MutationResult<Node>> CompleteWorkItemAsync(NodeId nodeId, string resultOrEvidence, MutationEnvelope envelope, CancellationToken ct = default)
        { Block(); return Task.FromResult(Success(Current[nodeId.Value])); }

        private static ActivityLogEntry FakeActivity() => new(
            "ACT-FAKE-001", DateTimeOffset.UtcNow, ActorType.Agent, "id", "name", "source",
            null, null, null, null, null, "op", "Node", "T-01", null, null, null, null,
            null, null, null, null, null, null, null, Array.Empty<string>(), null, null,
            null, null, null, null, null, DateTimeOffset.UtcNow);
    }
}
