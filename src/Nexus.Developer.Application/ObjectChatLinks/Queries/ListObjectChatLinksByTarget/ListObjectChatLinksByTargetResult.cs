namespace Nexus.Developer.Application.ObjectChatLinks.Queries.ListObjectChatLinksByTarget;

public sealed record ListObjectChatLinksByTargetResult(
    IReadOnlyList<ObjectChatLinkResult> Links);
