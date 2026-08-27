using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.Issues.Commands.CreateIssue;

public sealed record CreateIssueResult(
    IssueId IssueId,
    string Title,
    string Reference);
