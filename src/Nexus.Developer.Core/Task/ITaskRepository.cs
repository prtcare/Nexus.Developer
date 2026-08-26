using Nexus.Developer.Core.Common.Identifiers;
using DeveloperTask = Nexus.Developer.Core.Task.Task;

namespace Nexus.Developer.Core.Task;

// Deliberately does not extend the shared IRepository<TDomain, TId> convention --
// that interface's own method signatures (Task AddAsync(...), Task<TDomain?>
// GetAsync(...)) would need "Task" to mean two different things in the same
// declaration once TDomain = DeveloperTask. Spelled out directly instead, with the
// alias above keeping every domain-type usage unambiguous.
public interface ITaskRepository
{
    System.Threading.Tasks.Task AddAsync(
        DeveloperTask task,
        CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task<DeveloperTask?> GetAsync(
        TaskId id,
        CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task UpdateAsync(
        DeveloperTask task,
        CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task<IReadOnlyList<DeveloperTask>> ListByFeatureAsync(
        FeatureId featureId,
        CancellationToken cancellationToken = default);
}
