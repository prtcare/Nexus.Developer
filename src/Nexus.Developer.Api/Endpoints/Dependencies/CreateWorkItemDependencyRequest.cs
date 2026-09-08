namespace Nexus.Developer.Api.Endpoints.Dependencies;

// Enum-valued fields ride the wire as strings (the accepted WI-07-10.3.1
// representation), parsed case-insensitively by the endpoint with a clear
// valid-values error on a bad string. RequiredState is optional -- absent means
// "upstream must reach full completion". Reason is optional additive descriptive
// text for the edge (SP1-M04); blank is normalized to null by the domain entity.
public sealed record CreateWorkItemDependencyRequest(
    string UpstreamType,
    Guid UpstreamId,
    string DownstreamType,
    Guid DownstreamId,
    string Kind,
    string? RequiredState,
    Guid CreatedByUserId,
    string? Reason = null);
