using Microsoft.EntityFrameworkCore;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Milestones;
using DomainMilestone = Nexus.Developer.Core.Milestones.Milestone;

namespace Nexus.Developer.Infrastructure.Sql.Repositories;

public sealed class SqlMilestoneRepository : IMilestoneRepository
{
    private readonly NexusDeveloperDbContext _context;

    public SqlMilestoneRepository(NexusDeveloperDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        DomainMilestone domain,
        CancellationToken cancellationToken = default)
    {
        _context.Milestones.Add(domain);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<DomainMilestone?> GetAsync(
        MilestoneId id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Milestones
            .AsNoTracking()
            .FirstOrDefaultAsync(
                milestone => milestone.Id == id,
                cancellationToken);
    }

    public async Task UpdateAsync(
        DomainMilestone domain,
        CancellationToken cancellationToken = default)
    {
        if (_context.Entry(domain).State == EntityState.Detached)
        {
            _context.Milestones.Update(domain);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DomainMilestone>> ListBySubprojectAsync(
        SubprojectId subprojectId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Milestones
            .AsNoTracking()
            .Where(milestone => milestone.SubprojectId == subprojectId)
            .OrderBy(milestone => milestone.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
