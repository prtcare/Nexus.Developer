namespace Nexus.Developer.Core.DevelopmentControl;

// WI-07-0.2.4: the guarded-store CONTRACT (moved with the zero-IO DevControl contract
// set to the bootstrap-safe shared assembly in SP1-WAVE-04 Lane A). The concrete
// ConcurrencyGuardedDevelopmentControlStore decorator -- which acquires the named
// cross-process mutex and runs the mutation inside the critical section -- remains a
// runtime implementation in Nexus.Developer.Core (see
// src/Nexus.Developer.Core/DevelopmentControl/ConcurrencyGuardedDevelopmentControlStore.cs).
public interface IConcurrencyGuardedDevelopmentControlStore : IDevelopmentControlStore
{
    IDevelopmentControlStore Inner { get; }
    DevelopmentControlMutexIdentity MutexIdentity { get; }
    TimeSpan LockTimeout { get; }

    Task<AtomicWriteResult<T>> ExecuteAtomicWriteAsync<T>(
        AtomicWriteRequest<T> request,
        CancellationToken cancellationToken = default) where T : class;
}
