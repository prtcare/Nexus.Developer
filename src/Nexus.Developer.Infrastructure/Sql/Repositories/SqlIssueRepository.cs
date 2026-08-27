using Microsoft.EntityFrameworkCore;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Issues;
using DomainIssue = Nexus.Developer.Core.Issues.Issue;

namespace Nexus.Developer.Infrastructure.Sql.Repositories;

public sealed class SqlIssueRepository : IIssueRepository
{
    private readonly NexusDeveloperDbContext _context;

    public SqlIssueRepository(NexusDeveloperDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        DomainIssue domain,
        CancellationToken cancellationToken = default)
    {
        _context.Issues.Add(domain);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<DomainIssue?> GetAsync(
        IssueId id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Issues
            .AsNoTracking()
            .FirstOrDefaultAsync(
                issue => issue.Id == id,
                cancellationToken);
    }

    public async Task UpdateAsync(
        DomainIssue domain,
        CancellationToken cancellationToken = default)
    {
        if (_context.Entry(domain).State == EntityState.Detached)
        {
            _context.Issues.Update(domain);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
