using Nexus.Developer.Core.Milestones;

namespace Nexus.Developer.Application.Milestones.Queries.GetMilestone;

public sealed class GetMilestoneHandler
{
    private readonly IMilestoneRepository _repository;

    public GetMilestoneHandler(IMilestoneRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetMilestoneResult?> HandleAsync(
        GetMilestoneQuery query,
        CancellationToken cancellationToken = default)
    {
        var milestone = await _repository.GetAsync(query.MilestoneId, cancellationToken);

        if (milestone is null)
        {
            return null;
        }

        return new GetMilestoneResult(
            milestone.Id,
            milestone.SubprojectId,
            milestone.Name,
            milestone.Description,
            milestone.TargetDate,
            milestone.Status,
            milestone.CreatedByUserId,
            milestone.CreatedAt,
            milestone.Reference);
    }
}
