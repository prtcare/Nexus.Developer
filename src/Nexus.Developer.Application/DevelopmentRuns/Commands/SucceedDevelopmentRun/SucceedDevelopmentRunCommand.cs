using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.DevelopmentRuns.Commands.SucceedDevelopmentRun;

// SP1-M05: InProgress -> Completed. The caller MUST supply a non-blank summary of what
// actually happened; the aggregate throws on a blank summary.
public sealed record SucceedDevelopmentRunCommand(
    DevelopmentRunId DevelopmentRunId,
    string Summary);
