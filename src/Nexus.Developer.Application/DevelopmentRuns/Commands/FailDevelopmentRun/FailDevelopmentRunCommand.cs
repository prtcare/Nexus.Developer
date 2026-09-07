using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.DevelopmentRuns.Commands.FailDevelopmentRun;

// SP1-M05: InProgress -> Failed. The summary is the caller's error reason and is
// optional.
public sealed record FailDevelopmentRunCommand(
    DevelopmentRunId DevelopmentRunId,
    string? Summary = null);
