using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.Subtasks.Queries.GetSubtask;

public sealed record GetSubtaskResult(
    SubtaskId SubtaskId,
    TaskId TaskId,
    string Title,
    string Description,
    DevelopmentItemStatus Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string Reference);
