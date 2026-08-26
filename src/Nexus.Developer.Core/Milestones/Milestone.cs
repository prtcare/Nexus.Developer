using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.Milestones;

// A grouping/delivery object, not a hierarchy level: it links to any set of
// Feature/Task/Subtask rows via MilestoneLink (many-to-many) and is never their
// parent (ADR-005 / M-07-10.1 acceptance: "No Milestone is a hierarchy parent of
// any node").
public sealed class Milestone : AggregateRoot<MilestoneId>
{
    public Milestone(
        MilestoneId id,
        SubprojectId subprojectId,
        string name,
        string description,
        DateTimeOffset? targetDate,
        Guid createdByUserId,
        DateTimeOffset createdAt)
        : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        SubprojectId = subprojectId;
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        TargetDate = targetDate;
        Status = MilestoneStatus.Planned;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
    }

    private Milestone(
        MilestoneId id,
        SubprojectId subprojectId,
        string name,
        string description,
        DateTimeOffset? targetDate,
        MilestoneStatus status,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        string reference)
        : base(id)
    {
        SubprojectId = subprojectId;
        Name = name;
        Description = description;
        TargetDate = targetDate;
        Status = status;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
        Reference = reference;
    }

    public SubprojectId SubprojectId { get; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public DateTimeOffset? TargetDate { get; private set; }

    public MilestoneStatus Status { get; private set; }

    public Guid CreatedByUserId { get; }

    public DateTimeOffset CreatedAt { get; }

    public string Reference { get; private set; } = string.Empty;

    public static Milestone Restore(
        MilestoneId id,
        SubprojectId subprojectId,
        string name,
        string description,
        DateTimeOffset? targetDate,
        MilestoneStatus status,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        string reference)
        => new(id, subprojectId, name, description, targetDate, status, createdByUserId, createdAt, reference);

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
    }

    public void ChangeDescription(string description)
    {
        Description = description?.Trim() ?? string.Empty;
    }

    public void Retarget(DateTimeOffset? targetDate)
    {
        TargetDate = targetDate;
    }

    public void ChangeStatus(MilestoneStatus status)
    {
        Status = status;
    }
}
