using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Tasks;
using DeveloperTask = Nexus.Developer.Core.Tasks.Task;

namespace Nexus.Developer.Application.Tasks.Commands.CreateTask;

// Aliases the domain Task type -- this handler's own HandleAsync must return a
// bare System.Threading.Tasks.Task<...>, and "using Nexus.Developer.Core.Tasks;"
// alone would make unqualified "Task" ambiguous (CS0104), same reasoning as
// ITaskRepository itself.
public sealed class CreateTaskHandler
{
    private readonly ITaskRepository _repository;

    public CreateTaskHandler(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async System.Threading.Tasks.Task<CreateTaskResult> HandleAsync(
        CreateTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        var task = new DeveloperTask(
            TaskId.New(),
            command.FeatureId,
            command.Title,
            command.Description,
            command.CreatedByUserId,
            DateTimeOffset.UtcNow);

        await _repository.AddAsync(task, cancellationToken);

        return new CreateTaskResult(
            task.Id,
            task.Title,
            task.Reference);
    }
}
