namespace Nexus.Developer.Api.Endpoints.Dependencies;

public sealed record GetDependencyResponse(
    Guid WorkItemDependencyId,
    string UpstreamType,
    Guid UpstreamId,
    string DownstreamType,
    Guid DownstreamId,
    string Kind,
    string? RequiredState,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt);
