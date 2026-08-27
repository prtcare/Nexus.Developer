using Microsoft.EntityFrameworkCore;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Tasks;
using DeveloperTask = Nexus.Developer.Core.Tasks.Task;

namespace Nexus.Developer.Infrastructure.Sql.Repositories;

// Spells out System.Threading.Tasks.Task explicitly for the same reason
// ITaskRepository itself does -- see that interface's remarks.
public sealed class SqlTaskRepository : ITaskRepository
{
    private readonly NexusDeveloperDbContext _context;

    public SqlTaskRepository(NexusDeveloperDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task AddAsync(
        DeveloperTask task,
        CancellationToken cancellationToken = default)
    {
        _context.Tasks.Add(task);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<DeveloperTask?> GetAsync(
        TaskId id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(
                task => task.Id == id,
                cancellationToken);
    }

    public async System.Threading.Tasks.Task UpdateAsync(
        DeveloperTask task,
        CancellationToken cancellationToken = default)
    {
        if (_context.Entry(task).State == EntityState.Detached)
        {
            _context.Tasks.Update(task);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<IReadOnlyList<DeveloperTask>> ListByFeatureAsync(
        FeatureId featureId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .AsNoTracking()
            .Where(task => task.FeatureId == featureId)
            .OrderBy(task => task.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
