using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.DevelopmentRuns.Commands.StartDevelopmentRun;

// SP1-M05: NotStarted -> InProgress. The caller supplies the worker identity (who/what
// is executing the run) that SP1-M03's aggregate records.
public sealed record StartDevelopmentRunCommand(
    DevelopmentRunId DevelopmentRunId,
    string WorkerId,
    string WorkerType);
