using Nexus.Developer.Core.DevelopmentControl;
using Xunit;

namespace Nexus.Developer.Core.Tests;

// WI-07-0.2.4 stabilization (SP1-M00): the bounded cross-process writer lock. These tests
// exercise both primitives (named mutex and named semaphore) against real kernel objects:
// acquire when free, bounded timeout when another thread holds the same identity, and
// idempotent exception-safe release. Timeout tests hold the lock on a dedicated background
// thread because System.Threading.Mutex is thread-affine -- Release must run on the thread
// that called WaitOne.
//
// Platform-awareness: the NAMED Mutex primitive is supported on all platforms (Linux CI
// included), so the NamedDevelopmentControlMutex tests run everywhere. The NAMED Semaphore
// primitive is Windows-only (the runtime throws PlatformNotSupportedException for a named
// constructor off Windows), so the Semaphore tests branch on OperatingSystem.IsWindows():
// on Windows they assert the real acquire/contention behavior; off Windows they assert the
// implementation's deterministic, controlled SystemFailure for the unsupported primitive --
// never letting a PlatformNotSupportedException escape (a threaded acquire that throws would
// abort the whole test host).
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

    // Acquire/release semantics for the named semaphore. On Windows this is the real
    // primitive (acquired -> held -> released -> disposed). Off Windows the named primitive
    // is unsupported, and the implementation fails closed as a deterministic SystemFailure --
    // asserted here on every platform so the unsupported behavior is explicit and a raw
    // PlatformNotSupportedException is never observed by a caller.
    [Fact]
    public void SemaphoreMutex_TryAcquire_AcquiresAndReleases()
    {
        var attempt = SemaphoreDevelopmentControlWriteLock.TryAcquire(NewIdentity(), TimeSpan.FromSeconds(5));

        if (OperatingSystem.IsWindows())
        {
            Assert.Equal(DevelopmentControlLockOutcome.Acquired, attempt.Outcome);
            Assert.NotNull(attempt.Lock);
            Assert.True(attempt.Lock!.IsHeld);
            attempt.Lock.Release();
            Assert.False(attempt.Lock.IsHeld);
            attempt.Lock.Dispose();
            return;
        }

        // Non-Windows (Linux CI): named semaphores do not exist; the attempt is a controlled
        // SystemFailure with no lock and no exception.
        Assert.Equal(DevelopmentControlLockOutcome.SystemFailure, attempt.Outcome);
        Assert.Null(attempt.Lock);
    }

    // Contention/timeout semantics exist only where the named semaphore primitive exists, so
    // this test runs on Windows only. Off Windows the acquire fails closed immediately
    // (SystemFailure), asserted deterministically by the acquire/release and factory tests;
    // nothing is backgrounded here, so no unsupported-platform exception can escape a thread
    // and abort the test host.
    [Fact]
    public void SemaphoreMutex_TryAcquire_TimesOutWhenAnotherThreadHoldsTheSameIdentity()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

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

    // The factory's Kind is platform-independent; the acquire outcome it reports follows the
    // platform (Windows: acquired; non-Windows: controlled SystemFailure for the unsupported
    // named semaphore). Asserted on every platform so factory/platform behavior is explicit.
    [Fact]
    public void SemaphoreFactory_ExposesItsKind()
    {
        var factory = new SemaphoreDevelopmentControlWriteLockFactory();

        Assert.Equal("named-semaphore", factory.Kind);
        var attempt = factory.TryAcquire(NewIdentity(), TimeSpan.FromSeconds(5));
        if (OperatingSystem.IsWindows())
        {
            Assert.Equal(DevelopmentControlLockOutcome.Acquired, attempt.Outcome);
            attempt.Lock!.Dispose();
        }
        else
        {
            Assert.Equal(DevelopmentControlLockOutcome.SystemFailure, attempt.Outcome);
            Assert.Null(attempt.Lock);
        }
    }

    // Acquires the supplied lock on a dedicated background thread (satisfying Mutex thread
    // affinity) and holds it until Release() is called. Entered is set once the lock is held
    // (or the acquire attempt finished) so the test can observe the outcome.
    //
    // A background-thread exception is deliberately NOT allowed to escape Run(): in .NET an
    // unhandled exception on a thread terminates the whole process, which would abort the test
    // host. Any acquire failure is instead captured and re-thrown from Release() on the TEST
    // thread, so it fails exactly this test while the host (and the rest of the suite) survive.
    private sealed class BackgroundLockHolder
    {
        private readonly Func<DevelopmentControlLockAttempt> _acquire;
        private readonly ManualResetEventSlim _entered = new();
        private readonly ManualResetEventSlim _release = new();
        private readonly Thread _thread;
        private DevelopmentControlLockAttempt _attempt = null!;
        private Exception? _acquireFailure;

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
            if (_acquireFailure is not null)
            {
                // Surface the failure on the test thread so the host survives. An unhandled
                // exception on the holder thread would abort the entire test run.
                throw new Xunit.Sdk.XunitException(
                    "Background lock acquire threw unexpectedly: " + _acquireFailure);
            }
        }

        private void Run()
        {
            try
            {
                _attempt = _acquire();
            }
            catch (Exception ex)
            {
                _acquireFailure = ex;
            }
            finally
            {
                _entered.Set(); // signal even on failure so the test can observe and Release()
            }

            _release.Wait(TimeSpan.FromSeconds(30));
            _attempt?.Lock?.Dispose();
        }
    }
}
