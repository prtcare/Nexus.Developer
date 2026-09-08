namespace Nexus.Developer.Core.DevelopmentRuns;

// SP1-M03 (Lane B1) "DevelopmentRun Phase-1 expansion": the structured result
// summary a run carries once it actually ends (WI-07-10.3 lifecycle). Outcome is the
// result-level vocabulary for the terminal half of DevelopmentRunStatus -- Success
// maps to Completed, Failed to Failed, Cancelled to Cancelled. It is deliberately NOT
// stored as a separate column: a run's single source of truth for how it ended is its
// Status, and DevelopmentRun derives this value object from Status + ResultSummary so
// the two vocabularies can never drift (no parallel status is invented). The Summary
// is caller-supplied free text only -- nothing here ever invents content, verification
// claims, or "evidence".
public enum DevelopmentRunOutcome
{
    Success = 1,
    Failed = 2,
    Cancelled = 3
}

public sealed class DevelopmentRunResult
{
    internal DevelopmentRunResult(
        DevelopmentRunOutcome outcome,
        string? summary)
    {
        Outcome = outcome;
        Summary = summary ?? string.Empty;
    }

    public DevelopmentRunOutcome Outcome { get; }

    public string Summary { get; }
}
