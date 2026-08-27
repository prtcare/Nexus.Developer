namespace Nexus.Developer.Application.ObjectChatLinks.Queries.ListObjectChatLinksByConversation;

public sealed record ListObjectChatLinksByConversationResult(
    IReadOnlyList<ObjectChatLinkResult> Links);
