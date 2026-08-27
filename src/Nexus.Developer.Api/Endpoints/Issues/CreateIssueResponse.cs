namespace Nexus.Developer.Api.Endpoints.Issues;

public sealed record CreateIssueResponse(
    Guid IssueId,
    string Title,
    string Reference);
