using Nexus.Developer.Application.Scope;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Features;
using Nexus.Developer.Core.Scope;

namespace Nexus.Developer.Application.Features.Commands.CreateFeature;

public sealed class CreateFeatureHandler
{
    private readonly IScopeClient _scopeClient;
    private readonly IFeatureRepository _repository;

    public CreateFeatureHandler(
        IScopeClient scopeClient,
        IFeatureRepository repository)
    {
        _scopeClient = scopeClient;
        _repository = repository;
    }

    public async Task<CreateFeatureResult> HandleAsync(
        CreateFeatureCommand command,
        CancellationToken cancellationToken = default)
    {
        // The Subproject is a foreign Product Core entity (hosted in
        // Nexus.Experience) -- never persist a Feature under a SubprojectId that
        // does not exist (M-07-10.1). GetSubprojectAsync returns null on a
        // confirmed 404; anything else throws as a real infrastructure failure.
        var subproject = await _scopeClient.GetSubprojectAsync(
            command.SubprojectId,
            cancellationToken);

        if (subproject is null)
        {
            throw new SubprojectNotFoundException(command.SubprojectId);
        }

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
