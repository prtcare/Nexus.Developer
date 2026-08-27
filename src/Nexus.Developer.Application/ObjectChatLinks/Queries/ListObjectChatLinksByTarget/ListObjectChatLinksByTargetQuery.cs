using Nexus.Developer.Core.ObjectChatLinks;

namespace Nexus.Developer.Application.ObjectChatLinks.Queries.ListObjectChatLinksByTarget;

public sealed record ListObjectChatLinksByTargetQuery(
    ObjectChatLinkTargetType TargetType,
    Guid TargetId);
