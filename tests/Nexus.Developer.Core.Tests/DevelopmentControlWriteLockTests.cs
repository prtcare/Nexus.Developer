using Nexus.Developer.Core.DevelopmentControl;
using Xunit;

namespace Nexus.Developer.Core.Tests;

// WI-07-0.2.4 stabilization (SP1-M00): the bounded cross-process writer lock. These tests
// exercise both primitives (named mutex and named semaphore) against real kernel objects:
// acquire when free, bounded timeout when another thread holds the same identity, and
// idempotent exception-safe release. Timeout tests hold the lock on a dedicated background
// thread because System.Threading.Mutex is thread-affine -- Release must run on the thread
// that called WaitOne.
public class DevelopmentControlWriteLockTests
{
    private static DevelopmentControlMutexIdentity NewIdentity() =>
        // Slash-free, extension-free token: an identity with '/' or '\' is treated by
        // DevelopmentControlMutexIdentity as a filesystem path and path-normalized. The lock
        // tests need only a distinct, deterministic identity used verbatim.
        DevelopmentControlMutexIdentity.FromStoreIdentity("test-store-lock-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void NamedMutex_TryAcquire_AcquiresWhenFree()
    {
        var attempt = NamedDevelopmentControlMutex.TryAcquire(NewIdentity(), TimeSpan.FromSeconds(5));

        Assert.Equal(DevelopmentControlLockOutcome.Acquired, attempt.Outcome);
        Assert.NotNull(attempt.Lock);
        Assert.True(attempt.Lock!.IsHeld);
        attempt.Lock.Dispose();
    }

    [Fact]
    public void NamedMutex_Release_IsIdempotentAndDisposeIsSafeAfterRelease()
    {
        var attempt = NamedDevelopmentControlMutex.TryAcquire(NewIdentity(), TimeSpan.FromSeconds(5));
        var writerLock = attempt.Lock!;

        writerLock.Release();
        Assert.False(writerLock.IsHeld);
        writerLock.Release(); // second release is a no-op
        writerLock.Dispose(); // release-then-dispose is safe
    }

    [Fact]
    public void NamedMutex_TryAcquire_TimesOutWhenAnotherThreadHoldsTheSameIdentity()
    {
        var identity = NewIdentity();
        var holder = new BackgroundLockHolder(() => NamedDevelopmentControlMutex.TryAcquire(identity, TimeSpan.FromSeconds(5)));
        try
        {
            Assert.True(holder.Entered.Wait(TimeSpan.FromSeconds(10)), "holder never acquired the lock");

            var attempt = NamedDevelopmentControlMutex.TryAcquire(identity, TimeSpan.FromMilliseconds(250));

            Assert.Equal(DevelopmentControlLockOutcome.Timeout, attempt.Outcome);
            Assert.Null(attempt.Lock);
        }
        finally
        {
            holder.Release();
        }
    }

    [Fact]
    public void NamedMutex_TryAcquire_OnADifferentIdentity_IsUnaffectedByTheHeldLock()
    {
        var held = NewIdentity();
        var other = NewIdentity();
        var holder = new BackgroundLockHolder(() => NamedDevelopmentControlMutex.TryAcquire(held, TimeSpan.FromSeconds(5)));
        try
        {
            Assert.True(holder.Entered.Wait(TimeSpan.FromSeconds(10)), "holder never acquired the lock");

            var attempt = NamedDevelopmentControlMutex.TryAcquire(other, TimeSpan.FromSeconds(5));

            Assert.Equal(DevelopmentControlLockOutcome.Acquired, attempt.Outcome);
            attempt.Lock!.Dispose();
        }
        finally
        {
            holder.Release();
        }
    }

    [Fact]
    public void NamedMutexFactory_ExposesItsKind()
    {
        var factory = new NamedDevelopmentControlWriteLockFactory();

        Assert.Equal("named-mutex", factory.Kind);
        var attempt = factory.TryAcquire(NewIdentity(), TimeSpan.FromSeconds(5));
        Assert.Equal(DevelopmentControlLockOutcome.Acquired, attempt.Outcome);
        attempt.Lock!.Dispose();
    }

    [Fact]
    public void SemaphoreMutex_TryAcquire_AcquiresAndReleases()
    {
        var attempt = SemaphoreDevelopmentControlWriteLock.TryAcquire(NewIdentity(), TimeSpan.FromSeconds(5));

        Assert.Equal(DevelopmentControlLockOutcome.Acquired, attempt.Outcome);
        Assert.NotNull(attempt.Lock);
        Assert.True(attempt.Lock!.IsHeld);
        attempt.Lock.Release();
        Assert.False(attempt.Lock.IsHeld);
        attempt.Lock.Dispose();
    }

    [Fact]
    public void SemaphoreMutex_TryAcquire_TimesOutWhenAnotherThreadHoldsTheSameIdentity()
    {
        var identity = NewIdentity();
        var holder = new BackgroundLockHolder(() => SemaphoreDevelopmentControlWriteLock.TryAcquire(identity, TimeSpan.FromSeconds(5)));
        try
        {
            Assert.True(holder.Entered.Wait(TimeSpan.FromSeconds(10)), "holder never acquired the lock");

            var attempt = SemaphoreDevelopmentControlWriteLock.TryAcquire(identity, TimeSpan.FromMilliseconds(250));

            Assert.Equal(DevelopmentControlLockOutcome.Timeout, attempt.Outcome);
            Assert.Null(attempt.Lock);
        }
        finally
        {
            holder.Release();
        }
    }

    [Fact]
    public void SemaphoreFactory_ExposesItsKind()
    {
        var factory = new SemaphoreDevelopmentControlWriteLockFactory();

        Assert.Equal("named-semaphore", factory.Kind);
        var attempt = factory.TryAcquire(NewIdentity(), TimeSpan.FromSeconds(5));
        Assert.Equal(DevelopmentControlLockOutcome.Acquired, attempt.Outcome);
        attempt.Lock!.Dispose();
    }

    // Acquires the supplied lock on a dedicated background thread (satisfying Mutex thread
    // affinity) and holds it until Release() is called. Entered is set once the lock is held
    // (or the acquire attempt finished) so the test can observe the outcome.
    private sealed class BackgroundLockHolder
    {
        private readonly Func<DevelopmentControlLockAttempt> _acquire;
        private readonly ManualResetEventSlim _entered = new();
        private readonly ManualResetEventSlim _release = new();
        private readonly Thread _thread;
        private DevelopmentControlLockAttempt _attempt = null!;

        public BackgroundLockHolder(Func<DevelopmentControlLockAttempt> acquire)
        {
            _acquire = acquire;
            _thread = new Thread(Run) { IsBackground = true };
            _thread.Start();
        }

        public ManualResetEventSlim Entered => _entered;
        public DevelopmentControlLockAttempt Attempt => _attempt;

        public void Release()
        {
            _release.Set();
            Assert.True(_thread.Join(TimeSpan.FromSeconds(10)), "holder thread did not exit");
        }

        private void Run()
        {
            try
            {
                _attempt = _acquire();
            }
            finally
            {
                _entered.Set(); // signal even on failure so the test can observe and Release()
            }

            _release.Wait(TimeSpan.FromSeconds(30));
            _attempt.Lock?.Dispose();
        }
    }
}
