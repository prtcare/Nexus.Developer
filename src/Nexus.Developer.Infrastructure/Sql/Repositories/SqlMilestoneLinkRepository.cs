using Microsoft.EntityFrameworkCore;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Milestones;

namespace Nexus.Developer.Infrastructure.Sql.Repositories;

public sealed class SqlMilestoneLinkRepository : IMilestoneLinkRepository
{
    private readonly NexusDeveloperDbContext _context;

    public SqlMilestoneLinkRepository(NexusDeveloperDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        MilestoneLink link,
        CancellationToken cancellationToken = default)
    {
        _context.MilestoneLinks.Add(link);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MilestoneLink>> ListByMilestoneAsync(
        MilestoneId milestoneId,
        CancellationToken cancellationToken = default)
    {
        return await _context.MilestoneLinks
            .AsNoTracking()
            .Where(link => link.MilestoneId == milestoneId)
            .OrderBy(link => link.LinkedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MilestoneLink>> ListByTargetAsync(
        MilestoneLinkTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        return await _context.MilestoneLinks
            .AsNoTracking()
            .Where(link => link.TargetType == targetType && link.TargetId == targetId)
            .OrderBy(link => link.LinkedAt)
            .ToListAsync(cancellationToken);
    }
}
