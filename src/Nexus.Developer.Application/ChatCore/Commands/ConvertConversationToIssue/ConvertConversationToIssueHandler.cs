using Nexus.Developer.Application.Issues.Commands.CreateIssue;
using Nexus.Developer.Application.ObjectChatLinks.Commands.CreateObjectChatLink;
using Nexus.Developer.Core.ChatCore;
using Nexus.Developer.Core.ObjectChatLinks;

namespace Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToIssue;

// Second M-12-0.1 backend slice: the ConvertConversationToFeature composition
// pattern extended to Issue. Issue has no parent id, so the CreateIssue step
// takes only the conversation's title/description; otherwise the same flow and
// the same known limitation -- if the ObjectChatLink step fails after the Issue
// was persisted, the exception propagates as-is and the Issue is intentionally
// left in place, unlinked.
public sealed class ConvertConversationToIssueHandler
{
    private readonly IChatCoreClient _chatCoreClient;
    private readonly CreateIssueHandler _createIssueHandler;
    private readonly CreateObjectChatLinkHandler _createObjectChatLinkHandler;

    public ConvertConversationToIssueHandler(
        IChatCoreClient chatCoreClient,
        CreateIssueHandler createIssueHandler,
        CreateObjectChatLinkHandler createObjectChatLinkHandler)
    {
        _chatCoreClient = chatCoreClient;
        _createIssueHandler = createIssueHandler;
        _createObjectChatLinkHandler = createObjectChatLinkHandler;
    }

    public async Task<ConvertConversationToIssueResult> HandleAsync(
        ConvertConversationToIssueCommand command,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _chatCoreClient.GetConversationAsync(
            command.ConversationId,
            cancellationToken);

        if (conversation is null)
        {
            throw new ConversationNotFoundException(command.ConversationId);
        }

        var issue = await _createIssueHandler.HandleAsync(
            new CreateIssueCommand(
                command.Title,
                command.Description ?? string.Empty,
                command.CreatedByUserId),
            cancellationToken);

        // If this step throws after the Issue was persisted above, the exception
        // propagates as-is and the Issue is intentionally left in place,
        // unlinked -- a known limitation of this slice (M-12-0.1), not something
        // to paper over with a compensating delete.
        var link = await _createObjectChatLinkHandler.HandleAsync(
            new CreateObjectChatLinkCommand(
                command.ConversationId,
                command.MessageRangeStart,
                command.MessageRangeEnd,
                ObjectChatLinkTargetType.Issue,
                issue.IssueId.Value,
                command.CreatedByUserId),
            cancellationToken);

        return new ConvertConversationToIssueResult(
            issue.IssueId,
            issue.Reference,
            issue.Title,
            link.ObjectChatLinkId,
            command.ConversationId);
    }
}
