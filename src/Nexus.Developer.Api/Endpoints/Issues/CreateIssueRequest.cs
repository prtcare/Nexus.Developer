namespace Nexus.Developer.Api.Endpoints.Issues;

public sealed record CreateIssueRequest(
    string Title,
    string? Description,
    Guid CreatedByUserId);
