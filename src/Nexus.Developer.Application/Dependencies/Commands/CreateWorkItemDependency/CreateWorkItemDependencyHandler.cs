using Nexus.Developer.Core.Common.Identifiers;
using Nexus.Developer.Core.Dependencies;
using Nexus.Developer.Core.Features;
using Nexus.Developer.Core.Issues;
using Nexus.Developer.Core.Milestones;
using Nexus.Developer.Core.Subtasks;
using ITaskRepository = Nexus.Developer.Core.Tasks.ITaskRepository;

namespace Nexus.Developer.Application.Dependencies.Commands.CreateWorkItemDependency;

public sealed class CreateWorkItemDependencyHandler
{
    private readonly IFeatureRepository _featureRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ISubtaskRepository _subtaskRepository;
    private readonly IMilestoneRepository _milestoneRepository;
    private readonly IIssueRepository _issueRepository;
    private readonly IWorkItemDependencyRepository _dependencyRepository;

    public CreateWorkItemDependencyHandler(
        IFeatureRepository featureRepository,
        ITaskRepository taskRepository,
        ISubtaskRepository subtaskRepository,
        IMilestoneRepository milestoneRepository,
        IIssueRepository issueRepository,
        IWorkItemDependencyRepository dependencyRepository)
    {
        _featureRepository = featureRepository;
        _taskRepository = taskRepository;
        _subtaskRepository = subtaskRepository;
        _milestoneRepository = milestoneRepository;
        _issueRepository = issueRepository;
        _dependencyRepository = dependencyRepository;
    }

    public async Task<CreateWorkItemDependencyResult> HandleAsync(
        CreateWorkItemDependencyCommand command,
        CancellationToken cancellationToken = default)
    {
        // Construct first: the entity constructor is the single authoritative shape
        // validator. It rejects the degenerate self-loop (same node on both ends)
        // and a RequiredState on a non-Blocking edge before any traversal or
        // persistence runs, satisfying the "rejected before any graph traversal"
        // acceptance.
        var dependency = new WorkItemDependency(
            WorkItemDependencyId.New(),
            command.UpstreamType,
            command.UpstreamId,
            command.DownstreamType,
            command.DownstreamId,
            command.Kind,
            command.RequiredState,
            command.CreatedByUserId,
            DateTimeOffset.UtcNow);

        await EnsureTargetExistsAsync("upstream", command.UpstreamType, command.UpstreamId, cancellationToken);
        await EnsureTargetExistsAsync("downstream", command.DownstreamType, command.DownstreamId, cancellationToken);

        // Only Blocking edges participate in cycle detection; a Parallel or
        // Informational edge can never close a cycle (WI-07-2.1.1).
        if (command.Kind == WorkItemDependencyKind.Blocking)
        {
            var existingBlockingEdges = await _dependencyRepository.ListAllBlockingAsync(cancellationToken);

            var cycle = DependencyGraphTraversal.FindCycleIfAdded(
                existingBlockingEdges,
                command.UpstreamType,
                command.UpstreamId,
                command.DownstreamType,
                command.DownstreamId);

            if (cycle.Count > 0)
            {
                // The edge is not persisted: a cyclic Blocking dependency is rejected
                // at write time, naming the full cycle path.
                throw new WorkItemDependencyCycleException(cycle);
            }
        }

        await _dependencyRepository.AddAsync(dependency, cancellationToken);

        return new CreateWorkItemDependencyResult(
            dependency.Id,
            dependency.UpstreamType,
            dependency.UpstreamId,
            dependency.DownstreamType,
            dependency.DownstreamId,
            dependency.Kind,
            dependency.RequiredState,
            dependency.CreatedByUserId,
            dependency.CreatedAt);
    }

    // A dependency edge must never dangle: confirm each endpoint resolves through
    // the repository that owns its type before persisting, naming which side was
    // invalid (WI-07-2.1.1). Mirrors CreateObjectChatLinkHandler's target check.
    private async Task EnsureTargetExistsAsync(
        string side,
        WorkItemDependencyNodeType nodeType,
        Guid nodeId,
        CancellationToken cancellationToken)
    {
        var exists = nodeType switch
        {
            WorkItemDependencyNodeType.Feature =>
                await _featureRepository.GetAsync(new FeatureId(nodeId), cancellationToken) is not null,
            WorkItemDependencyNodeType.Task =>
                await _taskRepository.GetAsync(new TaskId(nodeId), cancellationToken) is not null,
            WorkItemDependencyNodeType.Subtask =>
                await _subtaskRepository.GetAsync(new SubtaskId(nodeId), cancellationToken) is not null,
            WorkItemDependencyNodeType.Milestone =>
                await _milestoneRepository.GetAsync(new MilestoneId(nodeId), cancellationToken) is not null,
            WorkItemDependencyNodeType.Issue =>
                await _issueRepository.GetAsync(new IssueId(nodeId), cancellationToken) is not null,
            _ => throw new ArgumentOutOfRangeException(
                nameof(nodeType), nodeType, "Unrecognized WorkItemDependencyNodeType.")
        };

        if (!exists)
        {
            throw new WorkItemDependencyTargetNotFoundException(side, nodeType, nodeId);
        }
    }
}
