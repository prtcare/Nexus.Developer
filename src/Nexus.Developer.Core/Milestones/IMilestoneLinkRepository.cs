using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.Milestones;

public interface IMilestoneLinkRepository
{
    Task AddAsync(
        MilestoneLink link,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MilestoneLink>> ListByMilestoneAsync(
        MilestoneId milestoneId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MilestoneLink>> ListByTargetAsync(
        MilestoneLinkTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default);
}
