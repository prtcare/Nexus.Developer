using Microsoft.EntityFrameworkCore;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Features;
using DomainFeature = Nexus.Developer.Core.Features.Feature;

namespace Nexus.Developer.Infrastructure.Sql.Repositories;

public sealed class SqlFeatureRepository : IFeatureRepository
{
    private readonly NexusDeveloperDbContext _context;

    public SqlFeatureRepository(NexusDeveloperDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        DomainFeature domain,
        CancellationToken cancellationToken = default)
    {
        _context.Features.Add(domain);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<DomainFeature?> GetAsync(
        FeatureId id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Features
            .AsNoTracking()
            .FirstOrDefaultAsync(
                feature => feature.Id == id,
                cancellationToken);
    }

    public async Task UpdateAsync(
        DomainFeature domain,
        CancellationToken cancellationToken = default)
    {
        if (_context.Entry(domain).State == EntityState.Detached)
        {
            _context.Features.Update(domain);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DomainFeature>> ListBySubprojectAsync(
        SubprojectId subprojectId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Features
            .AsNoTracking()
            .Where(feature => feature.SubprojectId == subprojectId)
            .OrderBy(feature => feature.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
