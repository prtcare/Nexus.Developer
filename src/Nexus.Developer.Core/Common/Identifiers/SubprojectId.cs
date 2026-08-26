namespace Nexus.Developer.Core.Common.Identifiers;

// Subproject is a Product Core concept (physically hosted in Nexus.Experience,
// interim placement per WI-06-8.1.1 / ADR-005). Nexus.Developer must not reference
// a product domain assembly (AGENTS.md Boundary rules) -- it holds this identifier
// as an opaque Guid, not the product's own SubprojectId type.
public readonly record struct SubprojectId(Guid Value)
{
    public static SubprojectId New()
        => new(Guid.NewGuid());

    public override string ToString()
        => Value.ToString();
}
