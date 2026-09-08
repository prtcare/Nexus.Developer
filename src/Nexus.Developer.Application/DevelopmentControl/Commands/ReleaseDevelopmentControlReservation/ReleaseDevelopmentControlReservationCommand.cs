using Nexus.Developer.Core.DevelopmentControl;

namespace Nexus.Developer.Application.DevelopmentControl.Commands.ReleaseDevelopmentControlReservation;

// SP1-M05: governed mutation that releases a worker's reservation on a work-item node
// (the worker gave the work up). The matching open Active Change row is marked Released.
public sealed record ReleaseDevelopmentControlReservationCommand(
    string NodeId,
    string ChangeId,
    string ActorName,
    string? ActorId = null,
    ActorType ActorType = ActorType.Agent,
    string? Source = null,
    string? Reason = null,
    string? SessionId = null);
