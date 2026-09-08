namespace Nexus.Developer.Core.DevelopmentControl;

// Filters for SearchNodesAsync. Every field is optional; a null field means "no
// constraint on this dimension". Text matches against a node's Name/Path/Notes.
// ParentId restricts to direct children of the given parent (null ParentId alone would
// be ambiguous with "no constraint", so restricting to root nodes is expressed via the
// dedicated RootNodesOnly flag).
public sealed record NodeSearchCriteria(
    string? Text,
    NodeType? NodeType,
    Status? Status,
    NodeId? ParentId,
    bool RootNodesOnly);
