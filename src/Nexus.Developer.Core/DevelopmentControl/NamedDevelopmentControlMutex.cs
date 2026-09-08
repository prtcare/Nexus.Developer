using System.Diagnostics;

namespace Nexus.Developer.Core.DevelopmentControl;

// WI-07-0.2.4: a named cross-process mutex protecting Development Control workbook writes,
// backed by System.Threading.Mutex -- a real Windows kernel mutex shared by every process
// that uses the same DevelopmentControlMutexIdentity. Behaviors required by the work item:
//   - deterministic identity   (DevelopmentControlMutexIdentity.MutexName)
//   - usable across processes  (a kernel object; see the concurrency verification)
//   - controlled timeout       (WaitOne(timeout); never an infinite wait)
//   - controlled failure       (returns Timeout/SystemFailure; never throws on contention)
//   - deterministic release    (Release / Dispose; releasing twice is a no-op)
//   - exception-safe disposal  (using (var writerLock = ...))
//   - abandoned mutex          (AbandonedMutexException -> AbandonedRecovered: the previous
//                               holder died mid-write; the kernel has transferred ownership
//                               to this thread, so the lock is held and the caller may
//                               proceed -- no permanently locked workbook)
// THREAD AFFINITY: System.Threading.Mutex ownership is bound to the thread that called
// WaitOne; Release/ReleaseMutex must run on that same thread. Callers MUST NOT await
// between TryAcquire and Release/Dispose. The guarded store and the atomic-write
// coordinator satisfy this by completing the inner write synchronously while the lock is
// held -- correct for the synchronous Excel adapter. SemaphoreDevelopmentControlWriteLock
// is the any-thread-release alternative for genuinely-async critical sections.
public sealed class NamedDevelopmentControlMutex : IDevelopmentControlWriteLock
{
    private readonly Mutex _mutex;
    private bool _held;

    private NamedDevelopmentControlMutex(DevelopmentControlMutexIdentity identity, Mutex mutex)
    {
        Identity = identity;
        _mutex = mutex;
        _held = true;
    }

    public DevelopmentControlMutexIdentity Identity { get; }
    public bool IsHeld => _held;

    public static DevelopmentControlLockAttempt TryAcquire(
        DevelopmentControlMutexIdentity identity, TimeSpan timeout)
    {
        var watch = Stopwatch.StartNew();
        var mutex = new Mutex(false, identity.MutexName);
        try
        {
            bool acquired;
            try
            {
                acquired = mutex.WaitOne(timeout);
            }
            catch (AbandonedMutexException)
            {
                // The previous owner died without releasing; ownership transferred to us.
                watch.Stop();
                return new DevelopmentControlLockAttempt(
                    DevelopmentControlLockOutcome.AbandonedRecovered,
                    new NamedDevelopmentControlMutex(identity, mutex), watch.Elapsed);
            }

            if (!acquired)
            {
                watch.Stop();
                mutex.Dispose();
                return new DevelopmentControlLockAttempt(DevelopmentControlLockOutcome.Timeout, null, watch.Elapsed);
            }

            watch.Stop();
            return new DevelopmentControlLockAttempt(
                DevelopmentControlLockOutcome.Acquired,
                new NamedDevelopmentControlMutex(identity, mutex), watch.Elapsed);
        }
        catch (Exception ex) when (
            ex is UnauthorizedAccessException
                or WaitHandleCannotBeOpenedException
                or IOException
                or ObjectDisposedException)
        {
            watch.Stop();
            try { mutex.Dispose(); } catch { /* best effort */ }
            return new DevelopmentControlLockAttempt(DevelopmentControlLockOutcome.SystemFailure, null, watch.Elapsed);
        }
    }

    public void Release()
    {
        if (!_held) return;
        try
        {
            _mutex.ReleaseMutex();
        }
        finally
        {
            _held = false;
        }
    }

    public void Dispose()
    {
        Release();
        _mutex.Dispose();
    }
}

// Default named-mutex factory. Use it consistently for a given store across all writers;
// the guarded store and the atomic-write coordinator both accept it.
public sealed class NamedDevelopmentControlWriteLockFactory : IDevelopmentControlWriteLockFactory
{
    public string Kind => "named-mutex";

    public DevelopmentControlLockAttempt TryAcquire(DevelopmentControlMutexIdentity identity, TimeSpan timeout)
        => NamedDevelopmentControlMutex.TryAcquire(identity, timeout);
}
