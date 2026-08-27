using Nexus.Developer.Application.Features.Queries.GetFeature;
using Nexus.Developer.Core.Features;

namespace Nexus.Developer.Application.Features.Queries.ListFeaturesBySubproject;

public sealed class ListFeaturesBySubprojectHandler
{
    private readonly IFeatureRepository _repository;

    public ListFeaturesBySubprojectHandler(IFeatureRepository repository)
    {
        _repository = repository;
    }

    public async Task<ListFeaturesBySubprojectResult> HandleAsync(
        ListFeaturesBySubprojectQuery query,
        CancellationToken cancellationToken = default)
    {
        var features = await _repository.ListBySubprojectAsync(query.SubprojectId, cancellationToken);

        var results = features
            .Select(feature => new GetFeatureResult(
                feature.Id,
                feature.SubprojectId,
                feature.Title,
                feature.Description,
                feature.Status,
                feature.CreatedByUserId,
                feature.CreatedAt,
                feature.Reference))
            .ToList();

        return new ListFeaturesBySubprojectResult(results);
    }
}
