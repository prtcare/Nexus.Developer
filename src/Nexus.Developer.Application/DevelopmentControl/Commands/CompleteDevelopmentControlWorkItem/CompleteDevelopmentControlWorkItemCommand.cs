using Nexus.Developer.Core.DevelopmentControl;

namespace Nexus.Developer.Application.DevelopmentControl.Commands.CompleteDevelopmentControlWorkItem;

// SP1-M05: governed mutation that marks a work-item node Completed with a caller-supplied
// result/evidence string (AGENTS.md: completion requires a recorded human review and
// verified green integration). The matching open Active Change row is marked Completed in
// the same atomic save.
public sealed record CompleteDevelopmentControlWorkItemCommand(
    string NodeId,
    string ChangeId,
    string ActorName,
    string ResultOrEvidence,
    string? ActorId = null,
    ActorType ActorType = ActorType.Agent,
    string? Source = null,
    string? Reason = null,
    string? SessionId = null);
