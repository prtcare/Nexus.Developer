using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.Features;

// Thrown by the SetFeatureParent handler when the Feature being re-parented does
// not resolve. The endpoint maps this to 400 (invalid caller-supplied id on a
// mutation), never an unhandled 500 -- mirroring SubprojectNotFoundException.
public sealed class FeatureNotFoundException : Exception
{
    public FeatureNotFoundException(FeatureId featureId)
        : base($"The feature '{featureId}' does not exist.")
    {
        FeatureId = featureId;
    }

    public FeatureId FeatureId { get; }
}
