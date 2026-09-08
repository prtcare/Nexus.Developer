namespace Nexus.Developer.Core.DevelopmentControl;

// WI-07-0.2.4 (scope-amended CHG-20260830-017): the adapter integration surface that makes
// a multi-operation atomic work unit execute as ONE governed workbook save. The Core
// coordinator/guard owns the named cross-process writer lock and the control flow; this
// contract lets the Infrastructure adapter claim the persistence-level capability Core must
// not perform -- open the workbook ONCE, execute every store operation the work unit calls
// against the SAME in-memory workbook state, append all Version History / Activity Log
// evidence, then perform a single temp-write -> validate -> atomic-replace only when the
// whole unit succeeded. All-or-nothing: a failure at any operation (a validation error, a
// stale RowVersion, a structural anomaly) aborts before any canonical write, so no partial
// Operation 1 is ever persisted when Operation 2 fails.
//
// The runner is invoked while the named cross-process writer lock is held (by
// DevelopmentControlAtomicWriteCoordinator or ConcurrencyGuardedDevelopmentControlStore), so
// no guarded writer can interleave between the optimistic RowVersion pre-verification and
// the single save. The adapter performs that pre-verification against the opened persisted
// state this same save will replace; a missing entity is surfaced as NotFound, a stale
// ExpectedRowVersion as ConcurrencyConflict with the current node as ConflictDetails.
public interface IDevelopmentControlAtomicWorkUnitRunner
{
    Task<AtomicWriteResult<T>> ExecuteAtomicWorkUnitAsync<T>(
        Func<IDevelopmentControlStore, Task<MutationResult<T>>> workUnit,
        MutationEnvelope? envelope,
        string? verifyEntityNodeId,
        CancellationToken cancellationToken = default) where T : class;
}
