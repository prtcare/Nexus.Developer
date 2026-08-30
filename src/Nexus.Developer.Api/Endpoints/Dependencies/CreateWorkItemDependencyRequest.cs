namespace Nexus.Developer.Api.Endpoints.Dependencies;

// Enum-valued fields ride the wire as strings (the accepted WI-07-10.3.1
// representation), parsed case-insensitively by the endpoint with a clear
// valid-values error on a bad string. RequiredState is optional -- absent means
// "upstream must reach full completion".
public sealed record CreateWorkItemDependencyRequest(
    string UpstreamType,
    Guid UpstreamId,
    string DownstreamType,
    Guid DownstreamId,
    string Kind,
    string? RequiredState,
    Guid CreatedByUserId);
