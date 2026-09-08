using Nexus.Developer.Core.DevelopmentControl;
using Xunit;

namespace Nexus.Developer.Core.Tests;

// SP1-M04: controlled, lenient classification of the Master Roadmap's free-text "Phase" token.
// No new column/property is added, descendants are never REQUIRED to carry a strategic phase, and
// legacy/freeform tokens are never rejected. These tests lock the classification vocabulary:
//   - roadmap phases P0..P5 are recognized generally (any node type);
//   - strategic-wave labels SP1/SP2/SP3 are recognized ONLY on a feature-level node; on any
//     descendant they are classified with StrategicWaveMisplaced = true and are NON-blocking;
//   - absent / legacy / freeform tokens classify as Legacy and never throw.
public class NodePhaseTests
{
    [Theory]
    [InlineData("P0")]
    [InlineData("P1")]
    [InlineData("P2")]
    [InlineData("P3")]
    [InlineData("P4")]
    [InlineData("P5")]
    public void Classify_RoadmapPhase_IsRecognized_OnAnyNodeType(string phase)
    {
        var onFeature = NodePhase.Classify(NodeType.Feature, phase);
        Assert.Equal(NodePhaseCategory.RoadmapPhase, onFeature.Category);
        Assert.Equal(phase, onFeature.Phase);
        Assert.True(onFeature.IsFeatureLevel);
        Assert.False(onFeature.StrategicWaveMisplaced);

        var onDescendant = NodePhase.Classify(NodeType.WorkItem, phase);
        Assert.Equal(NodePhaseCategory.RoadmapPhase, onDescendant.Category);
        Assert.Equal(phase, onDescendant.Phase);
        Assert.False(onDescendant.IsFeatureLevel);
        Assert.False(onDescendant.StrategicWaveMisplaced);
    }

    [Fact]
    public void Classify_LowercaseRoadmapToken_IsNormalized()
    {
        var result = NodePhase.Classify(NodeType.Task, "  p3  ");

        Assert.Equal(NodePhaseCategory.RoadmapPhase, result.Category);
        Assert.Equal("P3", result.Phase);
    }

    [Theory]
    [InlineData("SP1")]
    [InlineData("SP2")]
    [InlineData("SP3")]
    public void Classify_StrategicWaveLabel_OnAFeature_IsRecognized_NotMisplaced(string phase)
    {
        var result = NodePhase.Classify(NodeType.Feature, phase);

        Assert.Equal(NodePhaseCategory.StrategicWave, result.Category);
        Assert.Equal(phase, result.Phase);
        Assert.True(result.IsFeatureLevel);
        Assert.False(result.StrategicWaveMisplaced);
    }

    [Theory]
    [InlineData(NodeType.Release)]
    [InlineData(NodeType.Milestone)]
    [InlineData(NodeType.WorkItem)]
    [InlineData(NodeType.Task)]
    [InlineData(NodeType.Subtask)]
    public void Classify_StrategicWaveLabel_OnADescendant_IsInformationalAndNonBlocking(NodeType descendantType)
    {
        // A descendant carrying SP1/SP2/SP3 must never throw and never be rejected: the
        // authoritative workbook (and older files) keep reading unchanged. The classification
        // merely flags the label as strategically misplaced -- an observation, not a gate.
        var result = NodePhase.Classify(descendantType, "SP1");

        Assert.Equal(NodePhaseCategory.StrategicWave, result.Category);
        Assert.Equal("SP1", result.Phase);
        Assert.False(result.IsFeatureLevel);
        Assert.True(result.StrategicWaveMisplaced);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("Not Started")]
    [InlineData("blue")]
    [InlineData("Gate-4")]
    [InlineData("ABC-123")]
    public void Classify_AbsentLegacyOrFreeformToken_IsAcceptedAsLegacy(string? phase)
    {
        var result = NodePhase.Classify(NodeType.Feature, phase);

        Assert.Equal(NodePhaseCategory.Legacy, result.Category);
        Assert.False(result.StrategicWaveMisplaced);
    }

    [Fact]
    public void Classify_AbsentToken_NormalizesPhaseToNull()
    {
        Assert.Null(NodePhase.Classify(NodeType.Milestone, null).Phase);
        Assert.Null(NodePhase.Classify(NodeType.Milestone, "  ").Phase);
    }

    [Fact]
    public void Classify_FreeformToken_PreservesTheTrimmedRawText()
    {
        var result = NodePhase.Classify(NodeType.Layer, "  seed-idea  ");

        Assert.Equal(NodePhaseCategory.Legacy, result.Category);
        Assert.Equal("seed-idea", result.Phase);
    }
}
