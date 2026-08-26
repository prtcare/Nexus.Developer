using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Feature;
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
    }
}
