using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.DevelopmentRuns;

namespace Nexus.Developer.Application.DevelopmentRuns.Queries.GetDevelopmentRun;

public sealed class GetDevelopmentRunHandler
{
    private readonly IDevelopmentRunRepository _repository;

    public GetDevelopmentRunHandler(IDevelopmentRunRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetDevelopmentRunResult?> HandleAsync(
        GetDevelopmentRunQuery query,
        CancellationToken cancellationToken = default)
    {
        var run = await _repository.GetAsync(query.DevelopmentRunId, cancellationToken);

        if (run is null)
        {
            return null;
        }

        return new GetDevelopmentRunResult(
            run.Id,
            run.TargetType,
            run.TargetId,
            run.Status,
            run.CreatedByUserId,
            run.CreatedAt,
            run.Reference);
    }
}
