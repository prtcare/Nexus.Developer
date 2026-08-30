using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.Dependencies;

// A directed dependency edge between two work-graph nodes (Feature/Task/Subtask/
// Milestone/Issue). Direction: upstream precedes/blocks downstream. For
// Kind=Blocking, downstream cannot proceed until upstream satisfies requiredState
// (or reaches full completion when requiredState is null). An edge, not a top-level
// work object -- like ObjectChatLink, it carries no Reference/Ref.
public sealed class WorkItemDependency
{
    public WorkItemDependency(
        WorkItemDependencyId id,
        WorkItemDependencyNodeType upstreamType,
        Guid upstreamId,
        WorkItemDependencyNodeType downstreamType,
        Guid downstreamId,
        WorkItemDependencyKind kind,
        WorkItemDependencyRequiredState? requiredState,
        Guid createdByUserId,
        DateTimeOffset createdAt)
    {
        if (!Enum.IsDefined(upstreamType))
        {
            throw new ArgumentOutOfRangeException(nameof(upstreamType));
        }

        if (!Enum.IsDefined(downstreamType))
        {
            throw new ArgumentOutOfRangeException(nameof(downstreamType));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (requiredState.HasValue && !Enum.IsDefined(requiredState.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(requiredState));
        }

        if (upstreamType == downstreamType && upstreamId == downstreamId)
        {
            throw new ArgumentException(
                "A work item cannot depend on itself; upstream and downstream must be different nodes.",
                nameof(downstreamId));
        }

        if (kind != WorkItemDependencyKind.Blocking && requiredState.HasValue)
        {
            throw new ArgumentException(
                "RequiredState applies only to Blocking dependencies; Parallel and Informational edges carry no required-state concept.",
                nameof(requiredState));
        }

        Id = id;
        UpstreamType = upstreamType;
        UpstreamId = upstreamId;
        DownstreamType = downstreamType;
        DownstreamId = downstreamId;
        Kind = kind;
        RequiredState = requiredState;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
    }

    public WorkItemDependencyId Id { get; }

    public WorkItemDependencyNodeType UpstreamType { get; }

    public Guid UpstreamId { get; }

    public WorkItemDependencyNodeType DownstreamType { get; }

    public Guid DownstreamId { get; }

    public WorkItemDependencyKind Kind { get; }

    public WorkItemDependencyRequiredState? RequiredState { get; }

    public Guid CreatedByUserId { get; }

    public DateTimeOffset CreatedAt { get; }
}
