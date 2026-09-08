namespace Nexus.Developer.Core.DevelopmentControl;

// ---------------------------------------------------------------------------
// SP1-WAVE-04 Lane C: the typed Dependency/Context Resolver vocabulary.
//
// Term ratified in the Lane C report against the authoritative docs: the R06 LAYER_MODEL and
// CURRENT_STATE spell the concept as the "Dependency/Context Resolver" (the bare acronym DCR
// never appears in Nexus.Platform\docs). "typed DCR" = resolving strongly-typed cross-scope
// work references (DevelopmentControlAddress source/target, typed reason, state, dependency,
// evidence/handback) instead of loose/string results -- the narrow missing piece R06 names
// (DevelopmentControlAddress awareness, cross-workbook resolution, DevelopmentControl
// integration). It is NOT a new relationship engine: these records are pure value contracts
// that MAP ONTO existing structures (reason -> WorkItemDependency.Reason / ActiveChange /
// PreflightDeclaration reason; state -> Node.Status / DevelopmentRun lifecycle; dependency ->
// WorkItemDependency edge; evidence/handback -> the governed "Result/Evidence" the store
// records on complete/activity). No persistence, no roadmap-changing autonomy, no automatic
// architecture authority, no automatic Git merge authority.
// ---------------------------------------------------------------------------

/// <summary>Typed lifecycle of a cross-scope Dependency/Context resolution (maps onto the
/// existing Node.Status / DevelopmentRun lifecycle states).</summary>
public enum DcrFlowState
{
    Requested = 0,
    Authorized = 1,
    InProgress = 2,
    EvidenceReady = 3,
    HandedBack = 4,
    Rejected = 5,
}

/// <summary>Typed reason a Product work object engages a Foundation target (maps onto the
/// existing reason text carried by WorkItemDependency / ActiveChange / PreflightDeclaration).</summary>
public enum DcrReasonKind
{
    Dependency = 0,
    Authorization = 1,
    Evidence = 2,
    Handback = 3,
    Review = 4,
}

public sealed record DcrReason(DcrReasonKind Kind, string Detail)
{
    public override string ToString() => $"{Kind}: {Detail}";
}

public sealed record DcrRequestId(string Value)
{
    public override string ToString() => Value;
}

/// <summary>
/// A typed cross-scope governed request. Source and Target are qualified DevelopmentControl
/// addresses (Lane B); a genuine cross-scope request has Source.Role != Target.Role. Carries
/// the typed reason, the requester and the current typed state.
/// </summary>
public sealed record DcrCrossScopeRequest(
    DcrRequestId RequestId,
    DevelopmentControlAddress Source,
    DevelopmentControlAddress Target,
    DcrReason Reason,
    ActorRef RequestedBy,
    DcrFlowState State)
{
    public bool IsCrossScope => Source.Role != Target.Role;
}

/// <summary>
/// The recorded typed dependency/authorization between a Product source and a Foundation
/// target (maps onto a WorkItemDependency edge -- source, target, reason -- that the governed
/// store records). Existence of both addresses in their roles is what authorizes it.
/// </summary>
public sealed record DcrDependencyAuthorization(
    DcrRequestId RequestId,
    DevelopmentControlAddress Source,
    DevelopmentControlAddress Target,
    DcrReason Reason)
{
    public bool IsProductsToFoundation =>
        Source.IsProducts && Target.IsFoundation;
}

/// <summary>
/// An immutable reference to the result/evidence a Foundation-side execution produced. Maps
/// onto the governed "Result / Evidence" the store records when work completes; never a
/// self-declared Assurance verdict (Assurance PASS remains authoritative only from the 08
/// Assurance interfaces -- see SP1-W06).
/// </summary>
public sealed record DcrEvidenceReference(string? EvidenceId, string Description);

/// <summary>
/// The typed result handed back to the Product source so it can resume. ResultReference names
/// the Foundation execution/run result (maps onto a DevelopmentRun result / the completed
/// governed node's Result/Evidence field).
/// </summary>
public sealed record DcrHandback(
    DcrRequestId RequestId,
    DevelopmentControlAddress Source,
    string ResultReference,
    DcrEvidenceReference? Evidence,
    DateTimeOffset ReturnedAt);
