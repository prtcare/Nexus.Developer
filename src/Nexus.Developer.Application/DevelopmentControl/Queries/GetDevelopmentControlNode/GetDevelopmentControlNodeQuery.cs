namespace Nexus.Developer.Application.DevelopmentControl.Queries.GetDevelopmentControlNode;

// SP1-M05: read query for one current roadmap node by its human-authored node id
// (e.g. "WI-07-2.1.1" -- the Development Control identity scheme is a string, not a
// Guid; see Core NodeId remarks).
public sealed record GetDevelopmentControlNodeQuery(string NodeId);
