namespace Nexus.Developer.Core.DevelopmentControl;

// WI-07-0.2.4: default coordinator implementing the atomic-write contract over any
// IDevelopmentControlStore. Sequence (acceptance criterion 4):
//   1. acquire the named cross-process writer lock (bounded timeout -> LockTimeout)
//   2. verify the expected RowVersion / current state against authoritative persisted state
//      (when the request carries VerifyEntityNodeId and a non-null ExpectedRowVersion)
//   3. run the governed mutation work unit through the store, whose existing save path is
//      the temp-write -> validate -> atomic replace for a single operation
//   4..6. the temporary-workbook write/validate/promote happens INSIDE the underlying
//      store's atomic save -- the coordinator never bypasses it, so a failed validation can
//      never knowingly replace the canonical workbook, and a failed mutation never leaves a
//      half-applied canonical state
//   7. release the lock deterministically (using scope)
//   8. expose a controlled AtomicWriteResult
// The work unit is completed synchronously while the named mutex is held (see the
// NamedDevelopmentControlMutex thread-affinity note); this is correct for the synchronous
// Excel adapter and for any store that completes its operations synchronously.
public sealed class DevelopmentControlAtomicWriteCoordinator : IDevelopmentControlAtomicWriteCoordinator
{
    private readonly IDevelopmentControlStore _store;
    private readonly IDevelopmentControlWriteLockFactory _lockFactory;

    public DevelopmentControlAtomicWriteCoordinator(
        IDevelopmentControlStore store,
        IDevelopmentControlWriteLockFactory lockFactory)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _lockFactory = lockFactory ?? throw new ArgumentNullException(nameof(lockFactory));
    }

    public string Kind => _lockFactory.Kind;

    public Task<AtomicWriteResult<T>> ExecuteAsync<T>(
        AtomicWriteRequest<T> request, CancellationToken cancellationToken = default) where T : class
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        return Task.FromResult(Execute(request, cancellationToken));
    }

    private AtomicWriteResult<T> Execute<T>(
        AtomicWriteRequest<T> request, CancellationToken cancellationToken) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 1. acquire writer protection (controlled timeout, no infinite wait)
        var attempt = _lockFactory.TryAcquire(request.Identity, request.LockTimeout);
        using (var writerLock = attempt.Lock)
        {
            if (writerLock is null)
            {
                return attempt.Outcome == DevelopmentControlLockOutcome.Timeout
                    ? AtomicWriteResult<T>.LockTimeout(attempt.Elapsed)
                    : AtomicWriteResult<T>.IoFailure(attempt.Elapsed,
                        "Could not acquire the Development Control writer lock (" + attempt.Outcome + ").");
            }

            // 2. verify expected RowVersion / current state against authoritative persisted
            //    state, inside the lock so no guarded writer can interleave between verify
            //    and write
            if (request.VerifyEntityNodeId is not null && request.Envelope?.ExpectedRowVersion is not null)
            {
                var current = _store.GetNodeAsync(new NodeId(request.VerifyEntityNodeId), cancellationToken)
                    .GetAwaiter().GetResult();
                if (current is null)
                {
                    return new AtomicWriteResult<T>(
                        DevelopmentControlConcurrencyOutcome.NotFound, false, default, null,
                        new[] { $"No current version of node '{request.VerifyEntityNodeId}'." },
                        null, attempt.Elapsed);
                }

                if (request.Envelope.ExpectedRowVersion != current.RowVersion)
                {
                    return new AtomicWriteResult<T>(
                        DevelopmentControlConcurrencyOutcome.ConcurrencyConflict, false, default,
                        current, Array.Empty<string>(), null, attempt.Elapsed);
                }
            }

            // 3..6. run the governed work unit. A store that exposes the atomic work-unit
            //       runner executes the whole unit against ONE open workbook -> one
            //       temp-write -> validate -> atomic-replace (a multi-operation atomic save);
            //       the adapter performs the optimistic RowVersion pre-verification against
            //       the opened persisted state while the writer lock is held, so the generic
            //       verify above is superseded on that path. A plain store runs the unit
            //       through its existing per-operation save path.
            AtomicWriteResult<T> result;
            if (_store is IDevelopmentControlAtomicWorkUnitRunner runner)
            {
                result = runner.ExecuteAtomicWorkUnitAsync<T>(
                        request.WorkUnit, request.Envelope, request.VerifyEntityNodeId, cancellationToken)
                    .GetAwaiter().GetResult() with { LockWait = attempt.Elapsed };
            }
            else
            {
                var mutation = request.WorkUnit(_store).GetAwaiter().GetResult();
                result = AtomicWriteResult<T>.FromMutation(mutation, attempt.Elapsed);
            }

            // 7. release (using scope); 8. expose the controlled result
            return result;
        }
    }
}
