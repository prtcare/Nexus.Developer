namespace Nexus.Developer.Core.Issue;

// The universal-attachment surface: Workspace/Project/Subproject are foreign
// (Product Core) references; Feature/Milestone/Task/Subtask are Developer's own;
// Chat is the Chat Core's Conversation; DevelopmentRun is Developer's own
// placeholder aggregate. All nine are represented the same way -- a tagged Guid --
// because Issue must attach to any of them without importing their types.
public enum IssueLinkTargetType
{
    Workspace = 1,
    Project = 2,
    Subproject = 3,
    Feature = 4,
    Milestone = 5,
    Task = 6,
    Subtask = 7,
    Chat = 8,
    DevelopmentRun = 9
}
