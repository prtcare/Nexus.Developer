namespace Nexus.Developer.Core.Common.Identifiers;

public readonly record struct TaskId(Guid Value)
{
    public static TaskId New()
        => new(Guid.NewGuid());

    public override string ToString()
        => Value.ToString();
}
