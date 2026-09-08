namespace Nexus.Developer.Core.DevelopmentControl;

// The five verdicts AGENTS.md's mandatory-preflight section requires, exactly:
// CLEAR, DEPENDENCY FOUND, OVERLAP FOUND, CONFLICT FOUND, ARCHITECTURE CONFLICT.
// A preflight compares a declared change against every change whose status is not
// Completed or Cancelled; CONFLICT FOUND and ARCHITECTURE CONFLICT stop the work.
public enum PreflightVerdict
{
    Clear = 1,
    DependencyFound = 2,
    OverlapFound = 3,
    ConflictFound = 4,
    ArchitectureConflict = 5
}
