using Nexus.Developer.Core.DevelopmentControl;

namespace Nexus.Developer.Application.DevelopmentControl.Queries.GetActiveDevelopmentChanges;

// SP1-M05: returns the open Active Changes register (open = not Completed/Cancelled).
public sealed class GetActiveDevelopmentChangesHandler
{
    private readonly IConcurrencyGuardedDevelopmentControlStore _store;

    public GetActiveDevelopmentChangesHandler(IConcurrencyGuardedDevelopmentControlStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyList<ActiveChange>> HandleAsync(
        GetActiveDevelopmentChangesQuery query,
        CancellationToken cancellationToken = default)
        => _store.GetActiveChangesAsync(cancellationToken);
}
