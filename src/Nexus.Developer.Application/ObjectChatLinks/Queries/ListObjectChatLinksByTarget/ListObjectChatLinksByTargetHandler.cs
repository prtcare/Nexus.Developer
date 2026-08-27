using Nexus.Developer.Core.ObjectChatLinks;

namespace Nexus.Developer.Application.ObjectChatLinks.Queries.ListObjectChatLinksByTarget;

public sealed class ListObjectChatLinksByTargetHandler
{
    private readonly IObjectChatLinkRepository _repository;

    public ListObjectChatLinksByTargetHandler(IObjectChatLinkRepository repository)
    {
        _repository = repository;
    }

    public async Task<ListObjectChatLinksByTargetResult> HandleAsync(
        ListObjectChatLinksByTargetQuery query,
        CancellationToken cancellationToken = default)
    {
        // "Reopening the object shows the associated chat": given a target type + id,
        // return every conversation that object is linked to (M-07-10.4 acceptance).
        var links = await _repository.ListByTargetAsync(query.TargetType, query.TargetId, cancellationToken);

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

        return new ListObjectChatLinksByTargetResult(results);
    }
}
