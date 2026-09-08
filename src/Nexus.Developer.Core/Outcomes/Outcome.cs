using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.Outcomes;

// An Outcome is the Developer-side terminal record of the actual result/value a governed
// execution produced, made verifiable by pointing at Assurance-owned evidence -- never by
// self-declaring a verdict. It is NOT "Task completed", NOT "Build PASS", NOT "Test PASS",
// NOT "Assurance PASS", NOT "Deployment succeeded": those are intermediate facts. An Outcome
// records, against a product, what a piece of work achieved (or is still working toward), the
// success criteria it is measured against, and -- once achieved -- references to the
// independent assurance evidence that already exists for that achievement.
//
// P1-WAVE-05A Lane E ships the minimum useful model as a Core domain aggregate with pure
// aggregate tests ONLY -- deliberately no EF configuration, no DbSet, no migration, no
// repository, no handler/API this wave. The brief lists zero persistence/query tests and
// explicitly asks for the "smallest exact model" without "bloated schema"; every listed
// behaviour is domain-level. The persistence + link-table shape for the first real consumer
// is documented in SP1_W06_OUTCOME_IMPLEMENTATION_REPORT.md so a later lane can add it with
// zero re-design (the same deferral Lane D applied to read/API surface).
//
// External identities are held as opaque VALUE strings, not typed references, for a
// mechanical reason: the CI feed (github-prtcare) carries zero versions of
// Nexus.Governance.Contracts and Nexus.Assurance.Contracts, so a committed PackageReference
// to either would break this repo's own CI restore (NU1101). ProductId below is the VALUE of
// the Governance product identity; AssuranceEvidenceIds are the VALUES of Assurance-held
// evidence ids. No Developer ProductId type is created (03 GOVERNANCE stays the only product
// identity authority), no verdict type is created, and the Outcome has no path to write,
// update, delete, or rate assurance evidence -- it only records references the caller asserts
// already exist. An adapter to the real Governance/Assurance types applies when the feed
// mirror lands (Lane D drop-in model).
public sealed class Outcome : AggregateRoot<OutcomeId>
{
    public Outcome(
        OutcomeId id,
        string productId,
        string description,
        string? successCriteria,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        IEnumerable<FeatureId> relatedFeatureIds)
        : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        ProductId = productId.Trim();
        Description = description.Trim();
        SuccessCriteria = string.IsNullOrWhiteSpace(successCriteria)
            ? null
            : successCriteria.Trim();
        Status = OutcomeStatus.Pending;
        AchievedAt = null;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
        Reference = string.Empty;
        RelatedFeatureIds = MaterializeFeatures(relatedFeatureIds);
        AssuranceEvidenceIds = new List<string>().AsReadOnly();
    }

    private Outcome(
        OutcomeId id,
        string productId,
        string description,
        string? successCriteria,
        OutcomeStatus status,
        DateTimeOffset? achievedAt,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        string reference,
        IReadOnlyList<FeatureId> relatedFeatureIds,
        IReadOnlyList<string> assuranceEvidenceIds)
        : base(id)
    {
        ProductId = productId;
        Description = description;
        SuccessCriteria = successCriteria;
        Status = status;
        AchievedAt = achievedAt;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
        Reference = reference;
        RelatedFeatureIds = relatedFeatureIds;
        AssuranceEvidenceIds = assuranceEvidenceIds;
    }

    // The Governance product identity VALUE (Nexus.Governance.Contracts.ProductId.Value).
    // Required, opaque, immutable, external authority: Developer holds the value only -- it
    // never mints a ProductId type and never consults a local product registry (none exists
    // here). An adapter to the real Governance ProductId applies once the feed mirror lands.
    public string ProductId { get; }

    public string Description { get; }

    public string? SuccessCriteria { get; }

    public OutcomeStatus Status { get; private set; }

    // null while Pending; stamped on Achieve (UtcNow unless the caller supplies a timestamp).
    // Achieved-timestamp semantics are enforced on Restore: an Achieved row must carry one, a
    // Pending row must not.
    public DateTimeOffset? AchievedAt { get; private set; }

    public Guid CreatedByUserId { get; }

    public DateTimeOffset CreatedAt { get; }

    public string Reference { get; private set; } = string.Empty;

    // Narrow Feature<->Outcome relation (the outcome's "related Feature(s)"), fixed at
    // construction and never mutated afterwards. Deliberately a plain set of Developer
    // FeatureIds in the style of the existing specialist link/child patterns (Task holds
    // FeatureId; IssueLink/MilestoneLink rows) -- NOT a generic WorkRelationship engine and
    // not a Feature that stores a child list.
    public IReadOnlyList<FeatureId> RelatedFeatureIds { get; }

    // Assurance evidence reference(s) -- the VALUES of Assurance-held evidence ids (Lane C's
    // AssuranceEvidenceId). Empty while Pending; populated only by Achieve with the
    // caller-supplied references. Developer never writes, creates, mutates, or rates the
    // evidence those ids name; it only references evidence that already exists. Carries no
    // verdict: an Outcome may point at evidence an Assurance verdict will be derived from, it
    // may never declare that verdict itself.
    public IReadOnlyList<string> AssuranceEvidenceIds { get; private set; }

    // Rehydration path: only a repository restoring a persisted row knows the reference the
    // store allocated and the row's achieved state -- the public constructor never does. The
    // status/timestamp invariant is enforced here (an Achieved Outcome must carry its
    // AchievedAt; a Pending one must not), mirroring how DevelopmentRun.Restore rehydrates
    // state only a store could know.
    public static Outcome Restore(
        OutcomeId id,
        string productId,
        string description,
        string? successCriteria,
        OutcomeStatus status,
        DateTimeOffset? achievedAt,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        string reference,
        IReadOnlyList<FeatureId> relatedFeatureIds,
        IReadOnlyList<string> assuranceEvidenceIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        if (status == OutcomeStatus.Achieved && achievedAt is null)
        {
            throw new ArgumentException(
                "An Achieved Outcome must carry its AchievedAt timestamp.",
                nameof(achievedAt));
        }

        if (status == OutcomeStatus.Pending && achievedAt is not null)
        {
            throw new ArgumentException(
                "A Pending Outcome cannot carry an AchievedAt timestamp.",
                nameof(achievedAt));
        }

        return new Outcome(
            id,
            productId.Trim(),
            description.Trim(),
            string.IsNullOrWhiteSpace(successCriteria) ? null : successCriteria.Trim(),
            status,
            achievedAt,
            createdByUserId,
            createdAt,
            reference,
            relatedFeatureIds,
            assuranceEvidenceIds);
    }

    // Lifecycle: Pending -> Achieved. Records the caller-supplied references to assurance
    // evidence that ALREADY exists; stamps AchievedAt (UtcNow unless the caller supplies the
    // timestamp). No evidence authority mutation: this method has no path to create, update,
    // delete, or rate evidence, and an Outcome can never be re-achieved or have its evidence
    // amended after achievement.
    public void Achieve(IEnumerable<string> assuranceEvidenceIds, DateTimeOffset? achievedAt = null)
    {
        if (Status != OutcomeStatus.Pending)
        {
            throw new InvalidOperationException(
                $"A {Status} outcome cannot be achieved; only a Pending outcome can be achieved.");
        }

        var references = new List<string>();
        foreach (var evidenceId in assuranceEvidenceIds)
        {
            if (string.IsNullOrWhiteSpace(evidenceId))
            {
                throw new ArgumentException(
                    "Assurance evidence references must be non-blank.",
                    nameof(assuranceEvidenceIds));
            }

            var trimmed = evidenceId.Trim();
            if (!references.Contains(trimmed))
            {
                references.Add(trimmed);
            }
        }

        AssuranceEvidenceIds = references.AsReadOnly();
        AchievedAt = achievedAt ?? DateTimeOffset.UtcNow;
        Status = OutcomeStatus.Achieved;
    }

    private static IReadOnlyList<FeatureId> MaterializeFeatures(
        IEnumerable<FeatureId> relatedFeatureIds)
    {
        var features = new List<FeatureId>();
        foreach (var featureId in relatedFeatureIds)
        {
            if (featureId.Value == Guid.Empty)
            {
                throw new ArgumentException(
                    "A related Feature must carry a non-default id.",
                    nameof(relatedFeatureIds));
            }

            if (!features.Contains(featureId))
            {
                features.Add(featureId);
            }
        }

        return features.AsReadOnly();
    }
}
