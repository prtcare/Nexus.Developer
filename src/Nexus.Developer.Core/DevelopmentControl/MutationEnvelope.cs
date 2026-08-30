namespace Nexus.Developer.Core.DevelopmentControl;

// Provenance and concurrency context carried by every mutating operation, alongside its
// operation-specific parameters. ExpectedRowVersion is the optimistic-concurrency token:
// null on Create, otherwise the RowVersion the caller last read; if the store's current
// version differs, the mutation returns Conflict instead of applying.
// ChangeId is the governed change this mutation belongs to (CHG-...); IdempotencyKey lets
// a retried mutation be recognized and deduplicated; Reason records "who set it and why"
// (AGENTS.md requires a manual status override to record both). The remaining fields
// carry the source/session/chat provenance the Activity Log records.
public sealed record MutationEnvelope(
    int? ExpectedRowVersion,
    ActorRef Actor,
    string Source,
    string? ChatPlatform,
    string? SessionId,
    string? PromptId,
    string? ChangeId,
    string? CorrelationId,
    string? IdempotencyKey,
    string? Reason);
