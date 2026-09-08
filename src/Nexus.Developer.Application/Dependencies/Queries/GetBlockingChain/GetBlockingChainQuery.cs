using Nexus.Developer.Core.Dependencies;

namespace Nexus.Developer.Application.Dependencies.Queries.GetBlockingChain;

public sealed record GetBlockingChainQuery(
    WorkItemDependencyNodeType NodeType,
    Guid NodeId);
