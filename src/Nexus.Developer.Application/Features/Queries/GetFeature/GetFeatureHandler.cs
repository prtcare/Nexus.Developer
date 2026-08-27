using Nexus.Developer.Core.Features;

namespace Nexus.Developer.Application.Features.Queries.GetFeature;

public sealed class GetFeatureHandler
{
    private readonly IFeatureRepository _repository;

    public GetFeatureHandler(IFeatureRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetFeatureResult?> HandleAsync(
        GetFeatureQuery query,
        CancellationToken cancellationToken = default)
    {
        var feature = await _repository.GetAsync(query.FeatureId, cancellationToken);

        if (feature is null)
        {
            return null;
        }

        return new GetFeatureResult(
            feature.Id,
            feature.SubprojectId,
            feature.Title,
            feature.Description,
            feature.Status,
            feature.CreatedByUserId,
            feature.CreatedAt,
            feature.Reference);
    }
}
