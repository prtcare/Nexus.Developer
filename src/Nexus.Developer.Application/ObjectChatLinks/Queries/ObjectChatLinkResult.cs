using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.ObjectChatLinks;

namespace Nexus.Developer.Application.ObjectChatLinks.Queries;

// Shared item shape for both ObjectChatLink list queries, mirroring how
// GetFeatureResult is reused by ListFeaturesBySubproject.
public sealed record ObjectChatLinkResult(
    ObjectChatLinkId ObjectChatLinkId,
    Guid ConversationId,
    Guid? MessageRangeStart,
    Guid? MessageRangeEnd,
    ObjectChatLinkTargetType TargetType,
    Guid TargetId,
    Guid LinkedByUserId,
    DateTimeOffset LinkedAt);
