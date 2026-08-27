namespace Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToIssue;

// Issue is universally attachable (no parent id of its own), so unlike the
// Task/Subtask convert commands this one carries no parent -- the converted
// Issue gets its positioning exclusively from the ObjectChatLink, mirroring the
// Issue aggregate itself.
public sealed record ConvertConversationToIssueCommand(
    Guid ConversationId,
    string Title,
    string? Description,
    Guid CreatedByUserId,
    Guid? MessageRangeStart,
    Guid? MessageRangeEnd);
