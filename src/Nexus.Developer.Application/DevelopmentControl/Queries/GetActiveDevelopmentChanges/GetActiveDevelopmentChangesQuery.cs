namespace Nexus.Developer.Application.DevelopmentControl.Queries.GetActiveDevelopmentChanges;

// SP1-M05: read query for every change whose status is not Completed/Cancelled (the
// open register AGENTS.md's mandatory preflight compares against), newest first.
public sealed record GetActiveDevelopmentChangesQuery;
