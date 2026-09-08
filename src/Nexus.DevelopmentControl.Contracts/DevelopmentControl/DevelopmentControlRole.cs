namespace Nexus.Developer.Core.DevelopmentControl;

// SP1-WAVE-04 Lane B: the ratified DevelopmentControl routing authority (R06 frozen shape;
// P1-WAVE-03 human review decision). Role is the routing authority of a governed
// DevelopmentControl address; a NodeId is immutable identity INSIDE a role. Role is NEVER
// inferred from a node-ID prefix -- prefixes are migration-discovery evidence only, never
// routing authority. In the two-workbook model Foundation owns the governed roadmap ledger
// and Products owns the governed product workbook(s); a V1 single-workbook deployment is a
// Foundation-only world (see DevelopmentControlAddressResolver).
public enum DevelopmentControlRole
{
    Foundation = 0,
    Products = 1,
}
