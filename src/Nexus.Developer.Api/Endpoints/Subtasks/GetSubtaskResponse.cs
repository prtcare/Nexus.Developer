namespace Nexus.Developer.Api.Endpoints.Subtasks;

public sealed record GetSubtaskResponse(
    Guid SubtaskId,
    Guid TaskId,
    string Title,
    string Description,
    int Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string Reference);
