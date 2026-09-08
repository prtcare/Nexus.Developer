using Nexus.Developer.Core.DevelopmentControl;
using Xunit;

namespace Nexus.Developer.Core.Tests;

// SP1-WAVE-04 Lane B: DevelopmentControlAddress + DevelopmentControlRole + pure resolver.
// Ratified semantics under test:
//   * role is the routing authority; NodeId is immutable identity inside a role
//   * explicit qualified lookup is deterministic (never inferred)
//   * an ambiguous unqualified lookup MUST NOT silently choose -> Ambiguous + candidates
//   * no node-ID prefix inference (prefixes are migration evidence, never routing authority)
//   * V1 single-workbook compat -> Foundation
//   * explicit result types (Resolved / NotFound / Ambiguous), address serialization round-trip
public class DevelopmentControlAddressTests
{
    private sealed class SetRegistry : IDevelopmentControlRoleRegistry
    {
        private readonly HashSet<string> _ids;
        public SetRegistry(DevelopmentControlRole role, params string[] ids)
        {
            Role = role;
            _ids = new HashSet<string>(ids, StringComparer.Ordinal);
        }

        public DevelopmentControlRole Role { get; }
        public bool Contains(NodeId nodeId) => _ids.Contains(nodeId.Value);
    }

    private static IDevelopmentControlRoleRegistry Foundation(params string[] ids) =>
        new SetRegistry(DevelopmentControlRole.Foundation, ids);

    private static IDevelopmentControlRoleRegistry Products(params string[] ids) =>
        new SetRegistry(DevelopmentControlRole.Products, ids);

    // ------------------------------------------------ qualified F / P resolve

    [Fact]
    public void QualifiedFoundation_Resolves_WhenFoundationOwnsTheId()
    {
        var registries = new[] { Foundation("M-07-LIVE"), Products("WI-07-LIVE") };

        var result = DevelopmentControlAddressResolver.ResolveQualified(
            DevelopmentControlRole.Foundation, new NodeId("M-07-LIVE"), registries);

        Assert.True(result.IsResolved);
        Assert.Equal(DevelopmentControlRole.Foundation, result.Address!.Value.Role);
        Assert.Equal("M-07-LIVE", result.Address!.Value.Id.Value);
    }

    [Fact]
    public void QualifiedProducts_Resolves_WhenProductsOwnsTheId()
    {
        var registries = new[] { Foundation("M-07-LIVE"), Products("WI-07-LIVE") };

        var result = DevelopmentControlAddressResolver.ResolveQualified(
            DevelopmentControlRole.Products, new NodeId("WI-07-LIVE"), registries);

        Assert.True(result.IsResolved);
        Assert.Equal(DevelopmentControlRole.Products, result.Address!.Value.Role);
        Assert.Equal("WI-07-LIVE", result.Address!.Value.Id.Value);
    }

    // ------------------------------------------------ same NodeId in both roles

    [Fact]
    public void SameNodeId_InBothRoles_UnqualifiedIsAmbiguous_AndNeverSilentlyChooses()
    {
        // A node identity that legitimately exists in BOTH workbooks (Foundation + Products).
        var registries = new[] { Foundation("SHARED-01"), Products("SHARED-01") };

        var result = DevelopmentControlAddressResolver.ResolveUnqualified(new NodeId("SHARED-01"), registries);

        Assert.True(result.IsAmbiguous);
        Assert.Null(result.Address); // no silent winner
        Assert.NotNull(result.Candidates);
        Assert.Equal(2, result.Candidates!.Count);
        Assert.Contains(result.Candidates, a => a.Role == DevelopmentControlRole.Foundation);
        Assert.Contains(result.Candidates, a => a.Role == DevelopmentControlRole.Products);
    }

    [Fact]
    public void SameNodeId_InBothRoles_QualifiedLookupIsDeterministicPerRole()
    {
        var registries = new[] { Foundation("SHARED-01"), Products("SHARED-01") };

        var f = DevelopmentControlAddressResolver.ResolveQualified(
            DevelopmentControlRole.Foundation, new NodeId("SHARED-01"), registries);
        var p = DevelopmentControlAddressResolver.ResolveQualified(
            DevelopmentControlRole.Products, new NodeId("SHARED-01"), registries);

        Assert.True(f.IsResolved && f.Address!.Value.IsFoundation);
        Assert.True(p.IsResolved && p.Address!.Value.IsProducts);
        Assert.NotEqual(f.Address!.Value, p.Address!.Value); // same id, different role -> distinct address
    }

    // ------------------------------------------------ missing

    [Fact]
    public void Unqualified_UnknownId_IsNotFound()
    {
        var registries = new[] { Foundation("M-07-LIVE"), Products("WI-07-LIVE") };

        var result = DevelopmentControlAddressResolver.ResolveUnqualified(new NodeId("DOES-NOT-EXIST"), registries);

        Assert.Equal(DevelopmentControlAddressResolveStatus.NotFound, result.Status);
        Assert.Null(result.Address);
    }

