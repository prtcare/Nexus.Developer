namespace Nexus.Developer.Core.Common.Identifiers;

public readonly record struct SubtaskId(Guid Value)
{
    public static SubtaskId New()
        => new(Guid.NewGuid());

    public override string ToString()
        => Value.ToString();
}
