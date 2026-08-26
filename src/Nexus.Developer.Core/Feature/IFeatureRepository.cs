using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.Feature;

public interface IFeatureRepository
    : IRepository<Feature, FeatureId>
{
    Task<IReadOnlyList<Feature>> ListBySubprojectAsync(
        SubprojectId subprojectId,
        CancellationToken cancellationToken = default);
}
