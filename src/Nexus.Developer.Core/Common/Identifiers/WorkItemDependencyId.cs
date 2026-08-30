namespace Nexus.Developer.Core.Common.Identifiers;

public readonly record struct WorkItemDependencyId(Guid Value)
{
    public static WorkItemDependencyId New()
        => new(Guid.NewGuid());

    public override string ToString()
        => Value.ToString();
}
