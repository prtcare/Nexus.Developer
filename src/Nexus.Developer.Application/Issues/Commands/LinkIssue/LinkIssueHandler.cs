using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Issues;

namespace Nexus.Developer.Application.Issues.Commands.LinkIssue;

public sealed class LinkIssueHandler
{
    private readonly IIssueLinkRepository _repository;

    public LinkIssueHandler(IIssueLinkRepository repository)
    {
        _repository = repository;
    }

    public async Task<LinkIssueResult> HandleAsync(
        LinkIssueCommand command,
        CancellationToken cancellationToken = default)
    {
        var link = new IssueLink(
            IssueLinkId.New(),
            command.IssueId,
            command.TargetType,
            command.TargetId,
            command.LinkedByUserId,
            DateTimeOffset.UtcNow);

        await _repository.AddAsync(link, cancellationToken);

        return new LinkIssueResult(link.Id);
    }
}
