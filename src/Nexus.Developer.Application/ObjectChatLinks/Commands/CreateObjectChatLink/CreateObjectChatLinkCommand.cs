using Nexus.Developer.Core.ObjectChatLinks;

namespace Nexus.Developer.Application.ObjectChatLinks.Commands.CreateObjectChatLink;

public sealed record CreateObjectChatLinkCommand(
    Guid ConversationId,
    Guid? MessageRangeStart,
    Guid? MessageRangeEnd,
    ObjectChatLinkTargetType TargetType,
    Guid TargetId,
    Guid LinkedByUserId);
