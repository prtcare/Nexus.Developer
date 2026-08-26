using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.Features;

// Top of Nexus.Developer's own owned hierarchy: Subproject (foreign, Product Core)
// > Feature > Task > Subtask. Milestone links to this via MilestoneLink without ever
// being its parent (ADR-005 / F-07-10).
public sealed class Feature : AggregateRoot<FeatureId>
{
    public Feature(
        FeatureId id,
        SubprojectId subprojectId,
        string title,
        string description,
        Guid createdByUserId,
        DateTimeOffset createdAt)
        : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        SubprojectId = subprojectId;
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Status = DevelopmentItemStatus.New;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
    }

    private Feature(
        FeatureId id,
        SubprojectId subprojectId,
        string title,
        string description,
        DevelopmentItemStatus status,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        string reference)
        : base(id)
    {
        SubprojectId = subprojectId;
        Title = title;
        Description = description;
        Status = status;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
        Reference = reference;
    }

    public SubprojectId SubprojectId { get; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public DevelopmentItemStatus Status { get; private set; }

    public Guid CreatedByUserId { get; }

    public DateTimeOffset CreatedAt { get; }

    public string Reference { get; private set; } = string.Empty;

    // Rehydration path: only a repository restoring a persisted row knows the
    // reference the store already allocated - the constructor above never does.
    public static Feature Restore(
        FeatureId id,
        SubprojectId subprojectId,
        string title,
        string description,
        DevelopmentItemStatus status,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        string reference)
        => new(id, subprojectId, title, description, status, createdByUserId, createdAt, reference);

    public void Rename(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title.Trim();
    }

    public void ChangeDescription(string description)
    {
        Description = description?.Trim() ?? string.Empty;
    }

    public void ChangeStatus(DevelopmentItemStatus status)
    {
        Status = status;
    }
}
