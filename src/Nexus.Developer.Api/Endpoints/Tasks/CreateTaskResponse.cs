namespace Nexus.Developer.Api.Endpoints.Tasks;

public sealed record CreateTaskResponse(
    Guid TaskId,
    string Title,
    string Reference);
