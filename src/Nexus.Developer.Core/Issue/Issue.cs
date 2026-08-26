using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.Issue;

// Universally attachable: an Issue is never permanently positioned inside the
// Workspace > Project > Subproject > Feature > Task > Subtask hierarchy. All of its
// positioning comes from IssueLink rows, which is why this aggregate carries no
// parent id of its own.
public sealed class Issue : AggregateRoot<IssueId>
{
    public Issue(
        IssueId id,
        string title,
        string description,
        Guid createdByUserId,
        DateTimeOffset createdAt)
        : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Status = IssueStatus.Open;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
    }

    private Issue(
        IssueId id,
        string title,
        string description,
        IssueStatus status,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        string reference)
        : base(id)
    {
        Title = title;
        Description = description;
        Status = status;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
        Reference = reference;
    }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public IssueStatus Status { get; private set; }

    public Guid CreatedByUserId { get; }

    public DateTimeOffset CreatedAt { get; }

    public string Reference { get; private set; } = string.Empty;

    public static Issue Restore(
        IssueId id,
        string title,
        string description,
        IssueStatus status,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        string reference)
        => new(id, title, description, status, createdByUserId, createdAt, reference);

    public void Rename(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title.Trim();
    }

    public void ChangeDescription(string description)
    {
        Description = description?.Trim() ?? string.Empty;
    }

    public void ChangeStatus(IssueStatus status)
    {
        Status = status;
    }
}
