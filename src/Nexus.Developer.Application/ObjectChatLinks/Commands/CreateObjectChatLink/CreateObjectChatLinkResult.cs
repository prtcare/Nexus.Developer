using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.ObjectChatLinks;

namespace Nexus.Developer.Application.ObjectChatLinks.Commands.CreateObjectChatLink;

public sealed record CreateObjectChatLinkResult(
    ObjectChatLinkId ObjectChatLinkId,
    Guid ConversationId,
    ObjectChatLinkTargetType TargetType,
    Guid TargetId,
    DateTimeOffset LinkedAt);
