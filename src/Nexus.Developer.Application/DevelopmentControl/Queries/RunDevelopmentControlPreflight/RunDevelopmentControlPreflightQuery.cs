using Nexus.Developer.Core.DevelopmentControl;

namespace Nexus.Developer.Application.DevelopmentControl.Queries.RunDevelopmentControlPreflight;

// SP1-M05: the mandatory-preflight check (AGENTS.md) comparing a declared change
// against every open change. A check, not a mutation -- no envelope.
public sealed record RunDevelopmentControlPreflightQuery(PreflightDeclaration Declaration);
