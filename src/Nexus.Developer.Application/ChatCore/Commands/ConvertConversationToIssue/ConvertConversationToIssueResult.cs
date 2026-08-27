using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToIssue;

public sealed record ConvertConversationToIssueResult(
    IssueId IssueId,
    string IssueReference,
    string Title,
    ObjectChatLinkId ObjectChatLinkId,
    Guid ConversationId);
