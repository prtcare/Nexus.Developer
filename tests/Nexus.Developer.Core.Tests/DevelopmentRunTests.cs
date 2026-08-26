using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.DevelopmentRuns;
using Xunit;

namespace Nexus.Developer.Core.Tests;

public class DevelopmentRunTests
{
    [Fact]
    public void Create_StartsNotStarted_WithAllPhase2PlaceholdersNull()
    {
        var run = new DevelopmentRun(
            DevelopmentRunId.New(), DevelopmentRunTargetType.Feature, Guid.NewGuid(),
            Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(DevelopmentRunStatus.NotStarted, run.Status);
        Assert.Null(run.PlanId);
        Assert.Null(run.PromptId);
        Assert.Null(run.ResultId);
        Assert.Null(run.ReportId);
        Assert.Null(run.CheckSetId);
        Assert.Null(run.VerificationId);
    }

    [Theory]
    [InlineData(DevelopmentRunTargetType.Feature)]
    [InlineData(DevelopmentRunTargetType.Task)]
    [InlineData(DevelopmentRunTargetType.Issue)]
    public void Create_AcceptsEveryAllowedTargetType(DevelopmentRunTargetType targetType)
    {
        var run = new DevelopmentRun(
            DevelopmentRunId.New(), targetType, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(targetType, run.TargetType);
    }

    [Fact]
    public void Create_RejectsUndefinedTargetType()
    {
        var invalid = (DevelopmentRunTargetType)999;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DevelopmentRun(DevelopmentRunId.New(), invalid, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Restore_CanRehydratePhase2Placeholders_ForFutureUse()
    {
        var planId = Guid.NewGuid();
        var verificationId = Guid.NewGuid();

        var run = DevelopmentRun.Restore(
            DevelopmentRunId.New(), DevelopmentRunTargetType.Task, Guid.NewGuid(),
            DevelopmentRunStatus.InProgress, Guid.NewGuid(), DateTimeOffset.UtcNow, "RUN-000123",
            planId: planId, promptId: null, resultId: null, reportId: null,
            checkSetId: null, verificationId: verificationId);

        Assert.Equal(planId, run.PlanId);
        Assert.Equal(verificationId, run.VerificationId);
        Assert.Equal(DevelopmentRunStatus.InProgress, run.Status);
    }
}
