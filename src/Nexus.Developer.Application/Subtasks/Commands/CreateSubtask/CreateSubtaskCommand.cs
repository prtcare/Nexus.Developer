using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.Subtasks.Commands.CreateSubtask;

public sealed record CreateSubtaskCommand(
    TaskId TaskId,
    string Title,
    string Description,
    Guid CreatedByUserId);
