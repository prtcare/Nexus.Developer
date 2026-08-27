using Nexus.Developer.Application.Milestones.Commands.CreateMilestone;
using Nexus.Developer.Application.ObjectChatLinks.Commands.CreateObjectChatLink;
using Nexus.Developer.Core.ChatCore;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.ObjectChatLinks;

namespace Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToMilestone;

// Second M-12-0.1 backend slice: the ConvertConversationToFeature composition
// pattern extended to Milestone. Milestone carries a Name (not a Title) and an
// optional TargetDate; otherwise the same flow and the same known limitation --
// if the ObjectChatLink step fails after the Milestone was persisted, the
// exception propagates as-is and the Milestone is intentionally left in place,
// unlinked.
public sealed class ConvertConversationToMilestoneHandler
{
    private readonly IChatCoreClient _chatCoreClient;
    private readonly CreateMilestoneHandler _createMilestoneHandler;
    private readonly CreateObjectChatLinkHandler _createObjectChatLinkHandler;

    public ConvertConversationToMilestoneHandler(
        IChatCoreClient chatCoreClient,
        CreateMilestoneHandler createMilestoneHandler,
        CreateObjectChatLinkHandler createObjectChatLinkHandler)
    {
        _chatCoreClient = chatCoreClient;
        _createMilestoneHandler = createMilestoneHandler;
        _createObjectChatLinkHandler = createObjectChatLinkHandler;
    }

    public async Task<ConvertConversationToMilestoneResult> HandleAsync(
        ConvertConversationToMilestoneCommand command,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _chatCoreClient.GetConversationAsync(
            command.ConversationId,
            cancellationToken);

        if (conversation is null)
        {
            throw new ConversationNotFoundException(command.ConversationId);
        }

        var milestone = await _createMilestoneHandler.HandleAsync(
            new CreateMilestoneCommand(
                new SubprojectId(command.SubprojectId),
                command.Name,
                command.Description ?? string.Empty,
                command.TargetDate,
                command.CreatedByUserId),
            cancellationToken);

        // If this step throws after the Milestone was persisted above, the exception
        // propagates as-is and the Milestone is intentionally left in place,
        // unlinked -- a known limitation of this slice (M-12-0.1), not something
        // to paper over with a compensating delete.
        var link = await _createObjectChatLinkHandler.HandleAsync(
            new CreateObjectChatLinkCommand(
                command.ConversationId,
                command.MessageRangeStart,
                command.MessageRangeEnd,
                ObjectChatLinkTargetType.Milestone,
                milestone.MilestoneId.Value,
                command.CreatedByUserId),
            cancellationToken);

        return new ConvertConversationToMilestoneResult(
            milestone.MilestoneId,
            milestone.Reference,
            milestone.Name,
            link.ObjectChatLinkId,
            command.ConversationId);
    }
}
