using Nexus.Developer.Core.DevelopmentRuns;

namespace Nexus.Developer.Application.DevelopmentRuns.Commands.StartDevelopmentRun;

// SP1-M05: loads the run, applies the NotStarted -> InProgress transition (SP1-M03
// Core), persists, and returns the resulting lifecycle read model.
public sealed class StartDevelopmentRunHandler
{
    private readonly IDevelopmentRunRepository _repository;

    public StartDevelopmentRunHandler(IDevelopmentRunRepository repository)
    {
        _repository = repository;
    }

    public async Task<DevelopmentRunLifecycleResult> HandleAsync(
        StartDevelopmentRunCommand command,
        CancellationToken cancellationToken = default)
    {
        var run = await _repository.GetAsync(command.DevelopmentRunId, cancellationToken);
        if (run is null)
        {
            throw new DevelopmentRunNotFoundException(command.DevelopmentRunId);
        }

        try
        {
            run.Start(command.WorkerId, command.WorkerType);
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
