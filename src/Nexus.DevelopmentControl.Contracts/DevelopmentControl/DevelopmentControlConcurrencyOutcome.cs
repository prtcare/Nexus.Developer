namespace Nexus.Developer.Core.DevelopmentControl;

// WI-07-0.2.4: the explicit, machine-readable outcome vocabulary for Development Control
// concurrent writes, layered OVER the existing MutationResult<T> semantics rather than
// replacing them. A plain mutation either succeeds, conflicts (optimistic concurrency or a
// state mismatch), or fails validation -- all representable in MutationResult today. The
// concurrency layer additionally introduces the lock/IO outcomes the store contract's
// result type cannot express (LockTimeout, IoFailure), and classifies the existing cases
// into one vocabulary so a caller gets a single switch statement for every terminal
// outcome of a guarded write. Validation details and NotFound stay primary on the
// underlying MutationResult; this enum is the classification layer on top.
public enum DevelopmentControlConcurrencyOutcome
{
    Success = 1,
    ConcurrencyConflict = 2,
    LockTimeout = 3,
    ValidationFailure = 4,
    NotFound = 5,
    InvalidRequest = 6,
    IoFailure = 7
}
