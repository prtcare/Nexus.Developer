using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToSubtask;

public sealed record ConvertConversationToSubtaskResult(
    SubtaskId SubtaskId,
    string SubtaskReference,
    string Title,
    ObjectChatLinkId ObjectChatLinkId,
    Guid ConversationId);
