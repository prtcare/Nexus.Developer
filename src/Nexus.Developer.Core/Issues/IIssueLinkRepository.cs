using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.Issues;

public interface IIssueLinkRepository
{
    Task AddAsync(
        IssueLink link,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IssueLink>> ListByIssueAsync(
        IssueId issueId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IssueLink>> ListByTargetAsync(
        IssueLinkTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default);
}
