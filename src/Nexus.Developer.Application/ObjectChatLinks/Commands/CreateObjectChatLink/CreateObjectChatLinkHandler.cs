using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Features;
using Nexus.Developer.Core.Issues;
using Nexus.Developer.Core.Milestones;
using Nexus.Developer.Core.ObjectChatLinks;
using Nexus.Developer.Core.Subtasks;
using ITaskRepository = Nexus.Developer.Core.Tasks.ITaskRepository;

namespace Nexus.Developer.Application.ObjectChatLinks.Commands.CreateObjectChatLink;

public sealed class CreateObjectChatLinkHandler
{
    private readonly IFeatureRepository _featureRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ISubtaskRepository _subtaskRepository;
    private readonly IMilestoneRepository _milestoneRepository;
    private readonly IIssueRepository _issueRepository;
    private readonly IObjectChatLinkRepository _objectChatLinkRepository;

    public CreateObjectChatLinkHandler(
        IFeatureRepository featureRepository,
        ITaskRepository taskRepository,
        ISubtaskRepository subtaskRepository,
        IMilestoneRepository milestoneRepository,
        IIssueRepository issueRepository,
        IObjectChatLinkRepository objectChatLinkRepository)
    {
        _featureRepository = featureRepository;
        _taskRepository = taskRepository;
        _subtaskRepository = subtaskRepository;
        _milestoneRepository = milestoneRepository;
        _issueRepository = issueRepository;
        _objectChatLinkRepository = objectChatLinkRepository;
    }

    public async Task<CreateObjectChatLinkResult> HandleAsync(
        CreateObjectChatLinkCommand command,
        CancellationToken cancellationToken = default)
    {
        await EnsureTargetExistsAsync(command.TargetType, command.TargetId, cancellationToken);

        var link = new ObjectChatLink(
            ObjectChatLinkId.New(),
            command.ConversationId,
            command.MessageRangeStart,
            command.MessageRangeEnd,
            command.TargetType,
            command.TargetId,
            command.LinkedByUserId,
            DateTimeOffset.UtcNow);

        await _objectChatLinkRepository.AddAsync(link, cancellationToken);

        return new CreateObjectChatLinkResult(
            link.Id,
            link.ConversationId,
            link.TargetType,
            link.TargetId,
            link.LinkedAt);
    }

    // A chat link must never dangle: before persisting, confirm the target object
    // actually resolves through the repository that owns its type. This is the one
    // piece of real validation logic this slice adds (M-07-10.4).
    private async Task EnsureTargetExistsAsync(
        ObjectChatLinkTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken)
    {
        var exists = targetType switch
        {
            ObjectChatLinkTargetType.Feature =>
                await _featureRepository.GetAsync(new FeatureId(targetId), cancellationToken) is not null,
            ObjectChatLinkTargetType.Task =>
                await _taskRepository.GetAsync(new TaskId(targetId), cancellationToken) is not null,
            ObjectChatLinkTargetType.Subtask =>
                await _subtaskRepository.GetAsync(new SubtaskId(targetId), cancellationToken) is not null,
            ObjectChatLinkTargetType.Milestone =>
                await _milestoneRepository.GetAsync(new MilestoneId(targetId), cancellationToken) is not null,
            ObjectChatLinkTargetType.Issue =>
                await _issueRepository.GetAsync(new IssueId(targetId), cancellationToken) is not null,
            _ => throw new ArgumentOutOfRangeException(
                nameof(targetType), targetType, "Unrecognized ObjectChatLinkTargetType.")
        };

        if (!exists)
        {
            throw new ObjectChatLinkTargetNotFoundException(targetType, targetId);
        }
    }
}
