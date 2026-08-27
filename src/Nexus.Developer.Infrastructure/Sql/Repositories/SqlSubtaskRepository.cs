using Microsoft.EntityFrameworkCore;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Subtasks;
using DomainSubtask = Nexus.Developer.Core.Subtasks.Subtask;

namespace Nexus.Developer.Infrastructure.Sql.Repositories;

public sealed class SqlSubtaskRepository : ISubtaskRepository
{
    private readonly NexusDeveloperDbContext _context;

    public SqlSubtaskRepository(NexusDeveloperDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        DomainSubtask domain,
        CancellationToken cancellationToken = default)
    {
        _context.Subtasks.Add(domain);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<DomainSubtask?> GetAsync(
        SubtaskId id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Subtasks
            .AsNoTracking()
            .FirstOrDefaultAsync(
                subtask => subtask.Id == id,
                cancellationToken);
    }

    public async Task UpdateAsync(
        DomainSubtask domain,
        CancellationToken cancellationToken = default)
    {
        if (_context.Entry(domain).State == EntityState.Detached)
        {
            _context.Subtasks.Update(domain);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DomainSubtask>> ListByTaskAsync(
        TaskId taskId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Subtasks
            .AsNoTracking()
            .Where(subtask => subtask.TaskId == taskId)
            .OrderBy(subtask => subtask.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
