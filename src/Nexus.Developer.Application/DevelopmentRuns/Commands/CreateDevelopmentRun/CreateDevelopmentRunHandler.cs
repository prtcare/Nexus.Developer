using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.DevelopmentRuns;
using Nexus.Developer.Core.Features;
using Nexus.Developer.Core.Issues;
using ITaskRepository = Nexus.Developer.Core.Tasks.ITaskRepository;

namespace Nexus.Developer.Application.DevelopmentRuns.Commands.CreateDevelopmentRun;

public sealed class CreateDevelopmentRunHandler
{
    private readonly IFeatureRepository _featureRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IIssueRepository _issueRepository;
    private readonly IDevelopmentRunRepository _developmentRunRepository;

    public CreateDevelopmentRunHandler(
        IFeatureRepository featureRepository,
        ITaskRepository taskRepository,
        IIssueRepository issueRepository,
        IDevelopmentRunRepository developmentRunRepository)
    {
        _featureRepository = featureRepository;
        _taskRepository = taskRepository;
        _issueRepository = issueRepository;
        _developmentRunRepository = developmentRunRepository;
    }

    public async Task<CreateDevelopmentRunResult> HandleAsync(
        CreateDevelopmentRunCommand command,
        CancellationToken cancellationToken = default)
    {
        await EnsureTargetExistsAsync(command.TargetType, command.TargetId, cancellationToken);

        var run = new DevelopmentRun(
            DevelopmentRunId.New(),
            command.TargetType,
            command.TargetId,
            command.CreatedByUserId,
            DateTimeOffset.UtcNow);

        await _developmentRunRepository.AddAsync(run, cancellationToken);

        return new CreateDevelopmentRunResult(
            run.Id,
            run.TargetType,
            run.TargetId,
            run.Reference);
    }

    // A DevelopmentRun must never dangle: before persisting, confirm the target
    // object actually resolves through the repository that owns its type. This is
    // the one piece of real validation logic this slice adds (WI-07-10.3.1),
    // mirroring CreateObjectChatLinkHandler's target check.
    private async Task EnsureTargetExistsAsync(
        DevelopmentRunTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken)
    {
        var exists = targetType switch
        {
            DevelopmentRunTargetType.Feature =>
                await _featureRepository.GetAsync(new FeatureId(targetId), cancellationToken) is not null,
            DevelopmentRunTargetType.Task =>
                await _taskRepository.GetAsync(new TaskId(targetId), cancellationToken) is not null,
            DevelopmentRunTargetType.Issue =>
                await _issueRepository.GetAsync(new IssueId(targetId), cancellationToken) is not null,
            _ => throw new ArgumentOutOfRangeException(
                nameof(targetType), targetType, "Unrecognized DevelopmentRunTargetType.")
        };

        if (!exists)
        {
            throw new DevelopmentRunTargetNotFoundException(targetType, targetId);
        }
    }
}
