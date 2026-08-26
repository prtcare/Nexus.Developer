namespace Nexus.Developer.Core.Common.Identifiers;

public readonly record struct IssueLinkId(Guid Value)
{
    public static IssueLinkId New()
        => new(Guid.NewGuid());

    public override string ToString()
        => Value.ToString();
}
