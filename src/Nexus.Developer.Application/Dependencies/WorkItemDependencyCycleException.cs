using Nexus.Developer.Core.Dependencies;

namespace Nexus.Developer.Application.Dependencies;

// Thrown by CreateWorkItemDependencyHandler when adding a Blocking edge would
// close a dependency cycle (WI-07-2.1.1). Carries the full cycle path, with the
// human-readable description naming it -- the path returned by
// DependencyGraphTraversal.FindCycleIfAdded includes the closing repeat, so the
// description reads e.g. "Task:B -> Task:C -> Task:A -> Task:B". Type-qualified ids
// keep the path unambiguous when two different node types share a Guid (which the
// entity explicitly permits).
public sealed class WorkItemDependencyCycleException : Exception
{
    public WorkItemDependencyCycleException(
        IReadOnlyList<(WorkItemDependencyNodeType Type, Guid Id)> path)
        : base($"Adding this dependency would create a cycle: {Describe(path)}")
    {
        Path = path;
    }

    public IReadOnlyList<(WorkItemDependencyNodeType Type, Guid Id)> Path { get; }

    private static string Describe(IReadOnlyList<(WorkItemDependencyNodeType Type, Guid Id)> path)
        => string.Join(" -> ", path.Select(node => $"{node.Type}:{node.Id}"));
}
