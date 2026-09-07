using System.Diagnostics;

namespace Nexus.Developer.Core.DevelopmentControl;

// WI-07-0.2.4: an alternative named cross-process lock backed by a count-1 named
// System.Threading.Semaphore. Semantically equivalent to a named mutex for mutual
// exclusion, with one practical difference: WaitOne/Release are NOT thread-bound, so this
// implementation is safe to hold across an awaited critical section (async-first callers).
// The trade-off is that a semaphore has no AbandonedMutexException signal: a dead holder is
// handled by the kernel releasing the slot automatically (the workbook can never be left
// permanently locked), and any orphan temp file left by a crashed writer is swept by the
// store's own save path on the next write.
// IMPORTANT: choose ONE primitive per store and use it consistently across ALL writers.
// The kernel name is shared, but a Mutex and a Semaphore are different object types and
// cannot coexist under the same name.
public sealed class SemaphoreDevelopmentControlWriteLock : IDevelopmentControlWriteLock
{
    private readonly Semaphore _semaphore;
    private bool _held;

    private SemaphoreDevelopmentControlWriteLock(DevelopmentControlMutexIdentity identity, Semaphore semaphore)
    {
        Identity = identity;
        _semaphore = semaphore;
        _held = true;
    }

    public DevelopmentControlMutexIdentity Identity { get; }
    public bool IsHeld => _held;

    public static DevelopmentControlLockAttempt TryAcquire(
        DevelopmentControlMutexIdentity identity, TimeSpan timeout)
    {
        var watch = Stopwatch.StartNew();
        var semaphore = new Semaphore(1, 1, identity.MutexName);
        try
        {
            var acquired = semaphore.WaitOne(timeout);
            if (!acquired)
            {
                watch.Stop();
                semaphore.Dispose();
                return new DevelopmentControlLockAttempt(DevelopmentControlLockOutcome.Timeout, null, watch.Elapsed);
            }

            watch.Stop();
            return new DevelopmentControlLockAttempt(
                DevelopmentControlLockOutcome.Acquired,
                new SemaphoreDevelopmentControlWriteLock(identity, semaphore), watch.Elapsed);
        }
        catch (Exception ex) when (
            ex is UnauthorizedAccessException
                or WaitHandleCannotBeOpenedException
                or IOException
                or ObjectDisposedException)
        {
            watch.Stop();
            try { semaphore.Dispose(); } catch { /* best effort */ }
            return new DevelopmentControlLockAttempt(DevelopmentControlLockOutcome.SystemFailure, null, watch.Elapsed);
        }
    }

    public void Release()
    {
        if (!_held) return;
        try { _semaphore.Release(); }
        finally { _held = false; }
    }

    public void Dispose()
    {
        Release();
        _semaphore.Dispose();
    }
}

// Default named-semaphore factory. Use it consistently for a given store across all
// writers; it is the async-safe alternative to NamedDevelopmentControlWriteLockFactory.
public sealed class SemaphoreDevelopmentControlWriteLockFactory : IDevelopmentControlWriteLockFactory
{
    public string Kind => "named-semaphore";

    public DevelopmentControlLockAttempt TryAcquire(DevelopmentControlMutexIdentity identity, TimeSpan timeout)
        => SemaphoreDevelopmentControlWriteLock.TryAcquire(identity, timeout);
}
