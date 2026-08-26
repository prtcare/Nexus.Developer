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
}
