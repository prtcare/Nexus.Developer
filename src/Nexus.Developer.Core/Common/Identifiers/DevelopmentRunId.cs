namespace Nexus.Developer.Core.Common.Identifiers;

public readonly record struct DevelopmentRunId(Guid Value)
{
    public static DevelopmentRunId New()
        => new(Guid.NewGuid());

    public override string ToString()
        => Value.ToString();
}
