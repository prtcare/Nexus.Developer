using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.Milestones.Commands.CreateMilestone;

public sealed record CreateMilestoneCommand(
    SubprojectId SubprojectId,
    string Name,
    string Description,
    DateTimeOffset? TargetDate,
    Guid CreatedByUserId);
