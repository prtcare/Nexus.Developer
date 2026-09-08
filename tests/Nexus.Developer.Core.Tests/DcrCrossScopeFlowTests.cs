using Nexus.Developer.Core.DevelopmentControl;
using Xunit;

namespace Nexus.Developer.Core.Tests;

// SP1-WAVE-04 Lane C: typed Dependency/Context Resolver cross-scope flow.
// Ratified term (see Lane C report): "typed DCR" = resolving strongly-typed cross-scope work
// references (DevelopmentControlAddress source/target, typed reason, state, dependency,
// evidence/handback) over the Lane B resolver -- NOT a new relationship engine. These tests
// prove the typed chain: Product work -> typed request -> Foundation address ->
// dependency/authorization recorded -> evidence reference -> handback -> Product resumes.
public class DcrCrossScopeFlowTests
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

    private static readonly ActorRef Requester =
        new(ActorType.Agent, "lane-c-test", "Lane C test");

    private static DevelopmentControlAddress Product(string id) =>
        DevelopmentControlAddress.Products(new NodeId(id));

    private static DevelopmentControlAddress Foundation(string id) =>
        DevelopmentControlAddress.Foundation(new NodeId(id));

    private static DcrCrossScopeRequest MakeRequest(
        DevelopmentControlAddress source, DevelopmentControlAddress target,
        string reasonDetail = "product work depends on foundation node") =>
        DcrCrossScopeFlow.Request(
            new DcrRequestId("DCR-REQ-0001"),
            source,
            target,
            new DcrReason(DcrReasonKind.Dependency, reasonDetail),
            Requester);

    // ---------------------------------------------------------- request shape

    [Fact]
    public void ProductToFoundationRequest_CarriesTypedSourceTargetReason()
    {
        var request = MakeRequest(Product("WI-07-PROD"), Foundation("M-07-FOUND"));

        Assert.True(request.IsCrossScope);
        Assert.True(request.Source.IsProducts);
        Assert.True(request.Target.IsFoundation);
        Assert.Equal(DcrReasonKind.Dependency, request.Reason.Kind);
        Assert.Equal(DcrFlowState.Requested, request.State);
        Assert.Equal(Requester, request.RequestedBy);
    }

    [Fact]
    public void SameRoleRequest_IsNotCrossScope()
    {
        var request = MakeRequest(Product("A"), Product("B"));

        Assert.False(request.IsCrossScope);
    }

    // ---------------------------------------------------------- typed resolution

    [Fact]
    public void Resolve_ProductToFoundation_BothOwned_RecordsTypedAuthorization()
    {
        var registries = new[]
        {
            new SetRegistry(DevelopmentControlRole.Products, "WI-07-PROD"),
            new SetRegistry(DevelopmentControlRole.Foundation, "M-07-FOUND"),
        };
        var request = MakeRequest(Product("WI-07-PROD"), Foundation("M-07-FOUND"));

        var resolution = DcrCrossScopeFlow.Resolve(request, registries);

        Assert.True(resolution.IsResolved);
        Assert.Equal(DcrResolveStatus.Resolved, resolution.Status);
        Assert.NotNull(resolution.Dependency);
        Assert.True(resolution.Dependency!.IsProductsToFoundation);
        Assert.Equal(request.RequestId, resolution.Dependency.RequestId);
        Assert.Equal(request.Reason, resolution.Dependency.Reason);
        Assert.Equal(Product("WI-07-PROD"), resolution.Dependency.Source);
        Assert.Equal(Foundation("M-07-FOUND"), resolution.Dependency.Target);
    }

    [Fact]
    public void Resolve_ProductSourceNotOwned_IsNotFound_NoSilentAuthorization()
    {
        var registries = new[]
        {
            new SetRegistry(DevelopmentControlRole.Products, "WI-OTHER"), // source id NOT owned
            new SetRegistry(DevelopmentControlRole.Foundation, "M-07-FOUND"),
        };
        var request = MakeRequest(Product("WI-07-PROD"), Foundation("M-07-FOUND"));

        var resolution = DcrCrossScopeFlow.Resolve(request, registries);

        Assert.Equal(DcrResolveStatus.NotFound, resolution.Status);
        Assert.Null(resolution.Dependency);
    }

    [Fact]
    public void Resolve_QualifiedTargetOwnedByBothRoles_ResolvesDeterministically_NoInference()
    {
        // A node id may legitimately exist in BOTH roles. The typed DCR request carries a
        // QUALIFIED Foundation address, so its resolution is deterministic to Foundation --
        // never silent, never inferred. Ambiguity is a property of UNQUALIFIED lookup (the
        // Lane B resolver, below), which a qualified address never triggers.
        var registries = new[]
        {
            new SetRegistry(DevelopmentControlRole.Products, "WI-07-PROD"),
            new SetRegistry(DevelopmentControlRole.Foundation, "SHARED-01"),
            new SetRegistry(DevelopmentControlRole.Products, "SHARED-01"), // same id in Products too
        };
        var request = MakeRequest(Product("WI-07-PROD"), Foundation("SHARED-01"));

        var resolution = DcrCrossScopeFlow.Resolve(request, registries);

        Assert.Equal(DcrResolveStatus.Resolved, resolution.Status);
        Assert.True(resolution.Dependency!.Target.IsFoundation);
        Assert.True(resolution.Dependency.Source.IsProducts);

        // The same unqualified id IS ambiguous -> explicit Ambiguous, no silent choice.
        var unqualified = DevelopmentControlAddressResolver.ResolveUnqualified(new NodeId("SHARED-01"), registries);
        Assert.True(unqualified.IsAmbiguous);
        Assert.Null(unqualified.Address);
    }

    [Fact]
    public void Resolve_SameRoleRequest_IsNotCrossScope()
    {
        var registries = new[]
        {
            new SetRegistry(DevelopmentControlRole.Products, "A", "B"),
        };
        var request = MakeRequest(Product("A"), Product("B"));

        var resolution = DcrCrossScopeFlow.Resolve(request, registries);

        Assert.Equal(DcrResolveStatus.NotCrossScope, resolution.Status);
        Assert.Null(resolution.Dependency);
    }

    [Fact]
    public void Resolve_NeverInfersRoleFromIdPrefix()
    {
        // The source id "M-07-X" reads like a Foundation milestone but only the Products
        // registry owns it; typed resolution must follow the registry, not the prefix.
        var registries = new[]
        {
            new SetRegistry(DevelopmentControlRole.Products, "M-07-X"),
            new SetRegistry(DevelopmentControlRole.Foundation, "M-07-FOUND"),
        };
        var request = MakeRequest(Product("M-07-X"), Foundation("M-07-FOUND"));

        var resolution = DcrCrossScopeFlow.Resolve(request, registries);

        Assert.Equal(DcrResolveStatus.Resolved, resolution.Status);
        Assert.True(resolution.Dependency!.Source.IsProducts);
    }

    // ---------------------------------------------------------- typed state machine

    [Theory]
    [InlineData(DcrFlowState.Requested, DcrFlowState.Authorized, true)]
    [InlineData(DcrFlowState.Requested, DcrFlowState.Rejected, true)]
    [InlineData(DcrFlowState.Requested, DcrFlowState.InProgress, false)]   // must authorize first
    [InlineData(DcrFlowState.Authorized, DcrFlowState.InProgress, true)]
    [InlineData(DcrFlowState.Authorized, DcrFlowState.EvidenceReady, false)]
    [InlineData(DcrFlowState.InProgress, DcrFlowState.EvidenceReady, true)]
    [InlineData(DcrFlowState.EvidenceReady, DcrFlowState.HandedBack, true)]
    [InlineData(DcrFlowState.HandedBack, DcrFlowState.EvidenceReady, false)] // terminal
    [InlineData(DcrFlowState.HandedBack, DcrFlowState.InProgress, false)]
    [InlineData(DcrFlowState.Rejected, DcrFlowState.Authorized, false)]      // terminal
    public void StateMachine_OnlyAllowsExplicitTransitions(DcrFlowState from, DcrFlowState to, bool allowed)
    {
        Assert.Equal(allowed, DcrCrossScopeFlow.TryTransition(from, to, out _));
        if (!allowed)
        {
            Assert.NotNull(DcrCrossScopeFlow.TryTransition(from, to, out var error) ? null : error);
        }
    }

    [Fact]
    public void StateMachine_HandedBackAndRejected_AreTerminal()
    {
        Assert.Empty(DcrCrossScopeFlow.AllowedNext(DcrFlowState.HandedBack));
        Assert.Empty(DcrCrossScopeFlow.AllowedNext(DcrFlowState.Rejected));
    }

    // ---------------------------------------------------------- full typed chain

    [Fact]
    public void FullTypedChain_ProductRequest_To_Handback_ResumesProduct()
    {
        var registries = new[]
        {
            new SetRegistry(DevelopmentControlRole.Products, "WI-07-PROD"),
            new SetRegistry(DevelopmentControlRole.Foundation, "M-07-FOUND"),
        };
        var request = MakeRequest(Product("WI-07-PROD"), Foundation("M-07-FOUND"));
        var requestedAt = new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero);

        // 1) Product work -> typed governed request to a Foundation address
        // 2) dependency/authorization recorded (both addresses exist in their roles)
        var resolution = DcrCrossScopeFlow.Resolve(request, registries);
        Assert.True(resolution.IsResolved);
        Assert.NotNull(resolution.Dependency);

        // 3) explicit state progression (never auto-advances)
        Assert.True(DcrCrossScopeFlow.TryTransition(request.State, DcrFlowState.Authorized, out _));
        Assert.True(DcrCrossScopeFlow.TryTransition(DcrFlowState.Authorized, DcrFlowState.InProgress, out _));
        Assert.True(DcrCrossScopeFlow.TryTransition(DcrFlowState.InProgress, DcrFlowState.EvidenceReady, out _));

        // 4) Assurance evidence is REFERENCED, never self-declared by the flow
        var evidence = new DcrEvidenceReference("EVID-0001", "Foundation run result captured as evidence reference");

        // 5) result/handback to the Product source -> Product resumes
        var handback = DcrCrossScopeFlow.Handback(
            resolution.Request, "RUN-FOUND-7: succeeded", evidence, requestedAt.AddHours(1));

        Assert.Equal(request.RequestId, handback.RequestId);
        Assert.True(handback.Source.IsProducts); // handed back to the Product source
        Assert.Equal("RUN-FOUND-7: succeeded", handback.ResultReference);
        Assert.Equal(evidence, handback.Evidence);
        Assert.Equal(requestedAt.AddHours(1), handback.ReturnedAt);
    }
}
