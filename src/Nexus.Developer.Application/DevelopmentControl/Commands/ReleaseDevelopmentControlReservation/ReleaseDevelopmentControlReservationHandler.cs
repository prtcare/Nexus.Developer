using Nexus.Developer.Core.DevelopmentControl;

namespace Nexus.Developer.Application.DevelopmentControl.Commands.ReleaseDevelopmentControlReservation;

// SP1-M05: releases a reservation as a governed atomic write. No open reservation for
// the node surfaces as a ValidationFailure (422) from the inner store.
public sealed class ReleaseDevelopmentControlReservationHandler
{
    private readonly IConcurrencyGuardedDevelopmentControlStore _store;

    public ReleaseDevelopmentControlReservationHandler(IConcurrencyGuardedDevelopmentControlStore store)
    {
        _store = store;
    }

    public async Task<AtomicWriteResult<Node>> HandleAsync(
        ReleaseDevelopmentControlReservationCommand command,
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
                new[] { "A ChangeId (CHG-...) is required to release a reservation." });
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
            store => store.ReleaseReservationAsync(nodeId, envelope));

        return await _store.ExecuteAtomicWriteAsync(request, cancellationToken);
    }
}
