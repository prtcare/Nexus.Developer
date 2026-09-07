using Nexus.Developer.Core.DevelopmentRuns;

namespace Nexus.Developer.Application.DevelopmentRuns.Commands.SucceedDevelopmentRun;

// SP1-M05: InProgress -> Completed with the caller-supplied result summary. The
// aggregate requires a non-blank summary (nothing is ever auto-generated).
public sealed class SucceedDevelopmentRunHandler
{
    private readonly IDevelopmentRunRepository _repository;

    public SucceedDevelopmentRunHandler(IDevelopmentRunRepository repository)
    {
        _repository = repository;
    }

    public async Task<DevelopmentRunLifecycleResult> HandleAsync(
        SucceedDevelopmentRunCommand command,
        CancellationToken cancellationToken = default)
    {
        var run = await _repository.GetAsync(command.DevelopmentRunId, cancellationToken);
        if (run is null)
        {
            throw new DevelopmentRunNotFoundException(command.DevelopmentRunId);
        }

        try
        {
            run.Succeed(command.Summary);
        }
        catch (InvalidOperationException ex)
        {
            throw new DevelopmentRunStateException(ex.Message);
        }

        await _repository.UpdateAsync(run, cancellationToken);

        return new DevelopmentRunLifecycleResult(
            run.Id,
            run.Status,
            run.Reference,
            run.WorkerId,
            run.WorkerType,
            run.StartedAt,
            run.CompletedAt,
            run.ResultSummary);
    }
}
