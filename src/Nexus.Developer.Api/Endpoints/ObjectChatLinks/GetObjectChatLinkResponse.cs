namespace Nexus.Developer.Api.Endpoints.ObjectChatLinks;

public sealed record GetObjectChatLinkResponse(
    Guid ObjectChatLinkId,
    Guid ConversationId,
    Guid? MessageRangeStart,
    Guid? MessageRangeEnd,
    string TargetType,
    Guid TargetId,
    Guid LinkedByUserId,
    DateTimeOffset LinkedAt);
