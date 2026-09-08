namespace Nexus.Developer.Core.DevelopmentControl;

// WI-07-0.2.4: a drop-in decorator that makes ANY IDevelopmentControlStore safe under
// concurrent writers by running every mutating operation inside a named cross-process
// mutex (deterministic identity derived from the store identity, bounded timeout,
// exception-safe release) and re-verifying the optimistic RowVersion against
// authoritative persisted state before the write. It composes with the existing Excel
// adapter with no adapter change: reads pass through, and each guarded write is delegated
// to the inner store whose existing temp-write -> validate -> atomic-replace save runs
// inside the critical section. The inner adapter's own optimistic check remains the
// authoritative guard even against writers that bypass the mutex.
//
// Lock-outcome mapping for the 22-operation IDevelopmentControlStore contract: the store
// result type cannot represent a lock timeout, so on Timeout/SystemFailure the mutation
// returns a failed MutationResult whose ValidationErrors names the explicit LOCK_TIMEOUT /
// lock-failure classification. Callers that need a structured outcome use
// ExecuteAtomicWriteAsync (returns AtomicWriteResult<T> with the full
// DevelopmentControlConcurrencyOutcome) or the DevelopmentControlAtomicWriteCoordinator.
// The IConcurrencyGuardedDevelopmentControlStore contract now lives in the bootstrap-safe
// shared assembly Nexus.DevelopmentControl.Contracts (SP1-WAVE-04 Lane A). This file keeps
// only the concrete guarded-store runtime decorator.
public sealed class ConcurrencyGuardedDevelopmentControlStore : IConcurrencyGuardedDevelopmentControlStore
{
    private readonly IDevelopmentControlStore _inner;
    private readonly IDevelopmentControlWriteLockFactory _lockFactory;
    private readonly DevelopmentControlMutexIdentity _identity;
    private readonly TimeSpan _lockTimeout;

    public ConcurrencyGuardedDevelopmentControlStore(
        IDevelopmentControlStore inner,
        IDevelopmentControlWriteLockFactory lockFactory,
        DevelopmentControlMutexIdentity identity,
        TimeSpan? lockTimeout = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _lockFactory = lockFactory ?? throw new ArgumentNullException(nameof(lockFactory));
        _identity = identity ?? throw new ArgumentNullException(nameof(identity));
        _lockTimeout = lockTimeout ?? TimeSpan.FromSeconds(10);
        if (_lockTimeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(lockTimeout));
    }

    public IDevelopmentControlStore Inner => _inner;
    public DevelopmentControlMutexIdentity MutexIdentity => _identity;
    public TimeSpan LockTimeout => _lockTimeout;

    // ------------------------------------------------------------------ reads
    // Reads pass through: a writer lock guards writes, and a read of the atomically
    // replaced workbook observes a consistent whole file. Reader/writer file-lock
    // contention during a replace surfaces as an IO error from the underlying adapter.

    public Task<ControlState?> GetControlStateAsync(CancellationToken ct = default) => _inner.GetControlStateAsync(ct);
    public Task<Node?> GetNodeAsync(NodeId nodeId, CancellationToken ct = default) => _inner.GetNodeAsync(nodeId, ct);
    public Task<IReadOnlyList<Node>> GetSubtreeAsync(NodeId rootNodeId, CancellationToken ct = default) => _inner.GetSubtreeAsync(rootNodeId, ct);
    public Task<IReadOnlyList<Node>> SearchNodesAsync(NodeSearchCriteria criteria, CancellationToken ct = default) => _inner.SearchNodesAsync(criteria, ct);
    public Task<PreflightResult> RunPreflightAsync(PreflightDeclaration declaration, CancellationToken ct = default) => _inner.RunPreflightAsync(declaration, ct);
    public Task<IReadOnlyList<ActiveChange>> GetActiveChangesAsync(CancellationToken ct = default) => _inner.GetActiveChangesAsync(ct);
    public Task<Node?> GetNextExecutableWorkItemAsync(CancellationToken ct = default) => _inner.GetNextExecutableWorkItemAsync(ct);
    public Task<IReadOnlyList<ActivityLogEntry>> GetActivityLogAsync(CancellationToken ct = default) => _inner.GetActivityLogAsync(ct);
    public Task<ValidationResult> ValidateControlStoreAsync(CancellationToken ct = default) => _inner.ValidateControlStoreAsync(ct);

    // ------------------------------------------------------------------ mutations

