namespace Nexus.Developer.Core.DevelopmentControl;

// WI-07-0.2.4: creates cross-process writer locks for a Development Control store
// identity. Kind names the primitive ("named-mutex" | "named-semaphore") so callers can
// report which mechanism protects a given store. TryAcquire is synchronous and bounded: it
// blocks at most the supplied timeout and returns an attempt describing the outcome -- it
// never throws on contention and never waits indefinitely.
public interface IDevelopmentControlWriteLockFactory
{
    string Kind { get; }

    DevelopmentControlLockAttempt TryAcquire(
        DevelopmentControlMutexIdentity identity,
        TimeSpan timeout);
}
