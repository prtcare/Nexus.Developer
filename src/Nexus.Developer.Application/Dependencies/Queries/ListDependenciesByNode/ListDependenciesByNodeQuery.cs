using Nexus.Developer.Core.Dependencies;

namespace Nexus.Developer.Application.Dependencies.Queries.ListDependenciesByNode;

public sealed record ListDependenciesByNodeQuery(
    WorkItemDependencyNodeType NodeType,
    Guid NodeId);
