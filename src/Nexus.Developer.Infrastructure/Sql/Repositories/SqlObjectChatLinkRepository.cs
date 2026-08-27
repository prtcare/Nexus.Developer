using Microsoft.EntityFrameworkCore;
using Nexus.Developer.Core.ObjectChatLinks;

namespace Nexus.Developer.Infrastructure.Sql.Repositories;

public sealed class SqlObjectChatLinkRepository : IObjectChatLinkRepository
{
    private readonly NexusDeveloperDbContext _context;

    public SqlObjectChatLinkRepository(NexusDeveloperDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        ObjectChatLink link,
        CancellationToken cancellationToken = default)
    {
        _context.ObjectChatLinks.Add(link);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ObjectChatLink>> ListByConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ObjectChatLinks
            .AsNoTracking()
            .Where(link => link.ConversationId == conversationId)
            .OrderBy(link => link.LinkedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ObjectChatLink>> ListByTargetAsync(
        ObjectChatLinkTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ObjectChatLinks
            .AsNoTracking()
            .Where(link => link.TargetType == targetType && link.TargetId == targetId)
            .OrderBy(link => link.LinkedAt)
            .ToListAsync(cancellationToken);
    }
}
