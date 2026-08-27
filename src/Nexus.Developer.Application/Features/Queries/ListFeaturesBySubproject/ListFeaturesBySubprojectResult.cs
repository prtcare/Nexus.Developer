using Nexus.Developer.Application.Features.Queries.GetFeature;

namespace Nexus.Developer.Application.Features.Queries.ListFeaturesBySubproject;

public sealed record ListFeaturesBySubprojectResult(
    IReadOnlyList<GetFeatureResult> Features);
