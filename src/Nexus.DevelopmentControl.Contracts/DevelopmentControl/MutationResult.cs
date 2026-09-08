namespace Nexus.Developer.Core.DevelopmentControl;

// Standard result envelope for every mutating store operation. Exactly one of the
// terminal states holds:
//   Success   -> Value carries the resulting entity (current Node or ActivityLogEntry).
//   Conflict  -> an optimistic-concurrency or state mismatch; ConflictDetails carries
//                the current row as the entity the operation concerns (a Node for node
//                operations, an ActiveChange where a change is the subject), so the
//                caller can see what it raced against.
//   neither   -> validation failures; ValidationErrors names each one.
// ActivityLogEntryId is set when the operation wrote an activity entry, so callers can
// correlate the mutation back to the append-only log.
// T is the entity the operation concerns: Node for node mutations, ActivityLogEntry for
// activity mutations.
public sealed record MutationResult<T>(
    bool Success,
    T? Value,
    bool Conflict,
    object? ConflictDetails,
    IReadOnlyList<string> ValidationErrors,
    string? ActivityLogEntryId);
