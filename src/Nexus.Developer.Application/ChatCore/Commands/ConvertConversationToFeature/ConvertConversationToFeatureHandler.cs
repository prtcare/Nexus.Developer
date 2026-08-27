using Nexus.Developer.Application.Features.Commands.CreateFeature;
using Nexus.Developer.Application.ObjectChatLinks.Commands.CreateObjectChatLink;
using Nexus.Developer.Core.ChatCore;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.ObjectChatLinks;

namespace Nexus.Developer.Application.ChatCore.Commands.ConvertConversationToFeature;

// First M-12-0.1 backend slice: compose Chat Core (Nexus.Experience) + Developer's
// existing Feature and ObjectChatLink slices into one "convert this conversation
// into a Feature" flow. Feature only for now -- Task/Subtask/Milestone/Issue
// follow later once this proves out.
public sealed class ConvertConversationToFeatureHandler
{
    private readonly IChatCoreClient _chatCoreClient;
    private readonly CreateFeatureHandler _createFeatureHandler;
    private readonly CreateObjectChatLinkHandler _createObjectChatLinkHandler;

    public ConvertConversationToFeatureHandler(
        IChatCoreClient chatCoreClient,
        CreateFeatureHandler createFeatureHandler,
        CreateObjectChatLinkHandler createObjectChatLinkHandler)
    {
        _chatCoreClient = chatCoreClient;
        _createFeatureHandler = createFeatureHandler;
        _createObjectChatLinkHandler = createObjectChatLinkHandler;
    }

    public async Task<ConvertConversationToFeatureResult> HandleAsync(
        ConvertConversationToFeatureCommand command,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _chatCoreClient.GetConversationAsync(
            command.ConversationId,
            cancellationToken);

        if (conversation is null)
        {
            throw new ConversationNotFoundException(command.ConversationId);
        }

        var feature = await _createFeatureHandler.HandleAsync(
            new CreateFeatureCommand(
                new SubprojectId(command.SubprojectId),
                command.Title,
                command.Description ?? string.Empty,
                command.CreatedByUserId),
            cancellationToken);

        // If this step throws after the Feature was persisted above, the exception
        // propagates as-is and the Feature is intentionally left in place,
        // unlinked -- a known limitation of this slice (M-12-0.1), not something
        // to paper over with a compensating delete.
        var link = await _createObjectChatLinkHandler.HandleAsync(
            new CreateObjectChatLinkCommand(
                command.ConversationId,
                command.MessageRangeStart,
                command.MessageRangeEnd,
                ObjectChatLinkTargetType.Feature,
                feature.FeatureId.Value,
                command.CreatedByUserId),
            cancellationToken);

        return new ConvertConversationToFeatureResult(
            feature.FeatureId,
            feature.Reference,
            feature.Title,
            link.ObjectChatLinkId,
            command.ConversationId);
    }
}
