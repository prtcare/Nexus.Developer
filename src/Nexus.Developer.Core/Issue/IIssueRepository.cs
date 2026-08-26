using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.Issue;

public interface IIssueRepository
    : IRepository<Issue, IssueId>
{
}
