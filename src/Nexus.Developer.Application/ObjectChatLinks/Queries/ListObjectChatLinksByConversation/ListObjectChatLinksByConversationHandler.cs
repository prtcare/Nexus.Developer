using Nexus.Developer.Core.ObjectChatLinks;

namespace Nexus.Developer.Application.ObjectChatLinks.Queries.ListObjectChatLinksByConversation;

public sealed class ListObjectChatLinksByConversationHandler
{
    private readonly IObjectChatLinkRepository _repository;

    public ListObjectChatLinksByConversationHandler(IObjectChatLinkRepository repository)
    {
        _repository = repository;
    }

    public async Task<ListObjectChatLinksByConversationResult> HandleAsync(
        ListObjectChatLinksByConversationQuery query,
        CancellationToken cancellationToken = default)
    {
        var links = await _repository.ListByConversationAsync(query.ConversationId, cancellationToken);

        var results = links
            .Select(link => new ObjectChatLinkResult(
                link.Id,
                link.ConversationId,
                link.MessageRangeStart,
                link.MessageRangeEnd,
                link.TargetType,
                link.TargetId,
                link.LinkedByUserId,
                link.LinkedAt))
            .ToList();

        return new ListObjectChatLinksByConversationResult(results);
    }
}
