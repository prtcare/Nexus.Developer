using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.Milestones.Commands.CreateMilestone;

public sealed record CreateMilestoneResult(
    MilestoneId MilestoneId,
    string Name,
    string Reference);
