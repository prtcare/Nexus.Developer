namespace Nexus.Developer.Api.Endpoints.ObjectChatLinks;

public sealed record CreateObjectChatLinkRequest(
    Guid ConversationId,
    Guid? MessageRangeStart,
    Guid? MessageRangeEnd,
    string TargetType,
    Guid TargetId,
    Guid LinkedByUserId);
