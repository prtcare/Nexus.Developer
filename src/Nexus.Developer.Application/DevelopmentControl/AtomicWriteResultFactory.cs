using Nexus.Developer.Core.DevelopmentControl;

namespace Nexus.Developer.Application.DevelopmentControl;

// SP1-M05: small internal factories for the synthetic (non-store) AtomicWriteResult
// states a governed-mutation handler returns BEFORE invoking the guarded store -- a
// malformed node id (InvalidRequest) or a node that does not exist (NotFound). Every
// other terminal state comes from the store/guard itself via AtomicWriteResult.FromMutation.
internal static class AtomicWriteResultFactory
{
    public static AtomicWriteResult<T> NotFound<T>(string nodeId, TimeSpan? lockWait = null) where T : class
        => new(
            DevelopmentControlConcurrencyOutcome.NotFound,
            false,
            default,
            null,
            new[] { $"No current version of node '{nodeId}'." },
            null,
            lockWait);

    public static AtomicWriteResult<T> InvalidRequest<T>(IReadOnlyList<string> errors) where T : class
        => new(
            DevelopmentControlConcurrencyOutcome.InvalidRequest,
            false,
            default,
            null,
            errors,
            null,
            null);
}
