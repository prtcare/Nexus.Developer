using Nexus.Developer.Application.Tasks.Queries.GetTask;
using Nexus.Developer.Core.Tasks;

namespace Nexus.Developer.Application.Tasks.Queries.ListTasksByFeature;

public sealed class ListTasksByFeatureHandler
{
    private readonly ITaskRepository _repository;

    public ListTasksByFeatureHandler(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async System.Threading.Tasks.Task<ListTasksByFeatureResult> HandleAsync(
        ListTasksByFeatureQuery query,
        CancellationToken cancellationToken = default)
    {
        var tasks = await _repository.ListByFeatureAsync(query.FeatureId, cancellationToken);

        var results = tasks
            .Select(task => new GetTaskResult(
                task.Id,
                task.FeatureId,
                task.Title,
                task.Description,
                task.Status,
                task.CreatedByUserId,
                task.CreatedAt,
                task.Reference,
                task.MigratedFromWorkItemId))
            .ToList();

        return new ListTasksByFeatureResult(results);
    }
}
