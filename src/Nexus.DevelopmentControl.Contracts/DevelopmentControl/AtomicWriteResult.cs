namespace Nexus.Developer.Core.DevelopmentControl;

// WI-07-0.2.4: the controlled result of a guarded atomic write, surfacing the explicit
// DevelopmentControlConcurrencyOutcome alongside the underlying MutationResult semantics.
// Outcome is the classification: Success / ConcurrencyConflict / LockTimeout /
// ValidationFailure / NotFound / InvalidRequest / IoFailure. Value, ConflictDetails,
// ValidationErrors and ActivityLogEntryId mirror the underlying MutationResult so callers
// get the same data they would from a direct store call, plus the concurrency
// classification and the time spent waiting for the writer lock.
public sealed record AtomicWriteResult<T>(
    DevelopmentControlConcurrencyOutcome Outcome,
    bool Success,
    T? Value,
    object? ConflictDetails,
    IReadOnlyList<string> ValidationErrors,
    string? ActivityLogEntryId,
    TimeSpan? LockWait)
{
    public static AtomicWriteResult<T> FromMutation(MutationResult<T> mutation, TimeSpan? lockWait) => new(
        Classify(mutation),
        mutation.Success,
        mutation.Value,
        mutation.ConflictDetails,
        mutation.ValidationErrors,
        mutation.ActivityLogEntryId,
        lockWait);

    public static AtomicWriteResult<T> LockTimeout(TimeSpan? lockWait) => new(
        DevelopmentControlConcurrencyOutcome.LockTimeout,
        false, default, null,
        new[] { "The Development Control writer lock could not be acquired within the configured timeout." },
        null, lockWait);

    public static AtomicWriteResult<T> IoFailure(TimeSpan? lockWait, string message) => new(
        DevelopmentControlConcurrencyOutcome.IoFailure,
        false, default, null,
        new[] { message }, null, lockWait);

    private static DevelopmentControlConcurrencyOutcome Classify(MutationResult<T> mutation)
    {
        if (mutation.Success) return DevelopmentControlConcurrencyOutcome.Success;
        if (mutation.Conflict) return DevelopmentControlConcurrencyOutcome.ConcurrencyConflict;
        if (mutation.ValidationErrors.Count > 0) return DevelopmentControlConcurrencyOutcome.ValidationFailure;
        return DevelopmentControlConcurrencyOutcome.InvalidRequest;
    }
}

// A governed atomic write request: the store identity to lock, the bounded lock timeout,
// the optional optimistic-concurrency token to verify (VerifyEntityNodeId plus a non-null
// Envelope.ExpectedRowVersion, both required for a verification pass), and the work unit
// that performs the mutation(s) against the guarded store. The work unit runs while the
// writer lock is held, so for the existing single-operation adapter its underlying
// temp-write -> validate -> atomic-replace save is serialized against every other guarded
// writer. Multi-operation work units (calling several store operations inside one unit)
// are coordinated under the same lock; executing them as ONE workbook save is the boundary
// that requires the adapter to expose a batch/multi-op save entry point (SCOPE_CHANGE_REQUIRED).
public sealed record AtomicWriteRequest<T>(
    DevelopmentControlMutexIdentity Identity,
    TimeSpan LockTimeout,
    MutationEnvelope? Envelope,
    string? VerifyEntityNodeId,
    Func<IDevelopmentControlStore, Task<MutationResult<T>>> WorkUnit) where T : class;

// Coordinates a governed atomic write for a Development Control store: acquires the named
// cross-process writer lock, verifies the expected RowVersion/current state against
// authoritative persisted state, executes the work unit inside the critical section, and
// exposes a controlled AtomicWriteResult -- the Core-side expression of the
// temp-write -> validate -> replace principle (WI-07-0.2.4 acceptance criterion 4). The
// actual workbook temp-save/validate/promote steps are executed by the underlying store's
// save path, which the existing Excel adapter already performs atomically for a single
// operation.
public interface IDevelopmentControlAtomicWriteCoordinator
{
    string Kind { get; }

    Task<AtomicWriteResult<T>> ExecuteAsync<T>(
        AtomicWriteRequest<T> request,
        CancellationToken cancellationToken = default) where T : class;
}
