using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Developer.Core.DevelopmentControl;
using Nexus.Developer.Infrastructure.DevelopmentControl;

namespace Nexus.Developer.Infrastructure;

// SP1-M05 (Lane A): the composition-root binding that makes the Development Control
// plane runnable through the real host. Before this, no DevelopmentControl type was
// registered anywhere: Core shipped the contracts and the concurrency/atomic-write
// layer (SP1-M00), Infrastructure shipped the Excel adapter and schema validator
// (WI-07-0.2.3), but no host wired them together. This extension reads a
// "DevelopmentControl:WorkbookPath" configuration key and registers the full governed
// stack for host lifetime (singleton):
//
//   ExcelDevelopmentControlStore (inner, over the canonical path)
//     -> IConcurrencyGuardedDevelopmentControlStore   (the guarded decorator, SP1-M00)
//        -> IDevelopmentControlStore                   (guarded store = the public read/write
//                                                       surface every Application handler uses)
//   NamedDevelopmentControlWriteLockFactory            -> IDevelopmentControlWriteLockFactory
//   DevelopmentControlMutexIdentity.FromWorkbookPath   -> deterministic cross-process lock name
//   DevelopmentControlAtomicWriteCoordinator           -> IDevelopmentControlAtomicWriteCoordinator
//
// The named-object lock the guarded store/coordinator acquire is derived from the
// workbook path via DevelopmentControlMutexIdentity, so any process that binds the same
// canonical path contends on the SAME kernel object (mutex name prefix
// "NexusDevelopmentControl_" + SHA-256). That is the cross-process writer-lock contract
// a later Forge <-> Developer proof will rely on: both processes configure the same
// "DevelopmentControl:WorkbookPath" and the same lock identity is derived.
//
// Registration is lazy (AddSingleton factories), so a missing workbook path only
// surfaces when a store/coordinator is first resolved -- never at host startup.
public static class DevelopmentControlServiceCollectionExtensions
{
    public const string ConfigurationSectionName = "DevelopmentControl";
    public const string WorkbookPathKey = "WorkbookPath";
    public const string LockTimeoutSecondsKey = "LockTimeoutSeconds";

    public const int DefaultLockTimeoutSeconds = 10;

    // Reads "DevelopmentControl:WorkbookPath" (+ optional "LockTimeoutSeconds") from the
    // supplied configuration. A relative WorkbookPath is resolved against contentRootPath
    // (the host's content root) when supplied, otherwise the current working directory.
    public static IServiceCollection AddDevelopmentControl(
        this IServiceCollection services,
        IConfiguration configuration,
        string? contentRootPath = null)
    {
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        var workbookPath = configuration.GetSection(ConfigurationSectionName)[WorkbookPathKey]
            ?? throw new InvalidOperationException(
                $"Configuration key '{ConfigurationSectionName}:{WorkbookPathKey}' is required to bind the Development Control store.");

        if (!Path.IsPathRooted(workbookPath))
        {
            workbookPath = Path.GetFullPath(Path.Combine(
                contentRootPath ?? Directory.GetCurrentDirectory(), workbookPath));
        }

        var lockTimeoutSeconds = DefaultLockTimeoutSeconds;
        var rawTimeout = configuration.GetSection(ConfigurationSectionName)[LockTimeoutSecondsKey];
        if (!string.IsNullOrWhiteSpace(rawTimeout)
            && !int.TryParse(rawTimeout, out lockTimeoutSeconds))
        {
            throw new InvalidOperationException(
                $"Configuration key '{ConfigurationSectionName}:{LockTimeoutSecondsKey}' must be an integer number of seconds.");
        }

        return services.AddDevelopmentControl(workbookPath, TimeSpan.FromSeconds(lockTimeoutSeconds));
    }

    // Direct overload (no IConfiguration): registers the governed stack over an explicit
    // workbook path with an explicit bounded lock timeout. Used by tests and by hosts that
    // already know the canonical path.
    public static IServiceCollection AddDevelopmentControl(
        this IServiceCollection services,
        string workbookPath,
        TimeSpan? lockTimeout = null)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (string.IsNullOrWhiteSpace(workbookPath))
            throw new ArgumentException("A workbook path is required.", nameof(workbookPath));

        var timeout = lockTimeout ?? TimeSpan.FromSeconds(DefaultLockTimeoutSeconds);
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lockTimeout),
                "The Development Control lock timeout must be greater than zero.");

        var fullPath = Path.GetFullPath(workbookPath);

        // The one real Excel adapter over the canonical workbook. Lazy factory: the file
        // must exist when the store is first resolved, not when the host starts.
        services.AddSingleton(_ => new ExcelDevelopmentControlStore(fullPath));

        // SP1-M00 concrete named-mutex factory (Core). Kind = "named-mutex".
        services.AddSingleton<IDevelopmentControlWriteLockFactory, NamedDevelopmentControlWriteLockFactory>();

        // The deterministic cross-process lock identity, derived from the canonical path.
        // Registered as a singleton instance so the guard, coordinator, and any caller that
        // needs to build an AtomicWriteRequest resolve the SAME identity.
        services.AddSingleton(DevelopmentControlMutexIdentity.FromWorkbookPath(fullPath));

        // The guarded decorator: every mutating op runs inside the named cross-process lock,
        // with verify-while-locked and a bounded lock wait surfaced as a controlled result.
        services.AddSingleton<IConcurrencyGuardedDevelopmentControlStore>(sp =>
        {
            var inner = sp.GetRequiredService<ExcelDevelopmentControlStore>();
            var lockFactory = sp.GetRequiredService<IDevelopmentControlWriteLockFactory>();
            var identity = sp.GetRequiredService<DevelopmentControlMutexIdentity>();
            return new ConcurrencyGuardedDevelopmentControlStore(inner, lockFactory, identity, timeout);
        });

        // The guarded store is the public IDevelopmentControlStore: reads pass through and
        // writes are governed. Handlers and endpoints resolve this and never the raw inner.
        services.AddSingleton<IDevelopmentControlStore>(sp =>
            sp.GetRequiredService<IConcurrencyGuardedDevelopmentControlStore>());

        // The atomic-write coordinator (SP1-M00) bound over the inner runner store + the same
        // lock factory: a host caller can issue an AtomicWriteRequest and get an
        // AtomicWriteResult<T> with the full DevelopmentControlConcurrencyOutcome.
        services.AddSingleton<IDevelopmentControlAtomicWriteCoordinator>(sp =>
            new DevelopmentControlAtomicWriteCoordinator(
                sp.GetRequiredService<ExcelDevelopmentControlStore>(),
                sp.GetRequiredService<IDevelopmentControlWriteLockFactory>()));

        return services;
    }
}
