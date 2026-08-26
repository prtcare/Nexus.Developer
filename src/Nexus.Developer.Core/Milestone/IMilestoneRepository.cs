using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.Milestone;

public interface IMilestoneRepository
    : IRepository<Milestone, MilestoneId>
{
    Task<IReadOnlyList<Milestone>> ListBySubprojectAsync(
        SubprojectId subprojectId,
        CancellationToken cancellationToken = default);
}
