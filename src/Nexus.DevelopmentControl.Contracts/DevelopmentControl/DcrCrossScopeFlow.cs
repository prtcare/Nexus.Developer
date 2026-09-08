namespace Nexus.Developer.Core.DevelopmentControl;

// SP1-WAVE-04 Lane C: the pure typed Dependency/Context Resolver cross-scope flow.
//
// Product work -> typed governed request to a Foundation address -> dependency/authorization
// recorded (both addresses exist in their roles) -> Foundation work (a governed execution) ->
// Assurance evidence reference -> result/handback -> Product resumes.
//
// This is a PURE resolver/validator over Lane B addresses + role registries + existing
// lifecycle semantics. It records nothing, mutates no roadmap, grants no architecture
// authority and performs no Git merge. An explicit typed resolution result replaces any
// silent choice.

/// <summary>Explicit outcome of a typed cross-scope resolution (never a silent choice).</summary>
public enum DcrResolveStatus
{
    Resolved = 0,
    NotFound = 1,          // source or target missing from its claimed role
    Ambiguous = 2,         // source or target exists in more than one role
    NotCrossScope = 3,     // source and target share a role (not a cross-scope request)
}

public readonly record struct DcrTypedResolution(
    DcrResolveStatus Status,
    DcrCrossScopeRequest Request,
    DevelopmentControlAddressResolution? SourceResolution,
    DevelopmentControlAddressResolution? TargetResolution,
    DcrDependencyAuthorization? Dependency)
{
    public bool IsResolved => Status == DcrResolveStatus.Resolved;
    public static DcrTypedResolution Fail(DcrResolveStatus status, DcrCrossScopeRequest request) =>
        new(status, request, null, null, null);
}

public static class DcrCrossScopeFlow
{
    /// <summary>Build a typed cross-scope request. A same-role (source==target role) request
    /// is not cross-scope and is rejected up front -- products engage Foundation, never a
    /// second authority inside their own role.</summary>
    public static DcrCrossScopeRequest Request(
        DcrRequestId requestId,
        DevelopmentControlAddress source,
        DevelopmentControlAddress target,
        DcrReason reason,
        ActorRef requestedBy) =>
        new(requestId, source, target, reason, requestedBy, DcrFlowState.Requested);

    /// <summary>
    /// Typed resolution of a cross-scope request: the ONLY way a dependency/authorization is
    /// recorded is both addresses existing in their claimed roles (via the Lane B resolver,
    /// never prefix inference). Returns the typed dependency/authorization edge or an explicit
    /// NotFound / Ambiguous / NotCrossScope.
    /// </summary>
    public static DcrTypedResolution Resolve(
        DcrCrossScopeRequest request,
        IReadOnlyList<IDevelopmentControlRoleRegistry> registries)
    {
        if (!request.IsCrossScope)
        {
            return DcrTypedResolution.Fail(DcrResolveStatus.NotCrossScope, request);
        }

        var sourceResolution = DevelopmentControlAddressResolver.ResolveQualified(
            request.Source.Role, request.Source.Id, registries);
        var targetResolution = DevelopmentControlAddressResolver.ResolveQualified(
            request.Target.Role, request.Target.Id, registries);

        if (sourceResolution.Status != DevelopmentControlAddressResolveStatus.Resolved)
        {
            return new DcrTypedResolution(
                sourceResolution.Status == DevelopmentControlAddressResolveStatus.Ambiguous
                    ? DcrResolveStatus.Ambiguous
                    : DcrResolveStatus.NotFound,
                request, sourceResolution, targetResolution, null);
        }

        if (targetResolution.Status != DevelopmentControlAddressResolveStatus.Resolved)
        {
            return new DcrTypedResolution(
                targetResolution.Status == DevelopmentControlAddressResolveStatus.Ambiguous
                    ? DcrResolveStatus.Ambiguous
                    : DcrResolveStatus.NotFound,
                request, sourceResolution, targetResolution, null);
        }

        // Both addresses exist in their claimed roles -> the typed dependency/authorization is
        // recorded (maps onto a WorkItemDependency edge the governed store would persist).
        var dependency = new DcrDependencyAuthorization(
            request.RequestId, request.Source, request.Target, request.Reason);

        return new DcrTypedResolution(
            DcrResolveStatus.Resolved, request, sourceResolution, targetResolution, dependency);
    }

    /// <summary>Explicit typed state machine for the flow. HandedBack and Rejected are
    /// terminal; every transition is enumerated -- the flow never auto-advances.</summary>
    public static IReadOnlyList<DcrFlowState> AllowedNext(DcrFlowState state) => state switch
    {
        DcrFlowState.Requested => new[] { DcrFlowState.Authorized, DcrFlowState.Rejected },
        DcrFlowState.Authorized => new[] { DcrFlowState.InProgress, DcrFlowState.Rejected },
        DcrFlowState.InProgress => new[] { DcrFlowState.EvidenceReady, DcrFlowState.Rejected },
        DcrFlowState.EvidenceReady => new[] { DcrFlowState.HandedBack },
        _ => Array.Empty<DcrFlowState>(),
    };

    public static bool TryTransition(DcrFlowState from, DcrFlowState to, out string? error)
    {
        error = null;
        if (from == to)
        {
            return true;
        }

        if (AllowedNext(from).Contains(to))
        {
            return true;
        }

        error = $"DCR state transition {from} -> {to} is not allowed.";
        return false;
    }

    /// <summary>
    /// Produce the typed handback to the Product source once Foundation work produced a
    /// result and its evidence reference. The request state must be EvidenceReady or
    /// Authorized/InProgress at the caller's discretion; HandedBack is terminal.
    /// </summary>
    public static DcrHandback Handback(
        DcrCrossScopeRequest request,
        string resultReference,
        DcrEvidenceReference? evidence,
        DateTimeOffset returnedAt) =>
        new(request.RequestId, request.Source, resultReference, evidence, returnedAt);
}
