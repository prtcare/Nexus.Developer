namespace Nexus.Developer.Core.ObjectChatLink;

// A Chat produces development objects, not the other way round -- so the target
// side only spans the objects a discussion can turn into (M-07-10.4 acceptance:
// "A Feature/Issue/Milestone/Task/Subtask may link to more than one Chat").
public enum ObjectChatLinkTargetType
{
    Feature = 1,
    Task = 2,
    Subtask = 3,
    Milestone = 4,
    Issue = 5
}
