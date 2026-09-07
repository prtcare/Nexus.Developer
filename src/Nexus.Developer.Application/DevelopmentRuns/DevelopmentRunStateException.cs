namespace Nexus.Developer.Application.DevelopmentRuns;

// SP1-M05: thrown by a lifecycle command handler when the requested transition is not
// legal for the run's current status (the SP1-M03 Core aggregate throws
// InvalidOperationException on an illegal transition). The endpoint maps this to 409
// Conflict -- the run is in a state that cannot accept the transition -- never an
// unhandled 500.
public sealed class DevelopmentRunStateException : Exception
{
    public DevelopmentRunStateException(string message)
        : base(message)
    {
    }
}
