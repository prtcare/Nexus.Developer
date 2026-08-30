using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.DevelopmentRuns;

namespace Nexus.Developer.Application.DevelopmentRuns.Commands.CreateDevelopmentRun;

public sealed record CreateDevelopmentRunResult(
    DevelopmentRunId DevelopmentRunId,
    DevelopmentRunTargetType TargetType,
    Guid TargetId,
    string Reference);
