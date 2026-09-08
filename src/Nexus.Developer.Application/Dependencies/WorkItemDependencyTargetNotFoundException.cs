using Nexus.Developer.Core.Dependencies;

namespace Nexus.Developer.Application.Dependencies;

// Thrown by CreateWorkItemDependencyHandler when either endpoint of the edge does
// not resolve (WI-07-2.1.1). Names which side was invalid so the create endpoint
// can return 400 (invalid caller-supplied input on a create call), never an
// unhandled 500.
public sealed class WorkItemDependencyTargetNotFoundException : Exception
{
    public WorkItemDependencyTargetNotFoundException(
        string side,
        WorkItemDependencyNodeType nodeType,
        Guid nodeId)
        : base($"The {side} {nodeType} '{nodeId}' does not exist.")
    {
        Side = side;
        NodeType = nodeType;
        NodeId = nodeId;
    }

    public string Side { get; }

    public WorkItemDependencyNodeType NodeType { get; }

    public Guid NodeId { get; }
}
