namespace Nexus.Developer.Core.Dependencies;

// How far the upstream item must have advanced before the downstream item may
// proceed (per CHG-20260830-009). Null on the edge -- the original, unqualified
// semantics -- means the upstream must reach full completion; a set value means
// the downstream may proceed once the upstream reaches that earlier point instead.
// Only meaningful on Kind=Blocking edges; the constructor rejects a set value on
// any other kind.
public enum WorkItemDependencyRequiredState
{
    PlanStable = 1,
    ContractFrozen = 2,
    CommitAvailable = 3,
    PrOpen = 4,
    PrApproved = 5,
    Merged = 6
}
