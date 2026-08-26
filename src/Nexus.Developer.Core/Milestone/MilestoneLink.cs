using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.Milestone;

// The many-to-many join between a Milestone and a Feature/Task/Subtask row. A real
// row, not a UI-only association (M-07-10.1 acceptance). TargetId is a plain Guid
// tagged by TargetType because it must span three different strongly-typed ids
// (FeatureId/TaskId/SubtaskId) -- the same pattern used by IssueLink and
// ObjectChatLink for the same reason.
public sealed class MilestoneLink
{
    public MilestoneLink(
        MilestoneLinkId id,
        MilestoneId milestoneId,
        MilestoneLinkTargetType targetType,
        Guid targetId,
        Guid linkedByUserId,
        DateTimeOffset linkedAt)
    {
        if (!Enum.IsDefined(targetType))
        {
            throw new ArgumentOutOfRangeException(nameof(targetType));
        }

        Id = id;
        MilestoneId = milestoneId;
        TargetType = targetType;
        TargetId = targetId;
        LinkedByUserId = linkedByUserId;
        LinkedAt = linkedAt;
    }

    public MilestoneLinkId Id { get; }

    public MilestoneId MilestoneId { get; }

    public MilestoneLinkTargetType TargetType { get; }

    public Guid TargetId { get; }

    public Guid LinkedByUserId { get; }

    public DateTimeOffset LinkedAt { get; }
}
