namespace Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToSubtask;

public sealed record ConvertConversationToSubtaskCommand(
    Guid ConversationId,
    Guid TaskId,
    string Title,
    string? Description,
    Guid CreatedByUserId,
    Guid? MessageRangeStart,
    Guid? MessageRangeEnd);
