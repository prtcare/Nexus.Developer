using Nexus.Developer.Application.Subtasks.Queries.GetSubtask;

namespace Nexus.Developer.Application.Subtasks.Queries.ListSubtasksByTask;

public sealed record ListSubtasksByTaskResult(
    IReadOnlyList<GetSubtaskResult> Subtasks);
