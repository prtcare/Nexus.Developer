namespace Nexus.Developer.Core.Milestones;

// A Milestone links to Feature/Task/Subtask only -- it is a delivery grouping over
// the owned hierarchy, not a universal attachment point (that is Issue's role).
public enum MilestoneLinkTargetType
{
    Feature = 1,
    Task = 2,
    Subtask = 3
}