    public Task<MutationResult<Node>> CreateNodeAsync(Node node, MutationEnvelope envelope, CancellationToken ct = default)
        => Task.FromResult(GuardedWrite<Node>(ct, nodeIdToVerify: null, envelope,
            store => store.CreateNodeAsync(node, envelope).GetAwaiter().GetResult()));

    public Task<MutationResult<Node>> UpdateNodeAsync(Node updatedNode, MutationEnvelope envelope, CancellationToken ct = default)
        => Task.FromResult(GuardedWrite<Node>(ct, updatedNode.NodeId.Value, envelope,
            store => store.UpdateNodeAsync(updatedNode, envelope).GetAwaiter().GetResult()));

    public Task<MutationResult<Node>> ReparentNodeAsync(NodeId nodeId, NodeId? newParentId, MutationEnvelope envelope, CancellationToken ct = default)
        => Task.FromResult(GuardedWrite<Node>(ct, nodeId.Value, envelope,
            store => store.ReparentNodeAsync(nodeId, newParentId, envelope).GetAwaiter().GetResult()));

    public Task<MutationResult<Node>> RetireNodeAsync(NodeId nodeId, MutationEnvelope envelope, CancellationToken ct = default)
        => Task.FromResult(GuardedWrite<Node>(ct, nodeId.Value, envelope,
            store => store.RetireNodeAsync(nodeId, envelope).GetAwaiter().GetResult()));

    public Task<MutationResult<Node>> AddDependencyAsync(NodeId nodeId, NodeId dependencyNodeId, MutationEnvelope envelope, CancellationToken ct = default)
        => Task.FromResult(GuardedWrite<Node>(ct, nodeId.Value, envelope,
            store => store.AddDependencyAsync(nodeId, dependencyNodeId, envelope).GetAwaiter().GetResult()));

    public Task<MutationResult<Node>> RemoveDependencyAsync(NodeId nodeId, NodeId dependencyNodeId, MutationEnvelope envelope, CancellationToken ct = default)
        => Task.FromResult(GuardedWrite<Node>(ct, nodeId.Value, envelope,
            store => store.RemoveDependencyAsync(nodeId, dependencyNodeId, envelope).GetAwaiter().GetResult()));

    public Task<MutationResult<Node>> ReserveWorkItemAsync(NodeId nodeId, ActorRef worker, string? branch, string? worktree, MutationEnvelope envelope, CancellationToken ct = default)
        => Task.FromResult(GuardedWrite<Node>(ct, nodeId.Value, envelope,
            store => store.ReserveWorkItemAsync(nodeId, worker, branch, worktree, envelope).GetAwaiter().GetResult()));

    public Task<MutationResult<ActivityLogEntry>> StartActivityAsync(NodeId nodeId, ActorRef worker, string operation, MutationEnvelope envelope, CancellationToken ct = default)
        => Task.FromResult(GuardedWrite<ActivityLogEntry>(ct, null, envelope,
            store => store.StartActivityAsync(nodeId, worker, operation, envelope).GetAwaiter().GetResult()));

    public Task<MutationResult<ActivityLogEntry>> RecordHeartbeatAsync(string activityId, MutationEnvelope envelope, CancellationToken ct = default)
        => Task.FromResult(GuardedWrite<ActivityLogEntry>(ct, null, envelope,
            store => store.RecordHeartbeatAsync(activityId, envelope).GetAwaiter().GetResult()));

    public Task<MutationResult<ActivityLogEntry>> CompleteActivityAsync(string activityId, string result, string? evidence, MutationEnvelope envelope, CancellationToken ct = default)
        => Task.FromResult(GuardedWrite<ActivityLogEntry>(ct, null, envelope,
            store => store.CompleteActivityAsync(activityId, result, evidence, envelope).GetAwaiter().GetResult()));

    public Task<MutationResult<ActivityLogEntry>> FailActivityAsync(string activityId, string errorCode, string errorMessage, MutationEnvelope envelope, CancellationToken ct = default)
        => Task.FromResult(GuardedWrite<ActivityLogEntry>(ct, null, envelope,
            store => store.FailActivityAsync(activityId, errorCode, errorMessage, envelope).GetAwaiter().GetResult()));

    public Task<MutationResult<Node>> ReleaseReservationAsync(NodeId nodeId, MutationEnvelope envelope, CancellationToken ct = default)
        => Task.FromResult(GuardedWrite<Node>(ct, nodeId.Value, envelope,
            store => store.ReleaseReservationAsync(nodeId, envelope).GetAwaiter().GetResult()));

