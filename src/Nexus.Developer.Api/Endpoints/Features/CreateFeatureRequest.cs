namespace Nexus.Developer.Api.Endpoints.Features;

public sealed record CreateFeatureRequest(
    Guid SubprojectId,
    string Title,
    string? Description,
    Guid CreatedByUserId);
