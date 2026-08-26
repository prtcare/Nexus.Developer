namespace Nexus.Developer.Core.Common.Identifiers;

public readonly record struct IssueId(Guid Value)
{
    public static IssueId New()
        => new(Guid.NewGuid());

    public override string ToString()
        => Value.ToString();
}
