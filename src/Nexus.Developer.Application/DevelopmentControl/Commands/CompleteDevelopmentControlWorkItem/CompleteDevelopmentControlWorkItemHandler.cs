using Nexus.Developer.Core.DevelopmentControl;

namespace Nexus.Developer.Application.DevelopmentControl.Commands.CompleteDevelopmentControlWorkItem;

// SP1-M05: completes a work item as a governed atomic write. A blank result/evidence is
// rejected as InvalidRequest (the governed completion rule requires caller-supplied
// evidence -- nothing is auto-generated).
public sealed class CompleteDevelopmentControlWorkItemHandler
{
    private readonly IConcurrencyGuardedDevelopmentControlStore _store;

    public CompleteDevelopmentControlWorkItemHandler(IConcurrencyGuardedDevelopmentControlStore store)
    {
        _store = store;
    }

    public async Task<AtomicWriteResult<Node>> HandleAsync(
        CompleteDevelopmentControlWorkItemCommand command,
        CancellationToken cancellationToken = default)
    {
        NodeId nodeId;
        try
        {
            nodeId = new NodeId(command.NodeId);
        }
        catch (ArgumentException ex)
        {
            return AtomicWriteResultFactory.InvalidRequest<Node>(new[] { ex.Message });
        }

        if (string.IsNullOrWhiteSpace(command.ChangeId))
        {
            return AtomicWriteResultFactory.InvalidRequest<Node>(
                new[] { "A ChangeId (CHG-...) is required to complete a work item." });
        }

        if (string.IsNullOrWhiteSpace(command.ResultOrEvidence))
        {
            return AtomicWriteResultFactory.InvalidRequest<Node>(
                new[] { "ResultOrEvidence is required to complete a work item (completion must record what happened)." });
        }

        var current = await _store.GetNodeAsync(nodeId, cancellationToken);
        if (current is null)
        {
            return AtomicWriteResultFactory.NotFound<Node>(nodeId.Value);
        }

        var actor = new ActorRef(command.ActorType, command.ActorId ?? command.ActorName, command.ActorName);
        var envelope = new MutationEnvelope(
            current.RowVersion,
            actor,
            command.Source ?? "Nexus.Developer.Application",
            ChatPlatform: null,
            command.SessionId,
            PromptId: null,
            command.ChangeId,
            CorrelationId: null,
            IdempotencyKey: null,
            command.Reason);

        var request = new AtomicWriteRequest<Node>(
            _store.MutexIdentity,
            _store.LockTimeout,
            envelope,
            nodeId.Value,
            store => store.CompleteWorkItemAsync(nodeId, command.ResultOrEvidence, envelope));

        return await _store.ExecuteAtomicWriteAsync(request, cancellationToken);
    }
}
