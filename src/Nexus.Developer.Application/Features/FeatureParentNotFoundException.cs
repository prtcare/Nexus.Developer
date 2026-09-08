using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.Features;

// Thrown when a create-subfeature or re-parent call names a parent Feature that
// does not resolve (D04). Maps to 400 on the create/parent endpoints: the parent
// id is invalid caller-supplied input, never an unhandled 500.
public sealed class FeatureParentNotFoundException : Exception
{
    public FeatureParentNotFoundException(FeatureId parentFeatureId)
        : base($"The parent feature '{parentFeatureId}' does not exist.")
    {
        ParentFeatureId = parentFeatureId;
    }

    public FeatureId ParentFeatureId { get; }
}
