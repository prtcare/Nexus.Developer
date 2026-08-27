using Microsoft.EntityFrameworkCore;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.DevelopmentRuns;
using DomainDevelopmentRun = Nexus.Developer.Core.DevelopmentRuns.DevelopmentRun;

namespace Nexus.Developer.Infrastructure.Sql.Repositories;

public sealed class SqlDevelopmentRunRepository : IDevelopmentRunRepository
{
    private readonly NexusDeveloperDbContext _context;

    public SqlDevelopmentRunRepository(NexusDeveloperDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        DomainDevelopmentRun domain,
        CancellationToken cancellationToken = default)
    {
        _context.DevelopmentRuns.Add(domain);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<DomainDevelopmentRun?> GetAsync(
        DevelopmentRunId id,
        CancellationToken cancellationToken = default)
    {
        return await _context.DevelopmentRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(
                run => run.Id == id,
                cancellationToken);
    }

    public async Task UpdateAsync(
        DomainDevelopmentRun domain,
        CancellationToken cancellationToken = default)
    {
        if (_context.Entry(domain).State == EntityState.Detached)
        {
            _context.DevelopmentRuns.Update(domain);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DomainDevelopmentRun>> ListByTargetAsync(
        DevelopmentRunTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        return await _context.DevelopmentRuns
            .AsNoTracking()
            .Where(run => run.TargetType == targetType && run.TargetId == targetId)
            .OrderBy(run => run.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
