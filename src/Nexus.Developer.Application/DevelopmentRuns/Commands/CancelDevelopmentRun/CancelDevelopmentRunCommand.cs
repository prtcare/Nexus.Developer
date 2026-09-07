using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.DevelopmentRuns.Commands.CancelDevelopmentRun;

// SP1-M05: NotStarted | InProgress -> Cancelled. The caller-supplied summary is
// optional; when omitted the aggregate records its standard cancellation text.
public sealed record CancelDevelopmentRunCommand(
    DevelopmentRunId DevelopmentRunId,
    string? Summary = null);
