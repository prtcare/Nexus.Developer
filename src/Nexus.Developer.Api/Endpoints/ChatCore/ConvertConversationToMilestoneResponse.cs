namespace Nexus.Developer.Api.Endpoints.ChatCore;

public sealed record ConvertConversationToMilestoneResponse(
    Guid MilestoneId,
    string MilestoneReference,
    string Name,
    Guid ObjectChatLinkId,
    Guid ConversationId);
