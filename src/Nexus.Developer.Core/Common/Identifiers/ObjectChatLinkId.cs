namespace Nexus.Developer.Core.Common.Identifiers;

public readonly record struct ObjectChatLinkId(Guid Value)
{
    public static ObjectChatLinkId New()
        => new(Guid.NewGuid());

    public override string ToString()
        => Value.ToString();
}
