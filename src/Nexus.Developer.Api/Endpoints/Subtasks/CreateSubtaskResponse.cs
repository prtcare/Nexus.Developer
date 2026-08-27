namespace Nexus.Developer.Api.Endpoints.Subtasks;

public sealed record CreateSubtaskResponse(
    Guid SubtaskId,
    string Title,
    string Reference);
