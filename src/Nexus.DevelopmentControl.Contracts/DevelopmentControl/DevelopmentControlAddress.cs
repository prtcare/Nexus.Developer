namespace Nexus.Developer.Core.DevelopmentControl;

// SP1-WAVE-04 Lane B: the ratified cross-workbook address shape
//     DevelopmentControlAddress { DevelopmentControlRole Role; NodeId Id; }
// (R06 LAYER_MODEL; WORK_UNIVERSE.md DevelopmentControlAddress row; P1-WAVE-03 human review
// decision #4). Role is the routing authority; NodeId is immutable identity inside the role.
// Construction is explicit and qualified -- an address always carries its role, so no lookup
// ever needs to infer a role from an ID prefix. NodeId equality is value equality, so two
// addresses with the same Role and Id are equal regardless of how they were constructed.
public readonly record struct DevelopmentControlAddress(
    DevelopmentControlRole Role,
    NodeId Id)
{
    public static DevelopmentControlAddress Foundation(NodeId id) => new(DevelopmentControlRole.Foundation, id);
    public static DevelopmentControlAddress Products(NodeId id) => new(DevelopmentControlRole.Products, id);

    public bool IsFoundation => Role == DevelopmentControlRole.Foundation;
    public bool IsProducts => Role == DevelopmentControlRole.Products;

    /// <summary>Deterministic, round-trippable canonical form: "Foundation:&lt;nodeId&gt;" / "Products:&lt;nodeId&gt;".</summary>
    public override string ToString() => $"{Role}:{Id.Value}";

    /// <summary>
    /// Parses a qualified address string ("Foundation:&lt;id&gt;" or "Products:&lt;id&gt;").
    /// A bare node ID (no role qualifier) is NOT an address -- that is the V1/unqualified
    /// form and must go through DevelopmentControlAddressResolver, never prefix inference.
    /// </summary>
    public static bool TryParse(string? value, out DevelopmentControlAddress address)
    {
        address = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var separator = value.IndexOf(':');
        if (separator <= 0 || separator == value.Length - 1)
        {
            return false;
        }

        var roleText = value[..separator];
        var idText = value[(separator + 1)..];
        if (!Enum.TryParse<DevelopmentControlRole>(roleText, ignoreCase: true, out var role))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(idText))
        {
            return false;
        }

        address = new DevelopmentControlAddress(role, new NodeId(idText));
        return true;
    }

    public static DevelopmentControlAddress Parse(string value) =>
        TryParse(value, out var address)
            ? address
            : throw new FormatException(
                $"'{value}' is not a qualified DevelopmentControlAddress (expected 'Foundation:<nodeId>' or 'Products:<nodeId>').");
}
