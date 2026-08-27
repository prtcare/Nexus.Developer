using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToTask;

public sealed record ConvertConversationToTaskResult(
    TaskId TaskId,
    string TaskReference,
    string Title,
    ObjectChatLinkId ObjectChatLinkId,
    Guid ConversationId);
