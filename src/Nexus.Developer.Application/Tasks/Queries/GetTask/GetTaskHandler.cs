using Nexus.Developer.Core.Tasks;

namespace Nexus.Developer.Application.Tasks.Queries.GetTask;

public sealed class GetTaskHandler
{
    private readonly ITaskRepository _repository;

    public GetTaskHandler(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async System.Threading.Tasks.Task<GetTaskResult?> HandleAsync(
        GetTaskQuery query,
        CancellationToken cancellationToken = default)
    {
        var task = await _repository.GetAsync(query.TaskId, cancellationToken);

        if (task is null)
        {
            return null;
        }

        return new GetTaskResult(
            task.Id,
            task.FeatureId,
            task.Title,
            task.Description,
            task.Status,
            task.CreatedByUserId,
            task.CreatedAt,
            task.Reference,
            task.MigratedFromWorkItemId);
    }
}