    [Fact]
    public void Qualified_WithUnknownId_IsNotFound()
    {
        var registries = new[] { Foundation("M-07-LIVE"), Products("WI-07-LIVE") };

        var result = DevelopmentControlAddressResolver.ResolveQualified(
            DevelopmentControlRole.Foundation, new NodeId("DOES-NOT-EXIST"), registries);

        Assert.Equal(DevelopmentControlAddressResolveStatus.NotFound, result.Status);
    }

    [Fact]
    public void Qualified_AgainstRoleWithNoRegistry_IsNotFound()
    {
        var registries = new[] { Foundation("M-07-LIVE") }; // no Products registry present

        var result = DevelopmentControlAddressResolver.ResolveQualified(
            DevelopmentControlRole.Products, new NodeId("WI-07-LIVE"), registries);

        Assert.Equal(DevelopmentControlAddressResolveStatus.NotFound, result.Status);
    }

    // ------------------------------------------------ V1 legacy compat

    [Fact]
    public void V1SingleWorkbook_BareNodeId_ResolvesToFoundation()
    {
        // Legacy single-workbook deployment: no role ambiguity, Foundation owns everything.
        var address = DevelopmentControlAddressResolver.ResolveV1(new NodeId("WI-07-LIVE"));

        Assert.True(address.IsFoundation);
        Assert.Equal("WI-07-LIVE", address.Id.Value);
    }

    [Fact]
    public void V1World_UnqualifiedViaFoundationOnlyRegistry_ResolvesToFoundation()
    {
        // Equivalent modelling of a V1 deployment as a Foundation-only registry.
        var registries = new[] { Foundation("WI-07-LIVE") };

        var result = DevelopmentControlAddressResolver.ResolveUnqualified(new NodeId("WI-07-LIVE"), registries);

        Assert.True(result.IsResolved);
        Assert.True(result.Address!.Value.IsFoundation);
    }

    // ------------------------------------------------ no ID-prefix inference

    [Fact]
    public void NodeIdPrefix_NeverInfersRoutingAuthority()
    {
        // "WI-07-LIVE" looks like a work item, but the registries are the only authority:
        // here Products owns it. Unqualified must follow the registry, not the prefix.
        var registries = new[] { Products("WI-07-LIVE") };

        var result = DevelopmentControlAddressResolver.ResolveUnqualified(new NodeId("WI-07-LIVE"), registries);

        Assert.True(result.IsResolved);
        Assert.True(result.Address!.Value.IsProducts);
    }

    [Fact]
    public void NodeIdPrefix_DoesNotMakeAnUnknownIdResolveToFoundation()
    {
        // Id absent from every registry -> NotFound. Never "fall back" to Foundation just
        // because the id looks like a Foundation/Milestone node.
        var registries = new[] { Products("WI-07-LIVE") };

        var result = DevelopmentControlAddressResolver.ResolveUnqualified(new NodeId("M-07-NOPE"), registries);

        Assert.Equal(DevelopmentControlAddressResolveStatus.NotFound, result.Status);
    }

    // ------------------------------------------------ serialization round-trip

    [Theory]
    [InlineData("M-07-LIVE")]
    [InlineData("WI-07-LIVE")]
    public void AddressSerialization_RoundTrips(string nodeIdText)
    {
        foreach (var role in new[] { DevelopmentControlRole.Foundation, DevelopmentControlRole.Products })
        {
            var address = new DevelopmentControlAddress(role, new NodeId(nodeIdText));

            Assert.True(DevelopmentControlAddress.TryParse(address.ToString(), out var parsed));
            Assert.Equal(address, parsed);
            Assert.Equal(address.ToString(), parsed.ToString());
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("WI-07-LIVE")]          // bare node id is NOT an address (V1/unqualified form)
    [InlineData("Wizard:WI-07-LIVE")]   // unknown role token
    [InlineData("Foundation:")]         // empty id
    [InlineData(":WI-07-LIVE")]         // missing role
    public void AddressParsing_RejectsNonQualifiedOrMalformed(string? text)
    {
        Assert.False(DevelopmentControlAddress.TryParse(text, out _));
    }

    [Fact]
    public void Address_IsValueTypeEquality_ByRoleAndId()
    {
        var a = DevelopmentControlAddress.Products(new NodeId("WI-07-LIVE"));
        var b = new DevelopmentControlAddress(DevelopmentControlRole.Products, new NodeId("WI-07-LIVE"));
        var c = DevelopmentControlAddress.Foundation(new NodeId("WI-07-LIVE"));

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.Equal("Products:WI-07-LIVE", a.ToString());
    }
}
