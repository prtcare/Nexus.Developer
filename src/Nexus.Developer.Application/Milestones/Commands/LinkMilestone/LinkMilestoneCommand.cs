using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Milestones;

namespace Nexus.Developer.Application.Milestones.Commands.LinkMilestone;

public sealed record LinkMilestoneCommand(
    MilestoneId MilestoneId,
    MilestoneLinkTargetType TargetType,
    Guid TargetId,
    Guid LinkedByUserId);
