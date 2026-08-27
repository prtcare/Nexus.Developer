namespace Nexus.Developer.Api.Endpoints.ObjectChatLinks;

public sealed record CreateObjectChatLinkResponse(
    Guid ObjectChatLinkId,
    Guid ConversationId,
    string TargetType,
    Guid TargetId,
    DateTimeOffset LinkedAt);
