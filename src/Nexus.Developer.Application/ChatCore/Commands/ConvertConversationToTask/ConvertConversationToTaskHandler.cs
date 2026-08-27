using Nexus.Developer.Application.ObjectChatLinks.Commands.CreateObjectChatLink;
using Nexus.Developer.Application.Tasks.Commands.CreateTask;
using Nexus.Developer.Core.ChatCore;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.ObjectChatLinks;

namespace Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToTask;

// Second M-12-0.1 backend slice: the ConvertConversationToFeature composition
// pattern extended to Task. Same flow and the same known limitation -- if the
// ObjectChatLink step fails after the Task was persisted, the exception
// propagates as-is and the Task is intentionally left in place, unlinked.
public sealed class ConvertConversationToTaskHandler
{
    private readonly IChatCoreClient _chatCoreClient;
    private readonly CreateTaskHandler _createTaskHandler;
    private readonly CreateObjectChatLinkHandler _createObjectChatLinkHandler;

    public ConvertConversationToTaskHandler(
        IChatCoreClient chatCoreClient,
        CreateTaskHandler createTaskHandler,
        CreateObjectChatLinkHandler createObjectChatLinkHandler)
    {
        _chatCoreClient = chatCoreClient;
        _createTaskHandler = createTaskHandler;
        _createObjectChatLinkHandler = createObjectChatLinkHandler;
    }

    public async Task<ConvertConversationToTaskResult> HandleAsync(
        ConvertConversationToTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _chatCoreClient.GetConversationAsync(
            command.ConversationId,
            cancellationToken);

        if (conversation is null)
        {
            throw new ConversationNotFoundException(command.ConversationId);
        }

        var task = await _createTaskHandler.HandleAsync(
            new CreateTaskCommand(
                new FeatureId(command.FeatureId),
                command.Title,
                command.Description ?? string.Empty,
                command.CreatedByUserId),
            cancellationToken);

        // If this step throws after the Task was persisted above, the exception
        // propagates as-is and the Task is intentionally left in place,
        // unlinked -- a known limitation of this slice (M-12-0.1), not something
        // to paper over with a compensating delete.
        var link = await _createObjectChatLinkHandler.HandleAsync(
            new CreateObjectChatLinkCommand(
                command.ConversationId,
                command.MessageRangeStart,
                command.MessageRangeEnd,
                ObjectChatLinkTargetType.Task,
                task.TaskId.Value,
                command.CreatedByUserId),
            cancellationToken);

        return new ConvertConversationToTaskResult(
            task.TaskId,
            task.Reference,
            task.Title,
            link.ObjectChatLinkId,
            command.ConversationId);
    }
}
