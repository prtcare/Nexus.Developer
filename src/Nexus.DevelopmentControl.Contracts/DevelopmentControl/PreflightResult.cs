namespace Nexus.Developer.Core.DevelopmentControl;

// RunPreflightAsync's result: the verdict plus explanatory detail. AGENTS.md says every
// negative verdict "names the rule and the conflicting change/node" -- Detail carries
// that narrative and Findings lists the individual named conflicts/dependencies/overlaps
// the verdict was derived from.
public sealed record PreflightResult(
    PreflightVerdict Verdict,
    string? Detail,
    IReadOnlyList<string> Findings);
