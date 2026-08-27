namespace Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToMilestone;

public sealed record ConvertConversationToMilestoneCommand(
    Guid ConversationId,
    Guid SubprojectId,
    string Name,
    string? Description,
    DateTimeOffset? TargetDate,
    Guid CreatedByUserId,
    Guid? MessageRangeStart,
    Guid? MessageRangeEnd);
