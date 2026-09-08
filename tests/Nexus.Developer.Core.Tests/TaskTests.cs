using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;
using Xunit;
using DeveloperTask = Nexus.Developer.Core.Tasks.Task;

namespace Nexus.Developer.Core.Tests;

public class TaskTests
{
    [Fact]
    public void Create_StartsNew_AndHasNoMigrationOrigin()
    {
        var task = new DeveloperTask(
            TaskId.New(), FeatureId.New(), "Wire up chat scope", "d",
            Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(DevelopmentItemStatus.New, task.Status);
        Assert.Null(task.MigratedFromWorkItemId);
    }

    [Fact]
    public void CreateFromWorkItemMigration_RecordsOriginId_NotSilently()
    {
        var originalWorkItemId = Guid.NewGuid();

        var task = DeveloperTask.CreateFromWorkItemMigration(
            TaskId.New(), FeatureId.New(), "Migrated item", "d",
            Guid.NewGuid(), DateTimeOffset.UtcNow, originalWorkItemId);

        Assert.Equal(originalWorkItemId, task.MigratedFromWorkItemId);
    }

    [Fact]
    public void Restore_PreservesMigrationOrigin_WhenPresent()
    {
        var originalWorkItemId = Guid.NewGuid();

        var task = DeveloperTask.Restore(
            TaskId.New(), FeatureId.New(), "T", "d", DevelopmentItemStatus.Completed,
            Guid.NewGuid(), DateTimeOffset.UtcNow, "TSK-00000007", originalWorkItemId);

        Assert.Equal(originalWorkItemId, task.MigratedFromWorkItemId);
        Assert.Equal(DevelopmentItemStatus.Completed, task.Status);
    }

    [Fact]
    public void Restore_MigrationOrigin_IsNull_ForOrdinaryTasks()
    {
        var task = DeveloperTask.Restore(
            TaskId.New(), FeatureId.New(), "T", "d", DevelopmentItemStatus.New,
            Guid.NewGuid(), DateTimeOffset.UtcNow, "TSK-00000008", migratedFromWorkItemId: null);

        Assert.Null(task.MigratedFromWorkItemId);
    }

    [Fact]
    public void Create_WithSourceRoadmapNodeId_TrimsAndSets_IdentityUnchanged()
    {
        var id = TaskId.New();
        var featureId = FeatureId.New();
        var createdAt = DateTimeOffset.UtcNow;

        // WU-02 narrow bridge: an optional roadmap-ledger NodeId string may tag a
        // Task at creation. Traceability only -- never alters the aggregate's own
        // Guid identity, and distinct from MigratedFromWorkItemId (Chat WorkItem
        // provenance, a Guid).
        var task = new DeveloperTask(
            id, featureId, "T", "d", Guid.NewGuid(), createdAt,
            sourceRoadmapNodeId: "  WI-07-2.1.1  ");

        Assert.Equal("WI-07-2.1.1", task.SourceRoadmapNodeId);
        Assert.Null(task.MigratedFromWorkItemId);
        Assert.Equal(id, task.Id);
        Assert.Equal(featureId, task.FeatureId);
        Assert.Equal(string.Empty, task.Reference);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_BlankSourceRoadmapNodeId_IsNull(string? sourceRoadmapNodeId)
    {
        var task = new DeveloperTask(
            TaskId.New(), FeatureId.New(), "T", "d", Guid.NewGuid(), DateTimeOffset.UtcNow,
            sourceRoadmapNodeId: sourceRoadmapNodeId);

        Assert.Null(task.SourceRoadmapNodeId);
    }

    [Fact]
    public void Create_SourceRoadmapNodeId_NullByDefault()
    {
        // No Task today is roadmap-originated; the field is dormant until the
        // roadmap importer (WI-07-1.1.3) exists.
        var task = new DeveloperTask(
            TaskId.New(), FeatureId.New(), "T", "d", Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Null(task.SourceRoadmapNodeId);
    }

    [Fact]
    public void Restore_RoundTripsSourceRoadmapNodeId()
    {
        var id = TaskId.New();
        var task = DeveloperTask.Restore(
            id, FeatureId.New(), "T", "d", DevelopmentItemStatus.Completed,
            Guid.NewGuid(), DateTimeOffset.UtcNow, "TSK-00000007",
            migratedFromWorkItemId: null, sourceRoadmapNodeId: "T-07-1");

        Assert.Equal("T-07-1", task.SourceRoadmapNodeId);
        Assert.Null(task.MigratedFromWorkItemId);
        Assert.Equal(id, task.Id);
        Assert.Equal(DevelopmentItemStatus.Completed, task.Status);
    }
}
