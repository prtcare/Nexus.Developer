using Nexus.ProductCore.Contracts;

namespace Nexus.Developer.Core.Scope;

/// <summary>
/// CHG-20260827-002 (M-06-1.2 Slice 3): declares Developer's own scope kinds, nested below
/// the shared Subproject trunk kind Layer 06 Product Core owns. Registered once at
/// Nexus.Developer.Api startup against that host's own process-local IScopeKindRegistry -
/// see ScopeKindRegistry's remarks in Nexus.ProductCore.Contracts for why this does not yet
/// resolve scope across Developer/Product Core/Experience as separate processes (an
/// explicit, known Phase 1 gap, not solved here).
///
/// Milestone is included, matching the M-06-1.2 acceptance criterion's language ("Developer
/// registers Milestone, Feature, WorkItem, Task as scope kinds below Subproject") updated for
/// the current domain model: WorkItem is retired (folds into Task, per ADR-005), and Issue is
/// deliberately NOT registered here - IssueLink's many-target attachment (Workspace | Project
/// | Subproject | Feature | Milestone | Task | Subtask | Chat | DevelopmentRun) does not fit
/// ScopeKindRegistration's single-parent-kind model, so Issue is out of scope for this
/// registry.
/// </summary>
public static class DeveloperScopeKinds
{
    public static readonly ScopeKind Feature = new("Feature");
    public static readonly ScopeKind Task = new("Task");
    public static readonly ScopeKind Subtask = new("Subtask");
    public static readonly ScopeKind Milestone = new("Milestone");

    public static void RegisterAll(IScopeKindRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register(new ScopeKindRegistration(Feature, WellKnownScopeKinds.Subproject, "Nexus.Developer"));
        registry.Register(new ScopeKindRegistration(Task, Feature, "Nexus.Developer"));
        registry.Register(new ScopeKindRegistration(Subtask, Task, "Nexus.Developer"));
        registry.Register(new ScopeKindRegistration(Milestone, WellKnownScopeKinds.Subproject, "Nexus.Developer"));
    }
}
