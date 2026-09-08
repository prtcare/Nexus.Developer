using Nexus.Developer.Application.Dependencies.Queries;

namespace Nexus.Developer.Application.Dependencies.Queries.ListDependenciesByNode;

public sealed record ListDependenciesByNodeResult(
    IReadOnlyList<WorkItemDependencyResult> Dependencies);
