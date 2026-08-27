namespace Nexus.Developer.Api.Endpoints.Features;

public sealed record CreateFeatureResponse(
    Guid FeatureId,
    string Title,
    string Reference);
