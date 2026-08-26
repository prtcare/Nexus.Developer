using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.Issues;

public interface IIssueRepository
    : IRepository<Issue, IssueId>
{
}
