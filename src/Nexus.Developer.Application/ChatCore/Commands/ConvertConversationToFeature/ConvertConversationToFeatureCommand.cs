namespace Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToFeature;

public sealed record ConvertConversationToFeatureCommand(
    Guid ConversationId,
    Guid SubprojectId,
    string Title,
    string? Description,
    Guid CreatedByUserId,
    Guid? MessageRangeStart,
    Guid? MessageRangeEnd);
