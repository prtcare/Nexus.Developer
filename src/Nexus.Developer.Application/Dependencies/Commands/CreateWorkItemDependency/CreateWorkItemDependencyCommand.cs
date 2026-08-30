using Nexus.Developer.Core.Dependencies;

namespace Nexus.Developer.Application.Dependencies.Commands.CreateWorkItemDependency;

public sealed record CreateWorkItemDependencyCommand(
    WorkItemDependencyNodeType UpstreamType,
    Guid UpstreamId,
    WorkItemDependencyNodeType DownstreamType,
    Guid DownstreamId,
    WorkItemDependencyKind Kind,
    WorkItemDependencyRequiredState? RequiredState,
    Guid CreatedByUserId);
