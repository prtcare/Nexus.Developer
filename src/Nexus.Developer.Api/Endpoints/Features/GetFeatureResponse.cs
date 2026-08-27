namespace Nexus.Developer.Api.Endpoints.Features;

public sealed record GetFeatureResponse(
    Guid FeatureId,
    Guid SubprojectId,
    string Title,
    string Description,
    int Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string Reference);
