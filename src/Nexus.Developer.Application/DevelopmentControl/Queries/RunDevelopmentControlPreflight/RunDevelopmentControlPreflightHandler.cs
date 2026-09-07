using Nexus.Developer.Core.DevelopmentControl;

namespace Nexus.Developer.Application.DevelopmentControl.Queries.RunDevelopmentControlPreflight;

// SP1-M05: runs the preflight declaration through the guarded store's read-only check
// and returns the single verdict plus named findings.
public sealed class RunDevelopmentControlPreflightHandler
{
    private readonly IConcurrencyGuardedDevelopmentControlStore _store;

    public RunDevelopmentControlPreflightHandler(IConcurrencyGuardedDevelopmentControlStore store)
    {
        _store = store;
    }

    public Task<PreflightResult> HandleAsync(
        RunDevelopmentControlPreflightQuery query,
        CancellationToken cancellationToken = default)
        => _store.RunPreflightAsync(query.Declaration, cancellationToken);
}
