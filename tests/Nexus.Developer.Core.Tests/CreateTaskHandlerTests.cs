using Nexus.Developer.Application.Tasks.Commands.CreateTask;
using Nexus.Developer.Core.Common.Identifiers;
using Xunit;
using DeveloperTask = Nexus.Developer.Core.Tasks.Task;

namespace Nexus.Developer.Core.Tests;

public class CreateTaskHandlerTests
{
    [Fact]
    public async Task Create_WhenCallerSuppliesSourceRoadmapNodeId_SetsItOnTask()
    {
        // WU-02 narrow bridge (WAVE-05A Lane D): the roadmap importer
        // (WI-07-1.1.3) will be a caller of the create contract, tagging the Task
        // with the roadmap-ledger NodeId that originated it. Optional and
        // traceability-only -- the aggregate normalizes blank -> null and trims.
        var featureId = FeatureId.New();
        var repository = new RecordingTaskRepository();
        var handler = new CreateTaskHandler(repository);

        var result = await handler.HandleAsync(
            new CreateTaskCommand(
                featureId,
                Title: "New Task",
                Description: "A task",
                CreatedByUserId: Guid.NewGuid(),
                SourceRoadmapNodeId: "  WI-07-2.1.1  "));

        var task = Assert.Single(repository.Tasks);

        Assert.Equal("WI-07-2.1.1", task.SourceRoadmapNodeId);
        Assert.Null(task.MigratedFromWorkItemId);
        Assert.Equal(featureId, task.FeatureId);
        Assert.Equal(task.Id, result.TaskId);
    }

    [Fact]
    public async Task Create_WhenNoSourceRoadmapNodeIdSupplied_TaskHasNone()
    {
        // The field must stay null for ordinary creates -- dormant until the
        // roadmap importer exists; no roadmap tag is ever fabricated.
        var repository = new RecordingTaskRepository();
        var handler = new CreateTaskHandler(repository);

        await handler.HandleAsync(
            new CreateTaskCommand(
                FeatureId.New(),
                Title: "New Task",
                Description: "A task",
                CreatedByUserId: Guid.NewGuid()));

        Assert.Null(Assert.Single(repository.Tasks).SourceRoadmapNodeId);
    }

    private sealed class RecordingTaskRepository : Nexus.Developer.Core.Tasks.ITaskRepository
    {
        private readonly List<DeveloperTask> _tasks = new();

        public IReadOnlyList<DeveloperTask> Tasks => _tasks;

        public System.Threading.Tasks.Task AddAsync(
            DeveloperTask task,
            CancellationToken cancellationToken = default)
        {
            _tasks.Add(task);
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task<DeveloperTask?> GetAsync(
            TaskId id,
            CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.FromResult(_tasks.FirstOrDefault(task => task.Id == id));

        public System.Threading.Tasks.Task UpdateAsync(
            DeveloperTask task,
            CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.CompletedTask;

        public System.Threading.Tasks.Task<IReadOnlyList<DeveloperTask>> ListByFeatureAsync(
            FeatureId featureId,
            CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.FromResult<IReadOnlyList<DeveloperTask>>(
                _tasks.Where(task => task.FeatureId == featureId).ToList());
    }
}
