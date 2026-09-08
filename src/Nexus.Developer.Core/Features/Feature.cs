using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.Features;

// Top of Nexus.Developer's own owned hierarchy: Subproject (foreign, Product Core)
// > Feature > Task > Subtask. Milestone links to this via MilestoneLink without ever
// being its parent (ADR-005 / F-07-10).
//
// D04: a Feature may optionally be a child of another Feature in the same
// Subproject -- children point UP (the child holds its parent's id), mirroring how
// Task holds FeatureId / Subtask holds TaskId. ParentFeatureId == null means root.
// Depth is unrestricted; the single cross-aggregate structural rule (no ancestor
// cycles) is enforced at the application write boundary because this aggregate
// cannot see a parent's ancestor chain from a single loaded row (the same split
// WorkItemDependency uses for cycle prevention).
public sealed class Feature : AggregateRoot<FeatureId>
{
    public Feature(
        FeatureId id,
        SubprojectId subprojectId,
        string title,
        string description,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        FeatureId? parentFeatureId = null,
        string? sourceRoadmapNodeId = null)
        : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (parentFeatureId == id)
        {
            throw new ArgumentException(
                "A feature cannot be its own parent.",
                nameof(parentFeatureId));
        }

        SubprojectId = subprojectId;
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Status = DevelopmentItemStatus.New;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
        ParentFeatureId = parentFeatureId;
        SourceRoadmapNodeId = string.IsNullOrWhiteSpace(sourceRoadmapNodeId)
            ? null
            : sourceRoadmapNodeId.Trim();
    }

    private Feature(
        FeatureId id,
        SubprojectId subprojectId,
        string title,
        string description,
        DevelopmentItemStatus status,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        string reference,
        FeatureId? parentFeatureId = null,
        string? sourceRoadmapNodeId = null)
        : base(id)
    {
        SubprojectId = subprojectId;
        Title = title;
        Description = description;
        Status = status;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
        Reference = reference;
        ParentFeatureId = parentFeatureId;
        SourceRoadmapNodeId = sourceRoadmapNodeId;
    }

    public SubprojectId SubprojectId { get; }

    // null == root. A Feature's parent (when present) is another Feature in the
    // same Subproject; the database enforces parent-exists via the self-FK
    // FK_Feature_ParentFeature (Restrict).
    public FeatureId? ParentFeatureId { get; private set; }

    // WU-02 narrow bridge (WAVE-05A Lane D): optional, external traceability only.
    // Holds the roadmap-ledger NodeId string (e.g. "F-07-10", "WI-07-2.1.1") that
    // originated this Feature when the roadmap importer (WI-07-1.1.3) wrote it.
    // Never a Guid, never runtime identity, never routing authority, never a
    // substitute for DevelopmentControlAddress. Plain external reference string;
    // null for every Feature created without a roadmap origin. Set only at
    // construction (blank -> null); carried through Restore.
    public string? SourceRoadmapNodeId { get; }

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
        string reference,
        FeatureId? parentFeatureId = null,
        string? sourceRoadmapNodeId = null)
        => new(id, subprojectId, title, description, status, createdByUserId, createdAt, reference, parentFeatureId, sourceRoadmapNodeId);

    // Changes the feature's parent (null == promote to root). Takes the loaded
    // candidate parent so the self-parent and same-subproject rules can be
    // enforced inside the aggregate -- the aggregate cannot resolve an id to a row
    // on its own. Cycle prevention (a feature cannot become an ancestor of its own
    // ancestor) needs the parent's ancestor chain, which is repository state; the
    // application write boundary checks it before calling SetParent (the
    // WorkItemDependency split). There is deliberately no "child may not itself
    // have children" guard: depth is unrestricted (multi-level hierarchy).
    public void SetParent(Feature? parent)
    {
        if (parent is null)
        {
            ParentFeatureId = null;
            return;
        }

        if (parent.Id == Id)
        {
            throw new ArgumentException(
                "A feature cannot be its own parent.",
                nameof(parent));
        }

        if (parent.SubprojectId != SubprojectId)
        {
            throw new ArgumentException(
                "A child feature must belong to the same subproject as its parent.",
                nameof(parent));
        }

        ParentFeatureId = parent.Id;
    }

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
