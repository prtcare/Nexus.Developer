using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.Features.Queries.GetFeature;

public sealed record GetFeatureResult(
    FeatureId FeatureId,
    SubprojectId SubprojectId,
    string Title,
    string Description,
    DevelopmentItemStatus Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string Reference);
