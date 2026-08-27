using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.Subtasks.Commands.CreateSubtask;

public sealed record CreateSubtaskResult(
    SubtaskId SubtaskId,
    string Title,
    string Reference);
