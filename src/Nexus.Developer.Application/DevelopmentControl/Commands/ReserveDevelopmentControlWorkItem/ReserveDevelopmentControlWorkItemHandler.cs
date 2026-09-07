using Nexus.Developer.Core.DevelopmentControl;

namespace Nexus.Developer.Application.DevelopmentControl.Commands.ReserveDevelopmentControlWorkItem;

// SP1-M05: executes the reserve as a governed atomic write through the guarded store.
// The optimistic ExpectedRowVersion is taken from an authoritative read performed by the
// handler; the guarded coordinator re-verifies it against the freshly opened workbook
// INSIDE the writer lock before the inner store writes, so a node that changed between
// the caller's read and the write surfaces as a controlled ConcurrencyConflict (409),
// never a lost update.
public sealed class ReserveDevelopmentControlWorkItemHandler
{
    private readonly IConcurrencyGuardedDevelopmentControlStore _store;

    public ReserveDevelopmentControlWorkItemHandler(IConcurrencyGuardedDevelopmentControlStore store)
    {
        _store = store;
    }

    public async Task<AtomicWriteResult<Node>> HandleAsync(
        ReserveDevelopmentControlWorkItemCommand command,
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
                new[] { "A ChangeId (CHG-...) is required to reserve a work item." });
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
            store => store.ReserveWorkItemAsync(nodeId, actor, command.Branch, command.Worktree, envelope));

        return await _store.ExecuteAtomicWriteAsync(request, cancellationToken);
    }
}
