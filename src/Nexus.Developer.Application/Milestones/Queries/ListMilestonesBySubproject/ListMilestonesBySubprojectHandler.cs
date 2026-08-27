using Nexus.Developer.Application.Milestones.Queries.GetMilestone;
using Nexus.Developer.Core.Milestones;

namespace Nexus.Developer.Application.Milestones.Queries.ListMilestonesBySubproject;

public sealed class ListMilestonesBySubprojectHandler
{
    private readonly IMilestoneRepository _repository;

    public ListMilestonesBySubprojectHandler(IMilestoneRepository repository)
    {
        _repository = repository;
    }

    public async Task<ListMilestonesBySubprojectResult> HandleAsync(
        ListMilestonesBySubprojectQuery query,
        CancellationToken cancellationToken = default)
    {
        var milestones = await _repository.ListBySubprojectAsync(query.SubprojectId, cancellationToken);

        var results = milestones
            .Select(milestone => new GetMilestoneResult(
                milestone.Id,
                milestone.SubprojectId,
                milestone.Name,
                milestone.Description,
                milestone.TargetDate,
                milestone.Status,
                milestone.CreatedByUserId,
                milestone.CreatedAt,
                milestone.Reference))
            .ToList();

        return new ListMilestonesBySubprojectResult(results);
    }
}
