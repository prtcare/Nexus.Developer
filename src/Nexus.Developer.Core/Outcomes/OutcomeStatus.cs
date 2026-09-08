namespace Nexus.Developer.Core.Outcomes;

// An Outcome is the actual result/value achieved by a governed execution. Its status is a
// Developer-side fact -- Pending until the outcome is achieved, then Achieved -- and is NOT
// an Assurance PASS and never carries a verdict. No Cancelled state: a not-yet-achieved
// Outcome simply stays Pending; the minimum model records achievement only (the brief's
// anti-bloat mandate -- see Outcome class remarks).
public enum OutcomeStatus
{
    Pending = 1,
    Achieved = 2
}
