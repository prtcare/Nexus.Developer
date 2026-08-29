using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Application.Scope;

// Thrown by CreateFeatureHandler/CreateMilestoneHandler when Product Core's
// GET /api/v1/subprojects/{id} reports the subproject does not exist (IScopeClient
// returned null). The endpoint maps this to 400 Bad Request rather than letting
// it surface as an unhandled 500.
public sealed class SubprojectNotFoundException : Exception
{
    public SubprojectNotFoundException(SubprojectId subprojectId)
        : base($"The subproject '{subprojectId}' does not exist.")
    {
        SubprojectId = subprojectId;
    }

    public SubprojectId SubprojectId { get; }
}
