using Nexus.Developer.Core.DevelopmentControl;
using Xunit;

namespace Nexus.Developer.Core.Tests;

// WI-07-0.2.4 stabilization (SP1-M00): the AtomicWriteResult classification layer over
// MutationResult<T>. Every plain-mutation terminal state must map to exactly one
// DevelopmentControlConcurrencyOutcome, and the two concurrency-layer-only outcomes
// (LockTimeout, IoFailure) must be constructible with the structured messages the contract
// promises.
public class AtomicWriteResultTests
{
    private static MutationResult<string> Success() =>
        new(true, "ok", false, null, Array.Empty<string>(), "ACT-1");

    private static MutationResult<string> Conflict() =>
        new(false, null, true, "current-node", Array.Empty<string>(), null);

    private static MutationResult<string> ValidationErrors() =>
        new(false, null, false, null, new[] { "envelope.Actor is required." }, null);

    private static MutationResult<string> NeitherTerminalState() =>
        new(false, null, false, null, Array.Empty<string>(), null);

    [Fact]
    public void FromMutation_MapsSuccessToSuccessOutcome()
    {
        var result = AtomicWriteResult<string>.FromMutation(Success(), TimeSpan.FromMilliseconds(12));

        Assert.Equal(DevelopmentControlConcurrencyOutcome.Success, result.Outcome);
        Assert.True(result.Success);
        Assert.Equal("ok", result.Value);
        Assert.Equal("ACT-1", result.ActivityLogEntryId);
        Assert.Equal(TimeSpan.FromMilliseconds(12), result.LockWait);
    }

    [Fact]
    public void FromMutation_MapsConflictToConcurrencyConflictOutcome()
    {
        var result = AtomicWriteResult<string>.FromMutation(Conflict(), null);

        Assert.Equal(DevelopmentControlConcurrencyOutcome.ConcurrencyConflict, result.Outcome);
        Assert.False(result.Success);
        Assert.Equal("current-node", result.ConflictDetails);
    }

    [Fact]
    public void FromMutation_MapsValidationErrorsToValidationFailureOutcome()
    {
        var result = AtomicWriteResult<string>.FromMutation(ValidationErrors(), null);

        Assert.Equal(DevelopmentControlConcurrencyOutcome.ValidationFailure, result.Outcome);
        Assert.False(result.Success);
        Assert.Equal("envelope.Actor is required.", Assert.Single(result.ValidationErrors));
    }

    [Fact]
    public void FromMutation_MapsNeitherTerminalStateToInvalidRequestOutcome()
    {
        var result = AtomicWriteResult<string>.FromMutation(NeitherTerminalState(), null);

        Assert.Equal(DevelopmentControlConcurrencyOutcome.InvalidRequest, result.Outcome);
        Assert.False(result.Success);
    }

    [Fact]
    public void LockTimeout_Factory_BuildsAStructuredTimeoutResult()
    {
        var result = AtomicWriteResult<string>.LockTimeout(TimeSpan.FromSeconds(2));

        Assert.Equal(DevelopmentControlConcurrencyOutcome.LockTimeout, result.Outcome);
        Assert.False(result.Success);
        Assert.Null(result.ConflictDetails);
        Assert.Contains("writer lock", Assert.Single(result.ValidationErrors));
        Assert.Equal(TimeSpan.FromSeconds(2), result.LockWait);
    }

    [Fact]
    public void IoFailure_Factory_BuildsAStructuredIoFailureResult()
    {
        var result = AtomicWriteResult<string>.IoFailure(TimeSpan.FromSeconds(1), "disk full");

        Assert.Equal(DevelopmentControlConcurrencyOutcome.IoFailure, result.Outcome);
        Assert.False(result.Success);
        Assert.Equal("disk full", Assert.Single(result.ValidationErrors));
    }
}
