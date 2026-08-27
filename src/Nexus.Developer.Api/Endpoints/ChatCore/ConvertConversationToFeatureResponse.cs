namespace Nexus.Developer.Api.Endpoints.ChatCore;

public sealed record ConvertConversationToFeatureResponse(
    Guid FeatureId,
    string FeatureReference,
    string Title,
    Guid ObjectChatLinkId,
    Guid ConversationId);
