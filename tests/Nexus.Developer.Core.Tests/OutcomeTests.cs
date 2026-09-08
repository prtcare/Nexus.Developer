using System.Collections;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Outcomes;
using Xunit;

namespace Nexus.Developer.Core.Tests;

// P1-WAVE-05A Lane E. Pure aggregate tests for the minimum Outcome model -- see the class
// remarks on Outcome for why persistence is deliberately not exercised here.
public class OutcomeTests
{
    private static Outcome NewOutcome(
        string? productId = "PRD-1",
        string? description = "Roadmap projection verified end-to-end.",
        IEnumerable<FeatureId>? relatedFeatures = null)
        => new(
            OutcomeId.New(),
            productId!,
            description!,
            successCriteria: "Focused + combined Developer tests green.",
            createdByUserId: Guid.NewGuid(),
            createdAt: DateTimeOffset.UtcNow,
            relatedFeatureIds: relatedFeatures ?? [FeatureId.New()]);

    [Fact]
    public void Create_StartsPending_WithNullAchievedAtNoEvidenceAndEmptyReference()
    {
        var outcome = NewOutcome();

        Assert.Equal(OutcomeStatus.Pending, outcome.Status);
        Assert.Null(outcome.AchievedAt);
        Assert.Empty(outcome.AssuranceEvidenceIds);
        Assert.Equal(string.Empty, outcome.Reference);
        Assert.NotEqual(Guid.Empty, outcome.Id.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RequiresProductId(string? productId)
    {
        Assert.ThrowsAny<ArgumentException>(() => NewOutcome(productId: productId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RequiresDescription(string? description)
    {
        Assert.ThrowsAny<ArgumentException>(() => NewOutcome(description: description));
    }

    [Fact]
    public void Create_TrimsProductIdDescriptionAndSuccessCriteria()
    {
        var outcome = new Outcome(
            OutcomeId.New(),
            "  PRD-1  ",
            "  Verified the bridge.  ",
            "  All green  ",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            [FeatureId.New()]);

        Assert.Equal("PRD-1", outcome.ProductId);
        Assert.Equal("Verified the bridge.", outcome.Description);
        Assert.Equal("All green", outcome.SuccessCriteria);
    }

    [Fact]
    public void Create_BlankSuccessCriteria_BecomesNull()
    {
        var outcome = new Outcome(
            OutcomeId.New(),
            "PRD-1",
            "description",
            "   ",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            []);

        Assert.Null(outcome.SuccessCriteria);
    }

    [Fact]
    public void Create_RelatedFeatureIds_RetainedDistinct()
    {
        var f1 = FeatureId.New();
        var f2 = FeatureId.New();
        var outcome = NewOutcome(relatedFeatures: new[] { f1, f2, f1 });

        Assert.Equal(2, outcome.RelatedFeatureIds.Count);
        Assert.Contains(f1, outcome.RelatedFeatureIds);
        Assert.Contains(f2, outcome.RelatedFeatureIds);
    }

    [Fact]
    public void Create_RelatedFeatureIds_AreReadOnly()
    {
        var outcome = NewOutcome();

        Assert.Throws<NotSupportedException>(
            () => ((IList<FeatureId>)outcome.RelatedFeatureIds).Add(FeatureId.New()));
    }

    [Fact]
    public void Create_RejectsDefaultFeatureId()
    {
        Assert.Throws<ArgumentException>(
            () => NewOutcome(relatedFeatures: new[] { default(FeatureId) }));
    }

    [Fact]
    public void Achieve_TransitionsToAchieved_AndStampsUtcNow()
    {
        var outcome = NewOutcome();
        var before = DateTimeOffset.UtcNow;

        outcome.Achieve(["ev-1"]);

        var after = DateTimeOffset.UtcNow;
        Assert.Equal(OutcomeStatus.Achieved, outcome.Status);
        Assert.NotNull(outcome.AchievedAt);
        Assert.InRange(outcome.AchievedAt!.Value, before, after);
    }

    [Fact]
    public void Achieve_WithCallerSuppliedTimestamp_UsesExactValue()
    {
        var outcome = NewOutcome();
        var supplied = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

        outcome.Achieve([], achievedAt: supplied);

        Assert.Equal(supplied, outcome.AchievedAt);
    }

    [Fact]
    public void Achieve_RecordsTrimmedDistinctEvidenceReferences()
    {
        var outcome = NewOutcome();

        outcome.Achieve(["  ev-1  ", "ev-2", "ev-1"]);

        Assert.Equal(2, outcome.AssuranceEvidenceIds.Count);
        Assert.Contains("ev-1", outcome.AssuranceEvidenceIds);
        Assert.Contains("ev-2", outcome.AssuranceEvidenceIds);
        Assert.DoesNotContain("  ev-1  ", outcome.AssuranceEvidenceIds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Achieve_RejectsBlankEvidenceReference(string? evidenceId)
    {
        var outcome = NewOutcome();

        Assert.Throws<ArgumentException>(() => outcome.Achieve([evidenceId!]));
    }

    [Fact]
    public void Achieve_OnAlreadyAchievedOutcome_Throws()
    {
        var outcome = NewOutcome();
        outcome.Achieve(["ev-1"]);

        Assert.Throws<InvalidOperationException>(() => outcome.Achieve(["ev-2"]));
    }

    [Fact]
    public void Restore_AchievedOutcome_RequiresAchievedAt()
    {
        var f = FeatureId.New();

        Assert.Throws<ArgumentException>(() =>
            Outcome.Restore(
                OutcomeId.New(),
                "PRD-1",
                "description",
                null,
                OutcomeStatus.Achieved,
                achievedAt: null,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                "OUT-000001",
                [f],
                ["ev-1"]));
    }

    [Fact]
    public void Restore_PendingOutcome_RejectsAchievedAt()
    {
        var f = FeatureId.New();

        Assert.Throws<ArgumentException>(() =>
            Outcome.Restore(
                OutcomeId.New(),
                "PRD-1",
                "description",
                null,
                OutcomeStatus.Pending,
                achievedAt: DateTimeOffset.UtcNow,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                "OUT-000001",
                [f],
                []));
    }

    [Fact]
    public void Restore_AchievedOutcome_RoundTripsFullState()
    {
        var f1 = FeatureId.New();
        var f2 = FeatureId.New();
        var achievedAt = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

        var outcome = Outcome.Restore(
            OutcomeId.New(),
            "  PRD-1  ",
            "  description  ",
            "  criteria  ",
            OutcomeStatus.Achieved,
            achievedAt,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "OUT-000001",
            [f1, f2],
            ["ev-1"]);

        Assert.Equal("PRD-1", outcome.ProductId);
        Assert.Equal(OutcomeStatus.Achieved, outcome.Status);
        Assert.Equal(achievedAt, outcome.AchievedAt);
        Assert.Equal("OUT-000001", outcome.Reference);
        Assert.Equal(2, outcome.RelatedFeatureIds.Count);
        Assert.Equal(["ev-1"], outcome.AssuranceEvidenceIds);
    }

    [Fact]
    public void Restore_RejectsUndefinedStatus()
    {
        var f = FeatureId.New();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Outcome.Restore(
                OutcomeId.New(),
                "PRD-1",
                "description",
                null,
                (OutcomeStatus)999,
                null,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                "OUT-000001",
                [f],
                []));
    }

    // The authority boundary, encoded as a surface assertion: an Outcome must expose no way
    // to self-declare an Assurance verdict and no way to write/create/mutate evidence -- it
    // can only reference evidence that already exists (08 ASSURANCE stays authoritative).
    [Fact]
    public void PublicSurface_ExposesNoVerdictAndNoEvidenceWrite()
    {
        var forbiddenNames = new[]
        {
            "Pass", "Fail", "Verdict", "WriteEvidence", "CreateEvidence", "AddEvidence",
            "UpdateEvidence", "DeleteEvidence", "EvidenceStore", "RateEvidence"
        };

        var offending = typeof(Outcome)
            .GetMembers()
            .Select(m => m.Name)
            .Where(name => forbiddenNames.Any(f => name.Contains(f, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.Empty(offending);
    }
}
