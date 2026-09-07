using Nexus.Developer.Core.DevelopmentRuns;

namespace Nexus.Developer.Application.DevelopmentRuns.Commands.FailDevelopmentRun;

// SP1-M05: InProgress -> Failed with the caller's optional error reason.
public sealed class FailDevelopmentRunHandler
{
    private readonly IDevelopmentRunRepository _repository;

    public FailDevelopmentRunHandler(IDevelopmentRunRepository repository)
    {
        _repository = repository;
    }

    public async Task<DevelopmentRunLifecycleResult> HandleAsync(
        FailDevelopmentRunCommand command,
        CancellationToken cancellationToken = default)
    {
        var run = await _repository.GetAsync(command.DevelopmentRunId, cancellationToken);
        if (run is null)
        {
            throw new DevelopmentRunNotFoundException(command.DevelopmentRunId);
        }

        try
        {
            run.Fail(command.Summary);
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
