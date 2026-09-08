using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.Tasks;

// Named "Task" to match the user-specified hierarchy (Workspace > Project >
// Subproject > Feature > Task > Subtask) verbatim. This collides by name with
// System.Threading.Tasks.Task -- but the namespace is deliberately "Tasks"
// (plural), not "Task": a *namespace* segment named exactly "Task" would shadow
// System.Threading.Tasks.Task for every file anywhere under Nexus.Developer.Core
// (namespace-declaration lookup beats using-directive lookup in C#, so a sibling
// namespace called "Task" wins over "using System.Threading.Tasks;" project-wide,
// not just in files that reference this type -- this was caught by a real build
// failure across four unrelated files before the namespace was renamed to
// "Tasks"). A file that also needs the domain type under a shorter name can
// alias it, e.g. `using DeveloperTask = Nexus.Developer.Core.Tasks.Task;` -- see
// ITaskRepository.cs.
public sealed class Task : AggregateRoot<TaskId>
{
    public Task(
        TaskId id,
        FeatureId featureId,
        string title,
        string description,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        string? sourceRoadmapNodeId = null)
        : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        FeatureId = featureId;
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Status = DevelopmentItemStatus.New;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
        SourceRoadmapNodeId = string.IsNullOrWhiteSpace(sourceRoadmapNodeId)
            ? null
            : sourceRoadmapNodeId.Trim();
    }

    // WI-07-10.2.1: one-time, reviewable migration path from the Chat product's
    // existing WorkItem rows. Never used by normal Create -- only by the migration
    // script, and it keeps the origin id so the mapping is traceable, not silent
    // (M-07-10.2 acceptance: every WorkItem row maps to a Task referencing the
    // original WorkItem id; nothing is silently deleted).
    public static Task CreateFromWorkItemMigration(
        TaskId id,
        FeatureId featureId,
        string title,
        string description,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        Guid migratedFromWorkItemId)
    {
        var task = new Task(id, featureId, title, description, createdByUserId, createdAt);
        task.MigratedFromWorkItemId = migratedFromWorkItemId;
        return task;
    }

    private Task(
        TaskId id,
        FeatureId featureId,
        string title,
        string description,
        DevelopmentItemStatus status,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        string reference,
        Guid? migratedFromWorkItemId,
        string? sourceRoadmapNodeId = null)
        : base(id)
    {
        FeatureId = featureId;
        Title = title;
        Description = description;
        Status = status;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
        Reference = reference;
        MigratedFromWorkItemId = migratedFromWorkItemId;
        SourceRoadmapNodeId = sourceRoadmapNodeId;
    }

    public FeatureId FeatureId { get; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public DevelopmentItemStatus Status { get; private set; }

    public Guid CreatedByUserId { get; }

    public DateTimeOffset CreatedAt { get; }

    public string Reference { get; private set; } = string.Empty;

    // Set only via CreateFromWorkItemMigration or Restore. Null for every Task
    // created directly through Developer Chat / the Developer UI.
    public Guid? MigratedFromWorkItemId { get; private set; }

    // WU-02 narrow bridge (WAVE-05A Lane D): optional, external traceability only.
    // Holds the roadmap-ledger NodeId string (e.g. "WI-07-2.1.1", "T-07-1") that
    // originated this Task when the roadmap importer (WI-07-1.1.3) wrote it.
    // Never a Guid, never runtime identity, never routing authority, never a
    // substitute for DevelopmentControlAddress. Plain external reference string;
    // null for every Task created without a roadmap origin. Set only at
    // construction (blank -> null); carried through Restore. Distinct from
    // MigratedFromWorkItemId (Chat WorkItem provenance, a Guid) -- that field
    // stays the migration-script writer's; this one is the roadmap ledger.
    public string? SourceRoadmapNodeId { get; }

    public static Task Restore(
        TaskId id,
        FeatureId featureId,
        string title,
        string description,
        DevelopmentItemStatus status,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        string reference,
        Guid? migratedFromWorkItemId,
        string? sourceRoadmapNodeId = null)
        => new(id, featureId, title, description, status, createdByUserId, createdAt, reference, migratedFromWorkItemId, sourceRoadmapNodeId);

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
