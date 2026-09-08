using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.DevelopmentRuns;

// SP1-M05: thrown by a lifecycle command handler when the DevelopmentRun to transition
// cannot be resolved by id. The endpoint maps this to 404 Not Found rather than an
// unhandled 500.
public sealed class DevelopmentRunNotFoundException : Exception
{
    public DevelopmentRunNotFoundException(DevelopmentRunId developmentRunId)
        : base($"The development run '{developmentRunId}' does not exist.")
    {
        DevelopmentRunId = developmentRunId;
    }

    public DevelopmentRunId DevelopmentRunId { get; }
}
