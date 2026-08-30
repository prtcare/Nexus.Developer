namespace Nexus.Developer.Core.DevelopmentControl;

// Result of ValidateControlStoreAsync: whether the store is internally consistent and a
// list of the concrete problems found when it is not. A store is consistent when, among
// other invariants, every ParentId resolves to an existing node, exactly one current
// version exists per NodeId, dependencies resolve, and no two siblings share a SortKey.
public sealed record ValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors);
