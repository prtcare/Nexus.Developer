using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.Features;

// Thrown by the SetFeatureParent handler when making `child` a child of `parent`
// would make a Feature an ancestor of its own ancestor (D04 hierarchy cycle).
// Carries the full cycle path, closing back to the start -- the same shape
// WorkItemDependencyCycleException uses -- so the message reads e.g.
// "Feature:A -> Feature:C -> Feature:B -> Feature:A".
public sealed class FeatureHierarchyCycleException : Exception
{
    public FeatureHierarchyCycleException(IReadOnlyList<FeatureId> path)
        : base($"Setting this parent would create a hierarchy cycle: {Describe(path)}")
    {
        Path = path;
    }

    public IReadOnlyList<FeatureId> Path { get; }

    private static string Describe(IReadOnlyList<FeatureId> path)
        => string.Join(" -> ", path.Select(featureId => $"Feature:{featureId.Value}"));
}
