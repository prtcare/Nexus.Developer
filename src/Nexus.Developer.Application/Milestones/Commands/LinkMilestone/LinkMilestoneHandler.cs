using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Milestones;

namespace Nexus.Developer.Application.Milestones.Commands.LinkMilestone;

public sealed class LinkMilestoneHandler
{
    private readonly IMilestoneLinkRepository _repository;

    public LinkMilestoneHandler(IMilestoneLinkRepository repository)
    {
        _repository = repository;
    }

    public async Task<LinkMilestoneResult> HandleAsync(
        LinkMilestoneCommand command,
        CancellationToken cancellationToken = default)
    {
        var link = new MilestoneLink(
            MilestoneLinkId.New(),
            command.MilestoneId,
            command.TargetType,
            command.TargetId,
            command.LinkedByUserId,
            DateTimeOffset.UtcNow);

        await _repository.AddAsync(link, cancellationToken);

        return new LinkMilestoneResult(link.Id);
    }
}
