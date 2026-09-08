namespace Nexus.Developer.Application.DevelopmentControl.Queries.GetDevelopmentControlState;

// SP1-M05: read query for the overall Development Control snapshot (workbook/roadmap
// versions, node/change/finding counts, root node). No parameters -- the state is the
// whole control plane.
public sealed record GetDevelopmentControlStateQuery;
