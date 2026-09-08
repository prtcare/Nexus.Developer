using Nexus.Developer.Core.Dependencies;

namespace Nexus.Developer.Application.Dependencies.Queries.GetBlockingChain;

public sealed class GetBlockingChainHandler
{
    private readonly IWorkItemDependencyRepository _repository;

    public GetBlockingChainHandler(IWorkItemDependencyRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetBlockingChainResult> HandleAsync(
        GetBlockingChainQuery query,
        CancellationToken cancellationToken = default)
    {
        var blockingEdges = await _repository.ListAllBlockingAsync(cancellationToken);

        var chain = DependencyGraphTraversal.GetBlockingChain(
            blockingEdges,
            query.NodeType,
            query.NodeId);

        return new GetBlockingChainResult(chain);
    }
}
