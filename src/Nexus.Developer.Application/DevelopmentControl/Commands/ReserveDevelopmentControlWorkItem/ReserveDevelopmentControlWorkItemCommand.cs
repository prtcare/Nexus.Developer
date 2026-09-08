using Nexus.Developer.Core.DevelopmentControl;

namespace Nexus.Developer.Application.DevelopmentControl.Commands.ReserveDevelopmentControlWorkItem;

// SP1-M05: governed mutation that marks a roadmap work-item node as reserved by a
// worker on a branch/worktree (AGENTS.md: "Reserve the change before the first edit").
// ChangeId is the CHG-... token the workbook's Active Changes ledger records; the node
// moves to InProgress and an open change row is appended -- all in one governed atomic
// write under the canonical writer lock.
public sealed record ReserveDevelopmentControlWorkItemCommand(
    string NodeId,
    string ChangeId,
    string ActorName,
    string? ActorId = null,
    ActorType ActorType = ActorType.Agent,
    string? Source = null,
    string? Branch = null,
    string? Worktree = null,
    string? Reason = null,
    string? SessionId = null);
