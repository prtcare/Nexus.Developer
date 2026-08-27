namespace Nexus.Developer.Api.Endpoints.Tasks;

public sealed record GetTaskResponse(
    Guid TaskId,
    Guid FeatureId,
    string Title,
    string Description,
    int Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string Reference,
    Guid? MigratedFromWorkItemId);
