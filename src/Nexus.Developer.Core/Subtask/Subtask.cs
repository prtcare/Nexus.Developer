using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.Subtask;

public sealed class Subtask : AggregateRoot<SubtaskId>
{
    public Subtask(
        SubtaskId id,
        TaskId taskId,
        string title,
        string description,
        Guid createdByUserId,
        DateTimeOffset createdAt)
        : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        TaskId = taskId;
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Status = DevelopmentItemStatus.New;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
    }

    private Subtask(
        SubtaskId id,
        TaskId taskId,
        string title,
        string description,
        DevelopmentItemStatus status,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        string reference)
        : base(id)
    {
        TaskId = taskId;
        Title = title;
        Description = description;
        Status = status;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
        Reference = reference;
    }

    public TaskId TaskId { get; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public DevelopmentItemStatus Status { get; private set; }

    public Guid CreatedByUserId { get; }

    public DateTimeOffset CreatedAt { get; }

    public string Reference { get; private set; } = string.Empty;

    public static Subtask Restore(
        SubtaskId id,
        TaskId taskId,
        string title,
        string description,
        DevelopmentItemStatus status,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        string reference)
        => new(id, taskId, title, description, status, createdByUserId, createdAt, reference);

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
