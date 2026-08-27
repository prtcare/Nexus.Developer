using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Milestones;

namespace Nexus.Developer.Application.Milestones.Queries.GetMilestone;

public sealed record GetMilestoneResult(
    MilestoneId MilestoneId,
    SubprojectId SubprojectId,
    string Name,
    string Description,
    DateTimeOffset? TargetDate,
    MilestoneStatus Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string Reference);
