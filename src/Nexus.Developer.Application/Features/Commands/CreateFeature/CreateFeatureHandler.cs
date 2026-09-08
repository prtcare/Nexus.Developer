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

        // A subfeature may only be created under an existing parent Feature in the
        // same Subproject (D04). The parent is Developer's own row, so it is
        // resolved through IFeatureRepository (unlike the foreign Subproject above).
        if (command.ParentFeatureId is not null)
        {
            var parent = await _repository.GetAsync(
                command.ParentFeatureId.Value,
                cancellationToken);

            if (parent is null)
            {
                throw new FeatureParentNotFoundException(command.ParentFeatureId.Value);
            }

            if (parent.SubprojectId != command.SubprojectId)
            {
                throw new ArgumentException(
                    "A child feature must belong to the same subproject as its parent.",
                    nameof(command.ParentFeatureId));
            }
        }

        var feature = new Feature(
            FeatureId.New(),
            command.SubprojectId,
            command.Title,
            command.Description,
            command.CreatedByUserId,
            DateTimeOffset.UtcNow,
            command.ParentFeatureId,
            command.SourceRoadmapNodeId);

        await _repository.AddAsync(feature, cancellationToken);

        return new CreateFeatureResult(
            feature.Id,
            feature.Title,
            feature.Reference);
    }
}
