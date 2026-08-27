using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.Features.Commands.CreateFeature;

public sealed record CreateFeatureResult(
    FeatureId FeatureId,
    string Title,
    string Reference);
