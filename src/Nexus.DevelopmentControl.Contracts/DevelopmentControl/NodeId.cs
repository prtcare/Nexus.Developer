namespace Nexus.Developer.Core.DevelopmentControl;

// The Development Control Node's identity is the Master Roadmap's own human-authored
// node id scheme -- strings like "WI-07-2.1.1", "M-07-2.1", "F-07-10" -- the exact ids
// used throughout NEXUS_DEVELOPMENT_CONTROL.xlsx and the roadmap docs, NOT a Guid.
// Every other aggregate in this repo (Feature, Task, WorkItemDependency,
// DevelopmentRun...) uses a Guid-backed strongly-typed id; this one deliberately does
// not, and is intentionally kept out of StronglyTypedIdConverters (which converts
// Guid-backed ids only). The store must round-trip these ids verbatim, so the value is
// trimmed on construction but otherwise never normalized.
public readonly record struct NodeId
{
    public NodeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A node id must be a non-empty, non-whitespace string.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
