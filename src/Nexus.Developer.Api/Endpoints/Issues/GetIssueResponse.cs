namespace Nexus.Developer.Api.Endpoints.Issues;

public sealed record GetIssueResponse(
    Guid IssueId,
    string Title,
    string Description,
    int Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string Reference);
