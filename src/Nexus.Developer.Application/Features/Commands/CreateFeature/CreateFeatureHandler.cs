using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Features;

namespace Nexus.Developer.Application.Features.Commands.CreateFeature;

public sealed class CreateFeatureHandler
{
    private readonly IFeatureRepository _repository;

    public CreateFeatureHandler(IFeatureRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateFeatureResult> HandleAsync(
        CreateFeatureCommand command,
        CancellationToken cancellationToken = default)
    {
        var feature = new Feature(
            FeatureId.New(),
            command.SubprojectId,
            command.Title,
            command.Description,
            command.CreatedByUserId,
            DateTimeOffset.UtcNow);

        await _repository.AddAsync(feature, cancellationToken);

        return new CreateFeatureResult(
            feature.Id,
            feature.Title,
            feature.Reference);
    }
}
