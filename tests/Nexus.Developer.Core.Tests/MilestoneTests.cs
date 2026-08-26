using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Milestones;
using Xunit;

namespace Nexus.Developer.Core.Tests;

public class MilestoneTests
{
    [Fact]
    public void Create_TargetDate_IsOptional()
    {
        var milestone = new Milestone(
            MilestoneId.New(), SubprojectId.New(), "Phase 1 exit", "d",
            targetDate: null, Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Null(milestone.TargetDate);
        Assert.Equal(MilestoneStatus.Planned, milestone.Status);
    }

    [Fact]
    public void Retarget_ChangesTargetDate()
    {
        var milestone = new Milestone(
            MilestoneId.New(), SubprojectId.New(), "M", "d", null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var newDate = DateTimeOffset.UtcNow.AddDays(30);

        milestone.Retarget(newDate);

        Assert.Equal(newDate, milestone.TargetDate);
    }

    [Theory]
    [InlineData(MilestoneLinkTargetType.Feature)]
    [InlineData(MilestoneLinkTargetType.Task)]
    [InlineData(MilestoneLinkTargetType.Subtask)]
    public void MilestoneLink_AcceptsOnlyOwnedHierarchyTargets(MilestoneLinkTargetType targetType)
    {
        var link = new MilestoneLink(
            MilestoneLinkId.New(), MilestoneId.New(), targetType, Guid.NewGuid(),
            Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(targetType, link.TargetType);
    }

    [Fact]
    public void MilestoneLink_RejectsUndefinedTargetType()
    {
        var invalid = (MilestoneLinkTargetType)999;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MilestoneLink(MilestoneLinkId.New(), MilestoneId.New(), invalid, Guid.NewGuid(),
                Guid.NewGuid(), DateTimeOffset.UtcNow));
    }
}
