using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.Features.Commands.SetFeatureParent;

public sealed record SetFeatureParentResult(
    FeatureId FeatureId,
    FeatureId? ParentFeatureId);
