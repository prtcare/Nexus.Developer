namespace Nexus.Developer.Core.Common.Identifiers;

public readonly record struct MilestoneLinkId(Guid Value)
{
    public static MilestoneLinkId New()
        => new(Guid.NewGuid());

    public override string ToString()
        => Value.ToString();
}
