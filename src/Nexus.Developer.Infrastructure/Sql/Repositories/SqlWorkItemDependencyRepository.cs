using Microsoft.EntityFrameworkCore;
using Nexus.Developer.Core.Dependencies;

namespace Nexus.Developer.Infrastructure.Sql.Repositories;

public sealed class SqlWorkItemDependencyRepository : IWorkItemDependencyRepository
{
    private readonly NexusDeveloperDbContext _context;

    public SqlWorkItemDependencyRepository(NexusDeveloperDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        WorkItemDependency dependency,
        CancellationToken cancellationToken = default)
    {
        _context.WorkItemDependencies.Add(dependency);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkItemDependency>> ListAllBlockingAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkItemDependencies
            .AsNoTracking()
            .Where(dependency => dependency.Kind == WorkItemDependencyKind.Blocking)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkItemDependency>> ListByNodeAsync(
        WorkItemDependencyNodeType nodeType,
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkItemDependencies
            .AsNoTracking()
            .Where(dependency =>
                (dependency.UpstreamType == nodeType && dependency.UpstreamId == nodeId) ||
                (dependency.DownstreamType == nodeType && dependency.DownstreamId == nodeId))
            .OrderBy(dependency => dependency.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
