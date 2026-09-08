namespace Nexus.Developer.Core.DevelopmentControl;

// Overall Development Control state, grounded in the workbook's Control Center sheet
// (which tracks current-node/milestone/work-item/blocked counts plus the workbook
// version). The architecture note enumerated the row-level DTOs but not an aggregate
// snapshot; this record fills that gap so GetControlStateAsync has a concrete shape.
// RootNodeId is the top Layer node of the roadmap tree, when one exists.
public sealed record ControlState(
    string WorkbookVersion,
    string ControlBaselineVersion,
    string RoadmapVersion,
    NodeId? RootNodeId,
    int CurrentNodeCount,
    int MilestoneCount,
    int WorkItemCount,
    int BlockedNodeCount,
    int ActiveChangeCount,
    int OpenAuditFindingCount,
    DateTimeOffset? LastUpdatedAt);
