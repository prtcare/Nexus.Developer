namespace Nexus.Developer.Api.Endpoints.Issues;

public sealed record LinkIssueRequest(
    int TargetType,
    Guid TargetId,
    Guid LinkedByUserId);
