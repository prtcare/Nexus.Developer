using Nexus.Developer.Core.DevelopmentRuns;

namespace Nexus.Developer.Application.DevelopmentRuns.Commands.CancelDevelopmentRun;

// SP1-M05: NotStarted | InProgress -> Cancelled. A run in any other state cannot be
// cancelled and surfaces as a DevelopmentRunStateException (409).
public sealed class CancelDevelopmentRunHandler
{
    private readonly IDevelopmentRunRepository _repository;

    public CancelDevelopmentRunHandler(IDevelopmentRunRepository repository)
    {
        _repository = repository;
    }

    public async Task<DevelopmentRunLifecycleResult> HandleAsync(
        CancelDevelopmentRunCommand command,
        CancellationToken cancellationToken = default)
    {
        var run = await _repository.GetAsync(command.DevelopmentRunId, cancellationToken);
        if (run is null)
        {
            throw new DevelopmentRunNotFoundException(command.DevelopmentRunId);
        }

        try
        {
            run.Cancel(command.Summary);
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
