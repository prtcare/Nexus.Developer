using Nexus.Developer.Application.Subtasks.Queries.GetSubtask;
using Nexus.Developer.Core.Subtasks;

namespace Nexus.Developer.Application.Subtasks.Queries.ListSubtasksByTask;

public sealed class ListSubtasksByTaskHandler
{
    private readonly ISubtaskRepository _repository;

    public ListSubtasksByTaskHandler(ISubtaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<ListSubtasksByTaskResult> HandleAsync(
        ListSubtasksByTaskQuery query,
        CancellationToken cancellationToken = default)
    {
        var subtasks = await _repository.ListByTaskAsync(query.TaskId, cancellationToken);

        var results = subtasks
            .Select(subtask => new GetSubtaskResult(
                subtask.Id,
                subtask.TaskId,
                subtask.Title,
                subtask.Description,
                subtask.Status,
                subtask.CreatedByUserId,
                subtask.CreatedAt,
                subtask.Reference))
            .ToList();

        return new ListSubtasksByTaskResult(results);
    }
}
