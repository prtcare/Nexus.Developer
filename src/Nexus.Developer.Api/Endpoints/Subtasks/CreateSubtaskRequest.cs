namespace Nexus.Developer.Api.Endpoints.Subtasks;

public sealed record CreateSubtaskRequest(
    Guid TaskId,
    string Title,
    string? Description,
    Guid CreatedByUserId);
