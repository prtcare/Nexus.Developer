using Nexus.Developer.Core.DevelopmentControl;

namespace Nexus.Developer.Application.DevelopmentControl.Queries.GetDevelopmentControlNode;

// SP1-M05: returns the current version of one node (IsDeleted nodes are hidden), or
// null when the id is unknown. A malformed node id (blank/whitespace) throws
// ArgumentException from NodeId and is mapped to a 400 by the endpoint.
public sealed class GetDevelopmentControlNodeHandler
{
    private readonly IConcurrencyGuardedDevelopmentControlStore _store;

    public GetDevelopmentControlNodeHandler(IConcurrencyGuardedDevelopmentControlStore store)
    {
        _store = store;
    }

    public Task<Node?> HandleAsync(
        GetDevelopmentControlNodeQuery query,
        CancellationToken cancellationToken = default)
        => _store.GetNodeAsync(new NodeId(query.NodeId), cancellationToken);
}
