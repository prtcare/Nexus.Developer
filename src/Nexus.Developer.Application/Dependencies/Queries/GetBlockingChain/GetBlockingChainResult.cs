using Nexus.Developer.Core.Dependencies;

namespace Nexus.Developer.Application.Dependencies.Queries.GetBlockingChain;

// The transitive blocking chain, nearest predecessor first. The seam WI-07-6.1.1
// will call into later; only this query is built now, not the bridge itself.
public sealed record GetBlockingChainResult(
    IReadOnlyList<(WorkItemDependencyNodeType Type, Guid Id)> Chain);
