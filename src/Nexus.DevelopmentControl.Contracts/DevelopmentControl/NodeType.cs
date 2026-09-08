namespace Nexus.Developer.Core.DevelopmentControl;

// Kind of a Development Control node, matching the Master Roadmap's "Node Type" column
// (control/ROADMAP_LEDGER.csv confirms Layer/Feature/Milestone/WorkItem/Task/Subtask in
// use). Release is a forward-compatibility value from the architecture note: no current
// roadmap row uses it, but future release-shaped nodes will, so the enum reserves it.
public enum NodeType
{
    Layer = 1,
    Release = 2,
    Feature = 3,
    Milestone = 4,
    WorkItem = 5,
    Task = 6,
    Subtask = 7
}
