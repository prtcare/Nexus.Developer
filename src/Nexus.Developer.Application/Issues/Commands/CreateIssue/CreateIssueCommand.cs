namespace Nexus.Developer.Application.Issues.Commands.CreateIssue;

public sealed record CreateIssueCommand(
    string Title,
    string Description,
    Guid CreatedByUserId);
