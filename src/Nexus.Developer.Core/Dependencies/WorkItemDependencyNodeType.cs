namespace Nexus.Developer.Core.Dependencies;

// The five work-graph object types a dependency edge may connect (M-07-2.1).
// Deliberately a separate enum from ObjectChatLinkTargetType despite identical
// values -- each bounded concept owns its own type enum, following this repo's
// precedent of one enum per concept (DevelopmentRunTargetType is likewise separate
// from ObjectChatLinkTargetType).
public enum WorkItemDependencyNodeType
{
    Feature = 1,
    Task = 2,
    Subtask = 3,
    Milestone = 4,
    Issue = 5
}
