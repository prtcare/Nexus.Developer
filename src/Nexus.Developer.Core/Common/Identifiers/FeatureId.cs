namespace Nexus.Developer.Core.Common.Identifiers;

public readonly record struct FeatureId(Guid Value)
{
    public static FeatureId New()
        => new(Guid.NewGuid());

    public override string ToString()
        => Value.ToString();
}
