namespace Nexus.Developer.Core.ObjectChatLink;

public interface IObjectChatLinkRepository
{
    Task AddAsync(
        ObjectChatLink link,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ObjectChatLink>> ListByConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ObjectChatLink>> ListByTargetAsync(
        ObjectChatLinkTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default);
}
