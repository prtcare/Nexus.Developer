using Nexus.Developer.Core.Subtasks;

namespace Nexus.Developer.Application.Subtasks.Queries.GetSubtask;

public sealed class GetSubtaskHandler
{
    private readonly ISubtaskRepository _repository;

    public GetSubtaskHandler(ISubtaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetSubtaskResult?> HandleAsync(
        GetSubtaskQuery query,
        CancellationToken cancellationToken = default)
    {
        var subtask = await _repository.GetAsync(query.SubtaskId, cancellationToken);

        if (subtask is null)
        {
            return null;
        }

        return new GetSubtaskResult(
            subtask.Id,
            subtask.TaskId,
            subtask.Title,
            subtask.Description,
            subtask.Status,
            subtask.CreatedByUserId,
            subtask.CreatedAt,
            subtask.Reference);
    }
}
