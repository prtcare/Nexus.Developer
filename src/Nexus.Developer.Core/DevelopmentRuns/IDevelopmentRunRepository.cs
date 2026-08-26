using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.DevelopmentRuns;

public interface IDevelopmentRunRepository
    : IRepository<DevelopmentRun, DevelopmentRunId>
{
    Task<IReadOnlyList<DevelopmentRun>> ListByTargetAsync(
        DevelopmentRunTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default);
}
