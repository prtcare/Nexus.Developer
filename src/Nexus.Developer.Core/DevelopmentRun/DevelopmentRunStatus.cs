namespace Nexus.Developer.Core.DevelopmentRun;

// Phase 1 only ever creates NotStarted rows -- the DEVELOP action is visible but
// does not execute (WI-07-10.3.2 / M-07-10.3 acceptance: "clearly labelled as
// Phase 2 functionality... No field only the Phase 2 orchestrator uses is
// implemented"). The rest of the lifecycle is defined now so Phase 2 does not need
// a breaking schema change to start using it.
public enum DevelopmentRunStatus
{
    NotStarted = 1,
    Planned = 2,
    InProgress = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6
}
