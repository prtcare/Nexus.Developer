namespace Nexus.Developer.Core.Common.Identifiers;

public readonly record struct OutcomeId(Guid Value)
{
    public static OutcomeId New()
        => new(Guid.NewGuid());

    public override string ToString()
        => Value.ToString();
}
