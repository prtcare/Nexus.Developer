using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Features;
using Xunit;

namespace Nexus.Developer.Core.Tests;

public class FeatureTests
{
    [Fact]
    public void Create_TrimsTitleAndDescription_AndStartsNew()
    {
        var feature = new Feature(
            FeatureId.New(),
            SubprojectId.New(),
            "  Developer Chat  ",
            "  discussion to structured object  ",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        Assert.Equal("Developer Chat", feature.Title);
        Assert.Equal("discussion to structured object", feature.Description);
        Assert.Equal(DevelopmentItemStatus.New, feature.Status);
        Assert.Equal(string.Empty, feature.Reference);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ThrowsOnBlankTitle(string title)
    {
        Assert.Throws<ArgumentException>(() =>
            new Feature(FeatureId.New(), SubprojectId.New(), title, "d", Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ChangeDescription_Null_BecomesEmptyString()
    {
        var feature = new Feature(FeatureId.New(), SubprojectId.New(), "F", "d", Guid.NewGuid(), DateTimeOffset.UtcNow);

        feature.ChangeDescription(null!);

        Assert.Equal(string.Empty, feature.Description);
    }

    [Fact]
    public void Restore_PreservesPersistedReferenceAndStatus()
    {
        var id = FeatureId.New();
        var subprojectId = SubprojectId.New();
        var createdAt = DateTimeOffset.UtcNow;
        var createdBy = Guid.NewGuid();

        var feature = Feature.Restore(
            id, subprojectId, "Existing", "desc", DevelopmentItemStatus.Active,
            createdBy, createdAt, "FEA-00000042");

        Assert.Equal(id, feature.Id);
        Assert.Equal(subprojectId, feature.SubprojectId);
        Assert.Equal(DevelopmentItemStatus.Active, feature.Status);
        Assert.Equal("FEA-00000042", feature.Reference);
        Assert.Equal(createdBy, feature.CreatedByUserId);
        Assert.Equal(createdAt, feature.CreatedAt);
        Assert.Null(feature.ParentFeatureId);
    }

    [Fact]
    public void Create_IsRootByDefault()
    {
        var feature = new Feature(FeatureId.New(), SubprojectId.New(), "F", "d", Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Null(feature.ParentFeatureId);
    }

    [Fact]
    public void Create_WithParentFeatureId_SetsParent()
    {
        var subprojectId = SubprojectId.New();
        var parentId = FeatureId.New();

        var feature = new Feature(FeatureId.New(), subprojectId, "Child", "d", Guid.NewGuid(), DateTimeOffset.UtcNow, parentId);

        Assert.Equal(parentId, feature.ParentFeatureId);
    }

    [Fact]
    public void Create_WhenParentFeatureIdEqualsOwnId_Throws()
    {
        var id = FeatureId.New();

        Assert.Throws<ArgumentException>(() =>
            new Feature(id, SubprojectId.New(), "F", "d", Guid.NewGuid(), DateTimeOffset.UtcNow, id));
    }

    [Fact]
    public void Restore_RoundTripsParentFeatureId()
    {
        var id = FeatureId.New();
        var subprojectId = SubprojectId.New();
        var parentId = FeatureId.New();
        var createdAt = DateTimeOffset.UtcNow;
        var createdBy = Guid.NewGuid();

        var feature = Feature.Restore(
            id, subprojectId, "Existing", "desc", DevelopmentItemStatus.Active,
            createdBy, createdAt, "FEA-00000042", parentId);

        Assert.Equal(parentId, feature.ParentFeatureId);
    }

    [Fact]
    public void SetParent_Null_PromotesChildToRoot()
    {
        var subprojectId = SubprojectId.New();
        var parentId = FeatureId.New();
        var child = new Feature(FeatureId.New(), subprojectId, "Child", "d", Guid.NewGuid(), DateTimeOffset.UtcNow, parentId);

        child.SetParent(parent: null);

        Assert.Null(child.ParentFeatureId);
    }

    [Fact]
    public void SetParent_OnRootToNull_IsNoOp()
    {
        var feature = new Feature(FeatureId.New(), SubprojectId.New(), "F", "d", Guid.NewGuid(), DateTimeOffset.UtcNow);

        feature.SetParent(parent: null);

        Assert.Null(feature.ParentFeatureId);
    }

    [Fact]
    public void SetParent_Self_ThrowsAndDoesNotChange()
    {
        var feature = new Feature(FeatureId.New(), SubprojectId.New(), "F", "d", Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(() => feature.SetParent(feature));

        Assert.Null(feature.ParentFeatureId);
    }

    [Fact]
    public void SetParent_ParentInDifferentSubproject_ThrowsAndDoesNotChange()
    {
        var subprojectA = SubprojectId.New();
        var subprojectB = SubprojectId.New();
        var by = Guid.NewGuid();
        var feature = new Feature(FeatureId.New(), subprojectA, "Child", "d", by, DateTimeOffset.UtcNow);
        var otherSubprojectFeature = new Feature(FeatureId.New(), subprojectB, "Parent", "d", by, DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(() => feature.SetParent(otherSubprojectFeature));

        Assert.Null(feature.ParentFeatureId);
    }

    [Fact]
    public void SetParent_ValidParent_SetsParentFeatureId()
    {
        var subprojectId = SubprojectId.New();
        var by = Guid.NewGuid();
        var child = new Feature(FeatureId.New(), subprojectId, "Child", "d", by, DateTimeOffset.UtcNow);
        var parent = new Feature(FeatureId.New(), subprojectId, "Parent", "d", by, DateTimeOffset.UtcNow);

        child.SetParent(parent);

        Assert.Equal(parent.Id, child.ParentFeatureId);
    }

    [Fact]
    public void SetParent_ToAFeatureThatItselfHasAParent_IsAllowed_MultiLevelHierarchy()
    {
        // D04: a child may itself have children (multi-level). Only ancestor
        // cycles are forbidden, and that rule is enforced at the write boundary.
        var subprojectId = SubprojectId.New();
        var by = Guid.NewGuid();
        var root = new Feature(FeatureId.New(), subprojectId, "Root", "d", by, DateTimeOffset.UtcNow);
        var middle = new Feature(FeatureId.New(), subprojectId, "Middle", "d", by, DateTimeOffset.UtcNow, root.Id);
        var leaf = new Feature(FeatureId.New(), subprojectId, "Leaf", "d", by, DateTimeOffset.UtcNow);

        leaf.SetParent(middle);

        Assert.Equal(middle.Id, leaf.ParentFeatureId);
        Assert.Equal(root.Id, middle.ParentFeatureId);
    }

    [Fact]
    public void Create_WithSourceRoadmapNodeId_TrimsAndSets_IdentityUnchanged()
    {
        var id = FeatureId.New();
        var subprojectId = SubprojectId.New();
        var createdAt = DateTimeOffset.UtcNow;

        // WU-02 narrow bridge: an optional roadmap-ledger NodeId string may tag a
        // Feature at creation. It is traceability only -- it must never change the
        // aggregate's own Guid identity.
        var feature = new Feature(
            id, subprojectId, "F", "d", Guid.NewGuid(), createdAt,
            parentFeatureId: null, sourceRoadmapNodeId: "  F-07-10  ");

        Assert.Equal("F-07-10", feature.SourceRoadmapNodeId);
        Assert.Equal(id, feature.Id);
        Assert.Equal(subprojectId, feature.SubprojectId);
        Assert.Equal(createdAt, feature.CreatedAt);
        Assert.Equal(string.Empty, feature.Reference);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_BlankSourceRoadmapNodeId_IsNull(string? sourceRoadmapNodeId)
    {
        var feature = new Feature(
            FeatureId.New(), SubprojectId.New(), "F", "d", Guid.NewGuid(), DateTimeOffset.UtcNow,
            sourceRoadmapNodeId: sourceRoadmapNodeId);

        Assert.Null(feature.SourceRoadmapNodeId);
    }

    [Fact]
    public void Create_SourceRoadmapNodeId_NullByDefault()
    {
        // No Feature today is roadmap-originated; the field is dormant until the
        // roadmap importer (WI-07-1.1.3) exists.
        var feature = new Feature(FeatureId.New(), SubprojectId.New(), "F", "d", Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Null(feature.SourceRoadmapNodeId);
    }

    [Fact]
    public void Restore_RoundTripsSourceRoadmapNodeId()
    {
        var id = FeatureId.New();
        var feature = Feature.Restore(
            id, SubprojectId.New(), "Existing", "desc", DevelopmentItemStatus.Active,
            Guid.NewGuid(), DateTimeOffset.UtcNow, "FEA-00000042",
            parentFeatureId: null, sourceRoadmapNodeId: "F-07-10");

        Assert.Equal("F-07-10", feature.SourceRoadmapNodeId);
        Assert.Equal(id, feature.Id);
    }

    [Fact]
    public void Restore_SourceRoadmapNodeId_IsNull_ForOrdinaryFeatures()
    {
        var feature = Feature.Restore(
            FeatureId.New(), SubprojectId.New(), "Existing", "desc", DevelopmentItemStatus.Active,
            Guid.NewGuid(), DateTimeOffset.UtcNow, "FEA-00000043");

        Assert.Null(feature.SourceRoadmapNodeId);
    }
}
