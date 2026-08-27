using Nexus.Developer.Core.ObjectChatLinks;

namespace Nexus.Developer.Application.ObjectChatLinks;

// Thrown by CreateObjectChatLinkHandler when the target object the link points at
// cannot be resolved (M-07-10.4): the endpoint maps this to 404 Not Found rather
// than letting it surface as an unhandled 500.
public sealed class ObjectChatLinkTargetNotFoundException : Exception
{
    public ObjectChatLinkTargetNotFoundException(
        ObjectChatLinkTargetType targetType,
        Guid targetId)
        : base($"The {targetType} target '{targetId}' does not exist, so the chat link cannot be created.")
    {
        TargetType = targetType;
        TargetId = targetId;
    }

    public ObjectChatLinkTargetType TargetType { get; }

    public Guid TargetId { get; }
}
