using Nexus.Developer.Core.Common;
using Nexus.Developer.Core.Common.Identifiers;

namespace Nexus.Developer.Core.Subtasks;

public interface ISubtaskRepository
    : IRepository<Subtask, SubtaskId>
{
    Task<IReadOnlyList<Subtask>> ListByTaskAsync(
        TaskId taskId,
        CancellationToken cancellationToken = default);
}
