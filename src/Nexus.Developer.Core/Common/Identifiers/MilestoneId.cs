namespace Nexus.Developer.Core.Common.Identifiers;

public readonly record struct MilestoneId(Guid Value)
{
    public static MilestoneId New()
        => new(Guid.NewGuid());

    public override string ToString()
        => Value.ToString();
}
