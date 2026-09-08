namespace Nexus.Developer.Core.DevelopmentControl;

// WI-07-0.2.4: the writer-lock abstraction protecting Development Control workbook writes.
// A lock is acquired with a bounded timeout (never an infinite wait), held across the
// governed critical section, and released deterministically and exception-safely via
// Dispose (or an explicit Release). The acquire reports how acquisition failed so callers
// surface a controlled LOCK_TIMEOUT / IO failure instead of an exception.

public enum DevelopmentControlLockOutcome
{
    Acquired = 1,

    // The acquire exceeded its timeout -- there is no silent infinite wait.
    Timeout = 2,

    // The previous holder's process died while holding the lock; the kernel transferred
    // ownership to this acquire, so the lock IS held and the caller may proceed. The dead
    // writer never committed a governed change, but the caller should treat the preceding
    // write as failed/unknown rather than relying on it.
    AbandonedRecovered = 3,

    // The underlying lock could not be created or waited on (system-level failure). The
    // caller must not attempt the write.
    SystemFailure = 4
}

// Result of a bounded lock-acquire attempt. Lock is non-null exactly when the lock is held
// (Outcome is Acquired or AbandonedRecovered). Elapsed is the time the acquire waited.
public sealed record DevelopmentControlLockAttempt(
    DevelopmentControlLockOutcome Outcome,
    IDevelopmentControlWriteLock? Lock,
    TimeSpan Elapsed);

// A held (or released) cross-process writer lock. Dispose is the exception-safe release
// path; releasing twice is a no-op. Implementations may carry platform thread-affinity
// requirements -- see NamedDevelopmentControlMutex.
public interface IDevelopmentControlWriteLock : IDisposable
{
    DevelopmentControlMutexIdentity Identity { get; }
    bool IsHeld { get; }
    void Release();
}
