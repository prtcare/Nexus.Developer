namespace Nexus.Developer.Core.Scope;

// Subset of Nexus.Experience's GetSubprojectResponse (Nexus.Products.Chat.Api /
// Endpoints/Subprojects/GetSubprojectResponse.cs) that this client needs: enough
// to confirm the subproject exists and echo back its identity. Field names match
// that response verbatim (SubprojectId, not Id -- the response has no "Id" field)
// so System.Text.Json binds them case-insensitively without a rename.
public sealed record ScopeSubproject(
    Guid SubprojectId,
    Guid ProjectId,
    string Name,
    string Reference);
