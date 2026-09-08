using Nexus.Developer.Core.DevelopmentControl;

namespace Nexus.Developer.Application.DevelopmentControl.Queries.GetDevelopmentControlState;

// SP1-M05: returns the workbook's ControlState read model, or null when the store has
// no content yet. Reads pass through the guarded store unguarded (a writer lock guards
// writes; reads observe a consistent whole file from the atomic replace).
public sealed class GetDevelopmentControlStateHandler
{
    private readonly IConcurrencyGuardedDevelopmentControlStore _store;

    public GetDevelopmentControlStateHandler(IConcurrencyGuardedDevelopmentControlStore store)
    {
        _store = store;
    }

    public Task<ControlState?> HandleAsync(
        GetDevelopmentControlStateQuery query,
        CancellationToken cancellationToken = default)
        => _store.GetControlStateAsync(cancellationToken);
}
