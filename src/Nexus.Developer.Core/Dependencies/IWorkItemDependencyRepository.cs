namespace Nexus.Developer.Core.Dependencies;

public interface IWorkItemDependencyRepository
{
    Task AddAsync(
        WorkItemDependency dependency,
        CancellationToken cancellationToken = default);

    // Every Kind=Blocking edge. The traversal/cycle-detection logic needs the whole
    // blocking subgraph in memory to walk it; for this milestone's V1 scope loading
    // it fully on each write/query is acceptable -- no pagination/indexing
    // optimization yet, this repo has no data volume to justify one. Revisit if
    // this ever becomes a real bottleneck (WI-07-2.1.1).
    Task<IReadOnlyList<WorkItemDependency>> ListAllBlockingAsync(
        CancellationToken cancellationToken = default);

    // Every edge (any kind) where the given node is either the upstream or the
    // downstream side. Powers "what does this node's dependency state look like" --
    // exactly what WI-07-6.1.1 will call into later.
    Task<IReadOnlyList<WorkItemDependency>> ListByNodeAsync(
        WorkItemDependencyNodeType nodeType,
        Guid nodeId,
        CancellationToken cancellationToken = default);
}
