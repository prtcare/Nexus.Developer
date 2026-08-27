using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Issues;
using DomainIssue = Nexus.Developer.Core.Issues.Issue;

namespace Nexus.Developer.Application.Issues.Commands.CreateIssue;

public sealed class CreateIssueHandler
{
    private readonly IIssueRepository _repository;

    public CreateIssueHandler(IIssueRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateIssueResult> HandleAsync(
        CreateIssueCommand command,
        CancellationToken cancellationToken = default)
    {
        var issue = new DomainIssue(
            IssueId.New(),
            command.Title,
            command.Description,
            command.CreatedByUserId,
            DateTimeOffset.UtcNow);

        await _repository.AddAsync(issue, cancellationToken);

        return new CreateIssueResult(
            issue.Id,
            issue.Title,
            issue.Reference);
    }
}
