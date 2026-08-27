namespace Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToTask;

public sealed record ConvertConversationToTaskCommand(
    Guid ConversationId,
    Guid FeatureId,
    string Title,
    string? Description,
    Guid CreatedByUserId,
    Guid? MessageRangeStart,
    Guid? MessageRangeEnd);
