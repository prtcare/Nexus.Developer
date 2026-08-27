namespace Nexus.Developer.Api.Endpoints.Tasks;

public sealed record CreateTaskRequest(
    Guid FeatureId,
    string Title,
    string? Description,
    Guid CreatedByUserId);
