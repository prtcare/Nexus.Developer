using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Subtasks;
using Xunit;

namespace Nexus.Developer.Core.Tests;

public class SubtaskTests
{
    [Fact]
    public void Create_ParentsToTask_NotFeature()
    {
        var taskId = TaskId.New();

        var subtask = new Subtask(SubtaskId.New(), taskId, "S", "d", Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(taskId, subtask.TaskId);
        Assert.Equal(DevelopmentItemStatus.New, subtask.Status);
    }

    [Fact]
    public void ChangeStatus_UpdatesInPlace()
    {
        var subtask = new Subtask(SubtaskId.New(), TaskId.New(), "S", "d", Guid.NewGuid(), DateTimeOffset.UtcNow);

        subtask.ChangeStatus(DevelopmentItemStatus.Blocked);

        Assert.Equal(DevelopmentItemStatus.Blocked, subtask.Status);
    }
}
