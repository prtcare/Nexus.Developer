namespace Nexus.Developer.Api.Endpoints.Dependencies;

public sealed record DependencyNodeResponse(
    string NodeType,
    Guid NodeId);
