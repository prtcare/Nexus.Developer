using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Dependencies;

namespace Nexus.Developer.Application.Dependencies.Queries;

// Shared item shape for the ListDependenciesByNode query, mirroring how
// ObjectChatLinkResult is shared across the ObjectChatLink list queries.
public sealed record WorkItemDependencyResult(
    WorkItemDependencyId WorkItemDependencyId,
    WorkItemDependencyNodeType UpstreamType,
    Guid UpstreamId,
    WorkItemDependencyNodeType DownstreamType,
    Guid DownstreamId,
    WorkItemDependencyKind Kind,
    WorkItemDependencyRequiredState? RequiredState,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string? Reason);
