using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Issues;

namespace Nexus.Developer.Application.Issues.Queries.GetIssue;

public sealed record GetIssueResult(
    IssueId IssueId,
    string Title,
    string Description,
    IssueStatus Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string Reference);
