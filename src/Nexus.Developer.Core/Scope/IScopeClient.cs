using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.Scope;

// Client contract for Nexus.Experience's Product Core Subproject endpoint, not an
// implementation -- the HTTP implementation lives in
// Nexus.Developer.Infrastructure/Scope.
public interface IScopeClient
{
    // Returns null when Product Core's GET /api/v1/subprojects/{id} responds 404
    // (the subproject does not exist). Any other failure -- 5xx, timeout,
    // deserialization error -- throws: that is a real infrastructure failure, not
    // "subproject doesn't exist".
    Task<ScopeSubproject?> GetSubprojectAsync(
        SubprojectId subprojectId,
        CancellationToken cancellationToken = default);
}
