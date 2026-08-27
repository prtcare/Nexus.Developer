using Microsoft.EntityFrameworkCore;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Issues;

namespace Nexus.Developer.Infrastructure.Sql.Repositories;

public sealed class SqlIssueLinkRepository : IIssueLinkRepository
{
    private readonly NexusDeveloperDbContext _context;

    public SqlIssueLinkRepository(NexusDeveloperDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        IssueLink link,
        CancellationToken cancellationToken = default)
    {
        _context.IssueLinks.Add(link);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IssueLink>> ListByIssueAsync(
        IssueId issueId,
        CancellationToken cancellationToken = default)
    {
        return await _context.IssueLinks
            .AsNoTracking()
            .Where(link => link.IssueId == issueId)
            .OrderBy(link => link.LinkedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IssueLink>> ListByTargetAsync(
        IssueLinkTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        return await _context.IssueLinks
            .AsNoTracking()
            .Where(link => link.TargetType == targetType && link.TargetId == targetId)
            .OrderBy(link => link.LinkedAt)
            .ToListAsync(cancellationToken);
    }
}
