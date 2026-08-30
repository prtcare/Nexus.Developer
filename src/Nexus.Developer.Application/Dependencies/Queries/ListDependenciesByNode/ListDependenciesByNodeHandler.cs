using Nexus.Developer.Core.Dependencies;

namespace Nexus.Developer.Application.Dependencies.Queries.ListDependenciesByNode;

public sealed class ListDependenciesByNodeHandler
{
    private readonly IWorkItemDependencyRepository _repository;

    public ListDependenciesByNodeHandler(IWorkItemDependencyRepository repository)
    {
        _repository = repository;
    }

    public async Task<ListDependenciesByNodeResult> HandleAsync(
        ListDependenciesByNodeQuery query,
        CancellationToken cancellationToken = default)
    {
        var dependencies = await _repository.ListByNodeAsync(query.NodeType, query.NodeId, cancellationToken);

        var results = dependencies
            .Select(dependency => new WorkItemDependencyResult(
                dependency.Id,
                dependency.UpstreamType,
                dependency.UpstreamId,
                dependency.DownstreamType,
                dependency.DownstreamId,
                dependency.Kind,
                dependency.RequiredState,
                dependency.CreatedByUserId,
                dependency.CreatedAt))
            .ToList();

        return new ListDependenciesByNodeResult(results);
    }
}
