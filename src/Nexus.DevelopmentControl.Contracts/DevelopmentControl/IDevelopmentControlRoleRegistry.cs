namespace Nexus.Developer.Core.DevelopmentControl;

// SP1-WAVE-04 Lane B: a per-role membership source the resolver asks "does this role own
// NodeId X?" -- the ONLY legitimate way to establish which role an unqualified node ID
// belongs to. Role is never inferred from an ID prefix. A role registry is a pure,
// zero-IO contract: in-memory/registry-backed implementations live in the consuming repo
// (Nexus.Developer, Forge/Foundation); no live Shared Platform service is required to
// resolve an address (Lane B constraint).
public interface IDevelopmentControlRoleRegistry
{
    DevelopmentControlRole Role { get; }

    /// <summary>True when this role is authoritative for the given immutable node identity.</summary>
    bool Contains(NodeId nodeId);
}
