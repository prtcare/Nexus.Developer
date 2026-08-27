using Nexus.Developer.Application.ObjectChatLinks.Commands.CreateObjectChatLink;
using Nexus.Developer.Application.Subtasks.Commands.CreateSubtask;
using Nexus.Developer.Core.ChatCore;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.ObjectChatLinks;

namespace Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToSubtask;

// Second M-12-0.1 backend slice: the ConvertConversationToFeature composition
// pattern extended to Subtask. Same flow and the same known limitation -- if the
// ObjectChatLink step fails after the Subtask was persisted, the exception
// propagates as-is and the Subtask is intentionally left in place, unlinked.
public sealed class ConvertConversationToSubtaskHandler
{
    private readonly IChatCoreClient _chatCoreClient;
    private readonly CreateSubtaskHandler _createSubtaskHandler;
    private readonly CreateObjectChatLinkHandler _createObjectChatLinkHandler;

    public ConvertConversationToSubtaskHandler(
        IChatCoreClient chatCoreClient,
        CreateSubtaskHandler createSubtaskHandler,
        CreateObjectChatLinkHandler createObjectChatLinkHandler)
    {
        _chatCoreClient = chatCoreClient;
        _createSubtaskHandler = createSubtaskHandler;
        _createObjectChatLinkHandler = createObjectChatLinkHandler;
    }

    public async Task<ConvertConversationToSubtaskResult> HandleAsync(
        ConvertConversationToSubtaskCommand command,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _chatCoreClient.GetConversationAsync(
            command.ConversationId,
            cancellationToken);

        if (conversation is null)
        {
            throw new ConversationNotFoundException(command.ConversationId);
        }

        var subtask = await _createSubtaskHandler.HandleAsync(
            new CreateSubtaskCommand(
                new TaskId(command.TaskId),
                command.Title,
                command.Description ?? string.Empty,
                command.CreatedByUserId),
            cancellationToken);

        // If this step throws after the Subtask was persisted above, the exception
        // propagates as-is and the Subtask is intentionally left in place,
        // unlinked -- a known limitation of this slice (M-12-0.1), not something
        // to paper over with a compensating delete.
        var link = await _createObjectChatLinkHandler.HandleAsync(
            new CreateObjectChatLinkCommand(
                command.ConversationId,
                command.MessageRangeStart,
                command.MessageRangeEnd,
                ObjectChatLinkTargetType.Subtask,
                subtask.SubtaskId.Value,
                command.CreatedByUserId),
            cancellationToken);

        return new ConvertConversationToSubtaskResult(
            subtask.SubtaskId,
            subtask.Reference,
            subtask.Title,
            link.ObjectChatLinkId,
            command.ConversationId);
    }
}
