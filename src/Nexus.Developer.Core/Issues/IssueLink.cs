using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.Issues;

public sealed class IssueLink
{
    public IssueLink(
        IssueLinkId id,
        IssueId issueId,
        IssueLinkTargetType targetType,
        Guid targetId,
        Guid linkedByUserId,
        DateTimeOffset linkedAt)
    {
        if (!Enum.IsDefined(targetType))
        {
            throw new ArgumentOutOfRangeException(nameof(targetType));
        }

        Id = id;
        IssueId = issueId;
        TargetType = targetType;
        TargetId = targetId;
        LinkedByUserId = linkedByUserId;
        LinkedAt = linkedAt;
    }

    public IssueLinkId Id { get; }

    public IssueId IssueId { get; }

    public IssueLinkTargetType TargetType { get; }

    public Guid TargetId { get; }

    public Guid LinkedByUserId { get; }

    public DateTimeOffset LinkedAt { get; }
}
