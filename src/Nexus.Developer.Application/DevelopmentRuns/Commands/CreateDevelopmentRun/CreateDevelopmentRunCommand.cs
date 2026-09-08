using Nexus.Developer.Core.DevelopmentRuns;

namespace Nexus.Developer.Application.DevelopmentRuns.Commands.CreateDevelopmentRun;

public sealed record CreateDevelopmentRunCommand(
    DevelopmentRunTargetType TargetType,
    Guid TargetId,
    Guid CreatedByUserId);
