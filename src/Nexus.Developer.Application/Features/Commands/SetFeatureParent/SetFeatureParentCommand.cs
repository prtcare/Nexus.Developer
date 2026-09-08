using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.Features.Commands.SetFeatureParent;

public sealed record SetFeatureParentCommand(
    FeatureId FeatureId,
    FeatureId? ParentFeatureId);
