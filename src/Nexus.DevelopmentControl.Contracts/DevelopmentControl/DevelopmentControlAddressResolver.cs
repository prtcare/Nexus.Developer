namespace Nexus.Developer.Core.DevelopmentControl;

// ---------------------------------------------------------------------------
// Resolution outcome vocabulary (explicit; never a silent "chose one").
// ---------------------------------------------------------------------------

public enum DevelopmentControlAddressResolveStatus
{
    Resolved = 0,
    NotFound = 1,
    Ambiguous = 2,
}

/// <summary>
/// The explicit result of an address lookup. NotFound and Ambiguous carry no silently-chosen
/// address; Ambiguous lists the concrete candidates so the caller can make an explicit choice.
/// </summary>
public readonly record struct DevelopmentControlAddressResolution(
    DevelopmentControlAddressResolveStatus Status,
    DevelopmentControlAddress? Address,
    IReadOnlyList<DevelopmentControlAddress>? Candidates)
{
    public static DevelopmentControlAddressResolution Resolved(DevelopmentControlAddress address) =>
        new(DevelopmentControlAddressResolveStatus.Resolved, address, null);

    public static DevelopmentControlAddressResolution NotFoundResult() =>
        new(DevelopmentControlAddressResolveStatus.NotFound, null, null);

    public static DevelopmentControlAddressResolution Ambiguous(IReadOnlyList<DevelopmentControlAddress> candidates) =>
        new(DevelopmentControlAddressResolveStatus.Ambiguous, null, candidates);

    public bool IsResolved => Status == DevelopmentControlAddressResolveStatus.Resolved;
    public bool IsAmbiguous => Status == DevelopmentControlAddressResolveStatus.Ambiguous;
}

// ---------------------------------------------------------------------------
// Pure resolver (SP1-WAVE-04 Lane B).
//
// Rules (ratified): role is the routing authority; an unqualified lookup that matches more
// than one role MUST NOT silently choose (Ambiguous); an explicit qualified lookup is
// deterministic; no node-ID prefix inference; V1 single-workbook deployments are a
// Foundation-only world.
// ---------------------------------------------------------------------------

public static class DevelopmentControlAddressResolver
{
    /// <summary>
    /// Explicit, deterministic qualified lookup: resolve node <paramref name="nodeId"/> under
    /// exactly <paramref name="role"/>. Deterministic because the role is stated -- no
    /// inference. NotFound when no registry for that role claims the id (or no registry for
    /// the role is present).
    /// </summary>
    public static DevelopmentControlAddressResolution ResolveQualified(
        DevelopmentControlRole role,
        NodeId nodeId,
        IReadOnlyList<IDevelopmentControlRoleRegistry> registries)
    {
        foreach (var registry in registries)
        {
            if (registry.Role == role && registry.Contains(nodeId))
            {
                return DevelopmentControlAddressResolution.Resolved(new DevelopmentControlAddress(role, nodeId));
            }
        }

        return DevelopmentControlAddressResolution.NotFoundResult();
    }

    /// <summary>
    /// Unqualified lookup: find which single role owns <paramref name="nodeId"/>. Returns
    /// Ambiguous (with all candidates) when more than one role claims the same node identity;
    /// it never silently prefers one. Returns NotFound when no known role owns the id.
    /// </summary>
    public static DevelopmentControlAddressResolution ResolveUnqualified(
        NodeId nodeId,
        IReadOnlyList<IDevelopmentControlRoleRegistry> registries)
    {
        var matches = new List<DevelopmentControlAddress>();
        foreach (var role in System.Enum.GetValues<DevelopmentControlRole>())
        {
            foreach (var registry in registries)
            {
                if (registry.Role == role && registry.Contains(nodeId))
                {
                    matches.Add(new DevelopmentControlAddress(role, nodeId));
                    break; // one claim per role is enough to name the role as a candidate
                }
            }
        }

        return matches.Count switch
        {
            0 => DevelopmentControlAddressResolution.NotFoundResult(),
            1 => DevelopmentControlAddressResolution.Resolved(matches[0]),
            _ => DevelopmentControlAddressResolution.Ambiguous(matches),
        };
    }

    /// <summary>
    /// V1 single-workbook compatibility: a legacy deployment knows only the Foundation
    /// ledger, so a bare node ID resolves to Foundation. This is an explicit statement of the
    /// deployment's shape, not prefix inference.
    /// </summary>
    public static DevelopmentControlAddress ResolveV1(NodeId nodeId) =>
        new(DevelopmentControlRole.Foundation, nodeId);
}