    public Task<MutationResult<Node>> CompleteWorkItemAsync(NodeId nodeId, string resultOrEvidence, MutationEnvelope envelope, CancellationToken ct = default)
        => Task.FromResult(GuardedWrite<Node>(ct, nodeId.Value, envelope,
            store => store.CompleteWorkItemAsync(nodeId, resultOrEvidence, envelope).GetAwaiter().GetResult()));

    // ------------------------------------------------------------------ extended surface

    public Task<AtomicWriteResult<T>> ExecuteAtomicWriteAsync<T>(
        AtomicWriteRequest<T> request, CancellationToken cancellationToken = default) where T : class
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        cancellationToken.ThrowIfCancellationRequested();

        var attempt = _lockFactory.TryAcquire(_identity, request.LockTimeout);
        using (var writerLock = attempt.Lock)
        {
            if (writerLock is null)
            {
                return Task.FromResult(attempt.Outcome == DevelopmentControlLockOutcome.Timeout
                    ? AtomicWriteResult<T>.LockTimeout(attempt.Elapsed)
                    : AtomicWriteResult<T>.IoFailure(attempt.Elapsed,
                        "Could not acquire the Development Control writer lock (" + attempt.Outcome + ")."));
            }

            // A runner-capable inner store executes the whole work unit as ONE governed save
            // (open once, N mutations against one in-memory workbook, one temp-write ->
            // validate -> atomic-replace); the adapter performs the optimistic RowVersion
            // pre-verification against the opened persisted state while the lock is held.
            // A plain inner store keeps the generic verify + per-operation save path below.
            if (_inner is IDevelopmentControlAtomicWorkUnitRunner runner)
            {
                var routed = runner.ExecuteAtomicWorkUnitAsync<T>(
                        request.WorkUnit, request.Envelope, request.VerifyEntityNodeId, cancellationToken)
                    .GetAwaiter().GetResult();
                return Task.FromResult(routed with { LockWait = attempt.Elapsed });
            }

            var mutation = RunGuarded<T>(cancellationToken, request.VerifyEntityNodeId, request.Envelope,
                store => request.WorkUnit(store).GetAwaiter().GetResult());
            return Task.FromResult(AtomicWriteResult<T>.FromMutation(mutation, attempt.Elapsed));
        }
    }

    // ------------------------------------------------------------------ guarded engine
    // Runs the write inside the named cross-process mutex. The inner write is completed
    // synchronously while the lock is held so Mutex ownership stays on the acquiring thread
    // (System.Threading.Mutex thread affinity); the Excel adapter executes synchronously
    // under Task.FromResult, so this does not block in practice.
    private MutationResult<T> GuardedWrite<T>(
        CancellationToken ct, string? nodeIdToVerify, MutationEnvelope envelope,
        Func<IDevelopmentControlStore, MutationResult<T>> write) where T : class
    {
        ct.ThrowIfCancellationRequested();

        var attempt = _lockFactory.TryAcquire(_identity, _lockTimeout);
        using (var writerLock = attempt.Lock)
        {
            if (writerLock is null)
            {
                // The 22-op contract cannot express a lock timeout, so it is surfaced as a
                // failed mutation naming the outcome explicitly; structured callers use the
                // AtomicWriteResult surface instead.
                var message = attempt.Outcome == DevelopmentControlLockOutcome.Timeout
                    ? "LOCK_TIMEOUT: the Development Control writer lock could not be acquired within the configured timeout."
                    : "LOCK_FAILURE: could not acquire the Development Control writer lock (" + attempt.Outcome + ").";
                return new MutationResult<T>(false, default, false, null, new[] { message }, null);
            }

            return RunGuarded<T>(ct, nodeIdToVerify, envelope, write);
        }
    }

    private MutationResult<T> RunGuarded<T>(
        CancellationToken ct, string? nodeIdToVerify, MutationEnvelope? envelope,
        Func<IDevelopmentControlStore, MutationResult<T>> write) where T : class
    {
        ct.ThrowIfCancellationRequested();

        // Optimistic RowVersion verification against authoritative persisted state, inside
        // the lock so no guarded writer can interleave between verify and write.
        if (nodeIdToVerify is not null && envelope?.ExpectedRowVersion is not null)
        {
            var current = _inner.GetNodeAsync(new NodeId(nodeIdToVerify), ct).GetAwaiter().GetResult();
            if (current is null)
            {
                return new MutationResult<T>(false, default, false, null,
                    new[] { $"No current version of node '{nodeIdToVerify}'." }, null);
            }

            if (envelope.ExpectedRowVersion != current.RowVersion)
            {
                return new MutationResult<T>(false, default, true, current, Array.Empty<string>(), null);
            }
        }

        return write(_inner);
    }
}
