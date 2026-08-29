using Nexus.Developer.Application.Scope;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Milestones;
using Nexus.Developer.Core.Scope;
using DomainMilestone = Nexus.Developer.Core.Milestones.Milestone;

namespace Nexus.Developer.Application.Milestones.Commands.CreateMilestone;

public sealed class CreateMilestoneHandler
{
    private readonly IScopeClient _scopeClient;
    private readonly IMilestoneRepository _repository;

    public CreateMilestoneHandler(
        IScopeClient scopeClient,
        IMilestoneRepository repository)
    {
        _scopeClient = scopeClient;
        _repository = repository;
    }

    public async Task<CreateMilestoneResult> HandleAsync(
        CreateMilestoneCommand command,
        CancellationToken cancellationToken = default)
    {
        // Same foreign-Subproject guard as CreateFeatureHandler (M-07-10.1):
        // never persist a Milestone under a SubprojectId that does not exist.
        var subproject = await _scopeClient.GetSubprojectAsync(
            command.SubprojectId,
            cancellationToken);

        if (subproject is null)
        {
            throw new SubprojectNotFoundException(command.SubprojectId);
        }

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
