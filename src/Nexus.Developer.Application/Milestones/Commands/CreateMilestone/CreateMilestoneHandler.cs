using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Milestones;
using DomainMilestone = Nexus.Developer.Core.Milestones.Milestone;

namespace Nexus.Developer.Application.Milestones.Commands.CreateMilestone;

public sealed class CreateMilestoneHandler
{
    private readonly IMilestoneRepository _repository;

    public CreateMilestoneHandler(IMilestoneRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateMilestoneResult> HandleAsync(
        CreateMilestoneCommand command,
        CancellationToken cancellationToken = default)
    {
        var milestone = new DomainMilestone(
            MilestoneId.New(),
            command.SubprojectId,
            command.Name,
            command.Description,
            command.TargetDate,
            command.CreatedByUserId,
            DateTimeOffset.UtcNow);

        await _repository.AddAsync(milestone, cancellationToken);

        return new CreateMilestoneResult(
            milestone.Id,
            milestone.Name,
            milestone.Reference);
    }
}
