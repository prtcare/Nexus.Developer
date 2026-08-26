using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.ObjectChatLinks;

// The permanent many-to-many relationship between a Developer Chat conversation
// (or a message range within it) and the development object(s) it produced. A real
// row, independent of either side being edited (M-07-10.4 acceptance) -- not the
// temporary UI-only link the "Developer Chat principle" explicitly warns against.
// ConversationId/message ids are plain Guids: Nexus.Developer does not import the
// Chat Core's domain assembly (AGENTS.md Boundary rules), it only holds the ids the
// Chat API already returns.
public sealed class ObjectChatLink
{
    public ObjectChatLink(
        ObjectChatLinkId id,
        Guid conversationId,
        Guid? messageRangeStart,
        Guid? messageRangeEnd,
        ObjectChatLinkTargetType targetType,
        Guid targetId,
        Guid linkedByUserId,
        DateTimeOffset linkedAt)
    {
        if (!Enum.IsDefined(targetType))
        {
            throw new ArgumentOutOfRangeException(nameof(targetType));
        }

        Id = id;
        ConversationId = conversationId;
        MessageRangeStart = messageRangeStart;
        MessageRangeEnd = messageRangeEnd;
        TargetType = targetType;
        TargetId = targetId;
        LinkedByUserId = linkedByUserId;
        LinkedAt = linkedAt;
    }

    public ObjectChatLinkId Id { get; }

    public Guid ConversationId { get; }

    // Optional provenance: which messages in the conversation the object was
    // actually drawn from. Null when the whole conversation is the context.
    public Guid? MessageRangeStart { get; }

    public Guid? MessageRangeEnd { get; }

    public ObjectChatLinkTargetType TargetType { get; }

    public Guid TargetId { get; }

    public Guid LinkedByUserId { get; }

    public DateTimeOffset LinkedAt { get; }
}
