using Nexus.Developer.Application.Milestones.Queries.GetMilestone;

namespace Nexus.Developer.Application.Milestones.Queries.ListMilestonesBySubproject;

public sealed record ListMilestonesBySubprojectResult(
    IReadOnlyList<GetMilestoneResult> Milestones);
