namespace Nexus.Developer.Core.Common;

// Shared status lifecycle for Feature, Task and Subtask -- three parallel-shaped
// aggregates under the Workspace > Project > Subproject > Feature > Task > Subtask
// hierarchy (ADR-005). Kept as one enum deliberately: Phase 1 does not need three
// near-identical status sets, and a shared vocabulary makes cross-level roll-up
// (e.g. "is this Feature done" from its Tasks) simpler later.
public enum DevelopmentItemStatus
{
    New = 1,
    Active = 2,
    Blocked = 3,
    Completed = 4,
    Cancelled = 5
}
