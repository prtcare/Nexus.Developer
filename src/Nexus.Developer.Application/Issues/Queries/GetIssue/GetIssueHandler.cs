using Nexus.Developer.Core.Issues;

namespace Nexus.Developer.Application.Issues.Queries.GetIssue;

public sealed class GetIssueHandler
{
    private readonly IIssueRepository _repository;

    public GetIssueHandler(IIssueRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetIssueResult?> HandleAsync(
        GetIssueQuery query,
        CancellationToken cancellationToken = default)
    {
        var issue = await _repository.GetAsync(query.IssueId, cancellationToken);

        if (issue is null)
        {
            return null;
        }

        return new GetIssueResult(
            issue.Id,
            issue.Title,
            issue.Description,
            issue.Status,
            issue.CreatedByUserId,
            issue.CreatedAt,
            issue.Reference);
    }
}
