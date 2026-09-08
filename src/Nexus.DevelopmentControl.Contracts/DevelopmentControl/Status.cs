namespace Nexus.Developer.Core.DevelopmentControl;

// Lifecycle status of a Development Control node, matching the Master Roadmap's
// "Status" column. Superseded is included because the workbook already uses it on real
// rows today (e.g. M-07-1.1 / M-07-1.2, and the 23 nodes marked Superseded -- never
// deleted -- by the Phase 1/2 redefinition) even though the architecture note's list
// predates that usage. "Never delete roadmap history" (AGENTS.md) is honored by
// retiring a node to Superseded rather than removing it.
public enum Status
{
    Proposed = 1,
    Planned = 2,
    Ready = 3,
    InProgress = 4,
    Blocked = 5,
    InReview = 6,
    Completed = 7,
    Cancelled = 8,
    Deferred = 9,
    Obsolete = 10,
    Superseded = 11
}
