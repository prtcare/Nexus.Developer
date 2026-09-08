namespace Nexus.Developer.Core.Dependencies;

// The three dependency kinds (M-07-2.1). Only Blocking edges participate in cycle
// detection and blocking-chain traversal; a Parallel or Informational edge never
// causes a cycle rejection and never appears in a blocking chain.
public enum WorkItemDependencyKind
{
    Blocking = 1,
    Parallel = 2,
    Informational = 3
}
