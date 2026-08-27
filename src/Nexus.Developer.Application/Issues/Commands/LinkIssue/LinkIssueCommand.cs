using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Issues;

namespace Nexus.Developer.Application.Issues.Commands.LinkIssue;

public sealed record LinkIssueCommand(
    IssueId IssueId,
    IssueLinkTargetType TargetType,
    Guid TargetId,
    Guid LinkedByUserId);
